
// F# の詳細については、http://fsharp.net を参照してください
// 詳細については、'F# チュートリアル' プロジェクトを参照してください。

module Hoge =

    open System
    open System.IO
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open Microsoft.FSharp.Compiler.Ast
    open Microsoft.FSharp.Compiler.Ast

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

        let rec visitPattern (indent: string) = function
          | SynPat.Wild(_) -> 
              printfn "%sUnderscore" indent
          | SynPat.Named(pat, name, _, _, _) ->
              visitPattern (indent + "  ") pat
              printfn "%sNamed: '%s'" indent name.idText
          | SynPat.LongIdent(LongIdentWithDots(ident, _), _, _, _, _, _) ->
              let names = String.concat "." [ for i in ident -> i.idText ]
              printfn "%sLongIdent: %s" indent names
//        | pat -> printfn "%sその他のパターン: %A" pat
          | _ -> ()

        let visitSimplePatterns (indent: string) = function
          | SynSimplePats.SimplePats(pats, _) ->
              for pat in pats do
                match pat with
                | SynSimplePat.Id(ident, _, _, _, _, _) ->
                  printfn "%sSimplePat.Id: '%s'" indent ident.idText
                | _ -> ()
          | _ -> ()

        let visitConst (indent: string) = function
          | SynConst.String(str, _) ->
              printfn "%sString: \"%s\"" indent str
          | _ -> ()

        let rec visitExpression (indent: string) = function
          // tests6
          | SynExpr.LetOrUse(_, _, bindings, body, _) ->
              printfn "%sLetOrUse (Expr):" indent
              for binding in bindings do
                let (Binding(access, kind, inlin, mutabl, attrs, xmlDoc, 
                             data, pat, retInfo, init, m, sp)) = binding
                visitPattern (indent + "  ") pat
                visitExpression (indent + "  ") init
              // 本体の式を走査
              printfn "%sLetOrUse (Body):" indent
              visitExpression (indent + "  ") body
          | SynExpr.App(_, _, expr0, expr1, _) ->
              printfn "%sApp (Expr0):" indent
              visitExpression (indent + "  ") expr0
              printfn "%sApp (Expr1):" indent
              visitExpression (indent + "  ") expr1
          // test
          | SynExpr.Ident id ->
              printfn "%sIdent: %A" indent id
          // 'hogehoge'
          | SynExpr.Const(c, _) ->
              printfn "%sConst:" indent
              visitConst (indent + "  ") c
          // tests, tests2, tests32
          | SynExpr.ArrayOrListOfSeqExpr(_, expr, _) ->
              printfn "%sArrayOrListOfSeqExpr:" indent
              visitExpression (indent + "  ") expr
          | SynExpr.CompExpr(_, _, expr, _) ->
              printfn "%sCompExpr:" indent
              visitExpression (indent + "  ") expr
          | SynExpr.Sequential(info, _, expr0, expr1, _) ->
              printfn "%sSequential: %A" indent info
              let indent1 = indent + "  "
              printfn "%s[0]:" indent1
              visitExpression (indent1 + "  ") expr0
              printfn "%s[1]:" indent1
              visitExpression (indent1 + "  ") expr1
          // tests3, tests32
          | SynExpr.YieldOrReturn(_, expr, _) ->
              printfn "%sYieldOrReturn:" indent
              visitExpression (indent + "  ") expr
          // tests5
          | SynExpr.Paren(expr, _, _, _) ->
              printfn "%sParen:" indent
              visitExpression (indent + "  ") expr
          // tests5
          | SynExpr.Lambda(_, _, pats, expr, _) ->
              printfn "%sLambda:" indent
              visitSimplePatterns (indent + "  ") pats
              visitExpression (indent + "  ") expr
//          | expr -> printfn "%sサポート対象外の式: %A" indent expr
          | _ -> ()

        let visitDeclarations (indent: string) decls = 
            for declaration in decls do
            match declaration with
            | SynModuleDecl.Let(isRec, bindings, range) ->
                // 宣言としてのletバインディングは
                // (visitExpressionで処理したような)式としてのletバインディングと
                // 似ているが、本体を持たない
                printfn "%slet bindings:" indent
                for binding in bindings do
                    let (Binding(access, kind, inlin, mutabl, attrs, xmlDoc, 
                                data, pat, retInfo, body, m, sp)) = binding
                    visitPattern (indent + "  ") pat
                    visitExpression (indent + "  ") body
//            | _ -> printfn "%sサポート対象外の宣言: %A" indent declaration
            | _ -> ()

        let visitModulesAndNamespaces (indent: string) modulesOrNss =
          for moduleOrNs in modulesOrNss do
            let (SynModuleOrNamespace(lid, isMod, decls, xml, attrs, _, m)) = moduleOrNs
            printfn "%s名前空間またはモジュール: %A" indent lid
            visitDeclarations "  " decls

        match results.ParseTree.Value with
        | ParsedInput.ImplFile(implFile) ->
            // 宣言を展開してそれぞれを走査する
            let (ParsedImplFileInput(fn, script, name, _, _, modules, _)) = implFile
            visitModulesAndNamespaces "" modules
        | _ -> failwith "F# インターフェイスファイル (*.fsi) は未サポートです。"



        0 // 整数の終了コードを返します
