﻿
// F# の詳細については、http://fsharp.net を参照してください
// 詳細については、'F# チュートリアル' プロジェクトを参照してください。

open System
open System.IO
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices

module private DiscovererImpl =

    type SymbolInformation (symbolName, fileName: string, minLine: int, maxLine: int, minColumn: int, maxColumn: int) =
        member __.SymbolName = symbolName
        member __.FileName = fileName
        member __.MinLine = minLine
        member __.MaxLine = maxLine
        member __.MinColumn = minColumn
        member __.MaxColumn = maxColumn

    [<Sealed>]
    type DiscoverContext private (symbolNames: string[], range: range) =
        new() = DiscoverContext([||], range())
        member __.Indent =
            symbolNames |> Seq.map (fun _ -> String.Empty) |> String.concat "  "
        member __.Nest(name: string, range) =
            DiscoverContext([name] |> (Seq.append symbolNames) |> Seq.toArray, range)
        member __.ToSymbolInformation() =
            SymbolInformation(symbolNames |> String.concat ".", range.FileName, range.StartLine, range.EndLine, range.StartColumn, range.EndColumn)

    let rec visitPattern (context: DiscoverContext) pat : SymbolInformation seq = seq {
        match pat with
//        | SynPat.Wild(range) -> 
//            let nest = context.Nest("_", range)
//            yield nest.ToSymbolInformation()
//            //printfn "%sUnderscore" context.Indent
        | SynPat.Named(pat, name, _, _, _) ->
            let nest = context.Nest(name.idText, name.idRange)
            yield! visitPattern nest pat
            yield nest.ToSymbolInformation()
            //printfn "%sNamed: '%s'" context.Indent name.idText
        | SynPat.LongIdent(LongIdentWithDots(ident, _), _, _, _, _, range) ->
            let names = String.concat "." [ for i in ident -> i.idText ]
            let nest = context.Nest(names, range)
            yield nest.ToSymbolInformation()
            //printfn "%sLongIdent: %s" context.Indent names
//        | pat -> printfn "%sその他のパターン: %A" context.Indent pat
        | _ -> ()
    }

    let visitSimplePatterns (context: DiscoverContext) pats : SymbolInformation seq = seq {
        match pats with
        | SynSimplePats.SimplePats(simplepats, _) ->
            for pat in simplepats do
                match pat with
                | SynSimplePat.Id(ident, _, _, _, _, range) ->
                    let nest = context.Nest(ident.idText, range)
                    yield nest.ToSymbolInformation()
                    //printfn "%sSimplePat.Id: '%s'" context.Indent ident.idText
                | _ -> ()
        | _ -> ()
    }

    let visitConst (context: DiscoverContext) value : SymbolInformation seq = seq {
        match value with
        | SynConst.String(str, range) ->
            let nest = context.Nest(str, range)
            yield nest.ToSymbolInformation()
            //printfn "%sString: \"%s\"" context.Indent str
        | _ -> ()
    }

    // Try get detailed test name (test title):
    //  expr:
    //    App (Expr0):
    //        App (Expr00):
    //          Ident: test
    //        App (Expr01):
    //          Const:
    //            String: "success test(list)"
    //    App (Expr1):
    //        CompExpr:
    //        ArrayOrListOfSeqExpr:   <-- context "Hoge"
    let tryGetTestName = function
        | SynExpr.App(_, _, expr0, expr1, _) ->
            match expr0 with
            | SynExpr.App(_, _, expr00, expr01, _) ->
                match expr01 with
                | SynExpr.Const(c, _) ->
                    match c with
                    | SynConst.String(str, range) ->
                        let contextSeq = match expr1 with | SynExpr.ArrayOrListOfSeqExpr(_, _, _) -> true | _ -> false
                        Some (str, contextSeq, range)
                    | _ -> None
                | _ -> None
            | _ -> None
        | _ -> None

    let rec visitExpressionInternal (context: DiscoverContext) expr : SymbolInformation seq =
      seq {
        match expr with
        // tests6
        | SynExpr.LetOrUse(_, _, bindings, body, _) ->
            //printfn "%sLetOrUse (Expr):" context.Indent
            for binding in bindings do
                yield! visitBinding context binding
            //printfn "%sLetOrUse (Body):" context.Indent
            yield! visitExpression context body
        | SynExpr.App(_, _, expr0, expr1, _) ->
            //printfn "%sApp (Expr0):" context.Indent
            yield! visitExpression context expr0
            //printfn "%sApp (Expr1):" context.Indent
            yield! visitExpression context expr1
        // test
//        | SynExpr.Ident id ->
//            //printfn "%sIdent: %A" context.Indent id
//            let nest = context.Nest(id.idText, id.idRange)
//            yield nest.ToSymbolInformation()
        // 'hogehoge'
//        | SynExpr.Const(c, _) ->
//            //printfn "%sConst:" context.Indent
//            yield! visitConst context c
        // tests, tests2, tests32
        | SynExpr.ArrayOrListOfSeqExpr(_, expr, _) ->
            //printfn "%sArrayOrListOfSeqExpr:" context.Indent
            yield! visitExpression context expr
        | SynExpr.CompExpr(_, _, expr, _) ->
            //printfn "%sCompExpr:" context.Indent
            yield! visitExpression context expr
        | SynExpr.Sequential(info, _, expr0, expr1, range) ->
            yield! visitExpression context expr0
            yield! visitExpression context expr1
//            //printfn "%sSequential: %A" context.Indent info
//            let nest0 = context.Nest("[0]", expr0.Range)
//            //printfn "%s[0]:" indent1
//            yield! visitExpression nest0 expr0
//            let nest1 = context.Nest("[1]", expr1.Range)
//            //printfn "%s[1]:" indent1
//            yield! visitExpression nest1 expr1
        // tests3, tests32
        | SynExpr.YieldOrReturn(_, expr, _) ->
            //printfn "%sYieldOrReturn:" context.Indent
            yield! visitExpression context expr
        // tests5
        | SynExpr.Paren(expr, _, _, _) ->
            //printfn "%sParen:" context.Indent
            yield! visitExpression context expr
        // tests5
        | SynExpr.Lambda(_, _, pats, expr, _) ->
            //printfn "%sLambda:" context.Indent
//            yield! visitSimplePatterns context pats
            yield! visitExpression context expr
//            | expr -> printfn "%sサポート対象外の式: %A" context.Indent expr
        | _ -> ()
      }

    and visitExpression (context: DiscoverContext) expr : SymbolInformation seq = seq {
        let nest =
            match tryGetTestName expr with
            | Some (name, _, range) -> Some (context.Nest(name, range))
            | None -> None
        match nest with
        | Some namedContext ->
            yield namedContext.ToSymbolInformation()
            yield! visitExpressionInternal namedContext expr
        | None ->
            yield! visitExpressionInternal context expr
      }

    and visitBinding (context: DiscoverContext) binding : SymbolInformation seq = seq {
        let (Binding(access, kind, inlin, mutabl, attrs, xmlDoc, 
                    data, pat, retInfo, body, m, sp)) = binding
        match pat with
        | SynPat.Named(pat, name, _, _, range) ->
            let namedContext =
                match tryGetTestName body with
                | Some (cname, contextSeq, crange) ->
                    match contextSeq with
                    | true -> context.Nest(cname, crange)
                    | false -> context.Nest(name.idText, range)
                | None -> context.Nest(name.idText, range)
            yield namedContext.ToSymbolInformation()
            yield! visitExpressionInternal namedContext body
        | SynPat.LongIdent(LongIdentWithDots(ident, _), _, _, _, _, range) ->
            let names = String.concat "." [ for i in ident -> i.idText ]
            let namedContext = context.Nest(names, range)
            yield namedContext.ToSymbolInformation()
            yield! visitExpressionInternal namedContext body
        | _ ->
            //yield! visitPattern context pat
            yield! visitExpressionInternal context body
      }

    let visitBindings (context: DiscoverContext) bindings : SymbolInformation seq = seq {
        for binding in bindings do
            yield! visitBinding context binding
    }

    let visitTypeDefine (context: DiscoverContext) typeDefine : SymbolInformation seq = seq {
        match typeDefine with
        | SynTypeDefn.TypeDefn(
                              SynComponentInfo.ComponentInfo(_, args, _, ident, _, _, _, range),
                              SynTypeDefnRepr.ObjectModel(kind, members, _),
                              _, _) ->
            let names = String.concat "." [ for i in ident -> i.idText ]
            let namedContext = context.Nest(names, range)
            //printfn "%sTypeDefn: %s" context.Indent names
            for memberDefine in members do
                match memberDefine with
                | SynMemberDefn.LetBindings(bindings, _, _, _) ->
                    //printfn "%sType Let:" namedContext.Indent
                    yield! visitBindings namedContext bindings
                | SynMemberDefn.Member(binding, _) ->
                    //printfn "%sType Member:" namedContext.Indent
                    yield! visitBinding namedContext binding
                | _ -> ()
        | _ -> ()
    }

    let rec visitDeclaration (context: DiscoverContext) decl : SymbolInformation seq = seq {
        match decl with
        // Basic module's binding
        | SynModuleDecl.Let(_, bindings, _) ->
            //printfn "%sModule Let:" context.Indent
            yield! visitBindings context bindings
        // MyClass
        | SynModuleDecl.Types(typeDefines, _) ->
            //printfn "%sModule Types:" context.Indent
            for typeDefine in typeDefines do
                yield! visitTypeDefine context typeDefine
        | SynModuleDecl.NestedModule(cinfo, nestedDecls, _, _) ->
            //printfn "%sNestedModule: %A" context.Indent decl
            let (SynComponentInfo.ComponentInfo(_, args, _, ident, _, _, _, range)) = cinfo
            let names = String.concat "." [ for i in ident -> i.idText ]
            let nest = context.Nest(names, range)
            yield! visitDeclarations nest nestedDecls
//            | _ -> printfn "%sサポート対象外の宣言: %A" context.Indent declaration
        | _ -> ()
     }

    and visitDeclarations (context: DiscoverContext) decls : SymbolInformation seq = seq {
        for declaration in decls do
            yield! visitDeclaration context declaration
    }

    let visitModulesAndNamespaces (context: DiscoverContext) modulesOrNss : SymbolInformation seq = seq {
        for moduleOrNs in modulesOrNss do
            let (SynModuleOrNamespace(ident, isMod, decls, xml, attrs, _, range)) = moduleOrNs
            //printfn "%sModuleOrNamespace: %A" context.Indent ident
            let names = String.concat "." [ for i in ident -> i.idText ]
            let nest = context.Nest(names, range)
            yield! visitDeclarations nest decls
    }

    let visitResults (results: FSharpParseFileResults) : SymbolInformation seq = seq {
        match results.ParseTree.Value with
        | ParsedInput.ImplFile(implFile) ->
            // 宣言を展開してそれぞれを走査する
            let (ParsedImplFileInput(fn, script, name, _, _, modules, _)) = implFile
            let context = DiscoverContext()
            yield! visitModulesAndNamespaces context modules
        | _ -> failwith "F# インターフェイスファイル (*.fsi) は未サポートです。"
    }

module Hoge =

    open System
    open System.IO
    open Microsoft.FSharp.Compiler.SourceCodeServices

    [<EntryPoint>]
    let main argv = 

        let parseFromFsproj (fsprojPath) =
            async {
                // インタラクティブチェッカーのインスタンスを作成
                let checker = FSharpChecker.Create()

                // スタンドアロンの(スクリプト)ファイルを表すコンテキストを取得
                let projOptions =
                    ProjectCracker.GetProjectOptionsFromProjectFile(fsprojPath)

                let path0 = projOptions.OtherOptions.[21]

                return! checker.ParseFileInProject(path0, File.ReadAllText(path0), projOptions) 
            }

        //let fsprojPath = "D:\\PROJECT\\Persimmon\\tests\\Persimmon.Script.Tests\\Persimmon.Script.Tests.fsproj"
        //let fsprojPath = "D:\\PROJECT\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests.fsproj"
        let fsprojPath = Path.Combine(Environment.CurrentDirectory, "Persimmon.Sample.fsproj")

        let results = parseFromFsproj(fsprojPath) |> Async.RunSynchronously
//        let r2 = results.GetAllUsesOfAllSymbols() |> Async.RunSynchronously
//        let re = r2 |> Seq.filter (fun symbolUse -> symbolUse.IsFromDefinition) |> Seq.map (fun symbolUse -> symbolUse.Symbol.FullName, symbolUse.Symbol) |> Seq.toArray
//        let r17ref = results.GetUsesOfSymbol(re.[17] |> snd) |> Async.RunSynchronously
//        let r70ref = results.GetUsesOfSymbol(r2.[70].Symbol) |> Async.RunSynchronously

        let finalResults = DiscovererImpl.visitResults results |> Seq.toArray


        0 // 整数の終了コードを返します
