
// F# の詳細については、http://fsharp.net を参照してください
// 詳細については、'F# チュートリアル' プロジェクトを参照してください。

module Hoge =

    open System
    open System.IO
    open Microsoft.FSharp.Compiler.SourceCodeServices
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

                let path0 = projOptions.OtherOptions.[22]

                return! checker.ParseFileInProject(path0, File.ReadAllText(path0), projOptions) 
            }

        //let fsprojPath = "D:\\PROJECT\\Persimmon\\tests\\Persimmon.Script.Tests\\Persimmon.Script.Tests.fsproj"
        //let fsprojPath = "D:\\PROJECT\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests.fsproj"
        let fsprojPath = "C:\\PROJECT\\Persimmon\examples\\Persimmon.Sample\\Persimmon.Sample.fsproj"

        let results = parseFromFsproj(fsprojPath) |> Async.RunSynchronously
//        let r2 = results.GetAllUsesOfAllSymbols() |> Async.RunSynchronously
//        let re = r2 |> Seq.filter (fun symbolUse -> symbolUse.IsFromDefinition) |> Seq.map (fun symbolUse -> symbolUse.Symbol.FullName, symbolUse.Symbol) |> Seq.toArray
//        let r17ref = results.GetUsesOfSymbol(re.[17] |> snd) |> Async.RunSynchronously
//        let r70ref = results.GetUsesOfSymbol(r2.[70].Symbol) |> Async.RunSynchronously

        let rec visitPattern = function
          | SynPat.Wild(_) -> 
              printfn "  .. アンダースコアパターン"
          | SynPat.Named(pat, name, _, _, _) ->
              visitPattern pat
              printfn "  .. 名前 '%s' のパターン" name.idText
          | SynPat.LongIdent(LongIdentWithDots(ident, _), _, _, _, _, _) ->
              let names = String.concat "." [ for i in ident -> i.idText ]
              printfn "  .. 識別子: %s" names
          | pat -> printfn "  .. その他のパターン: %A" pat

        let rec visitExpression = function
          | SynExpr.IfThenElse(cond, trueBranch, falseBranchOpt, _, _, _, _) ->
              // すべての部分式を走査
              printfn "条件部:"
              visitExpression cond
              visitExpression trueBranch
              falseBranchOpt |> Option.iter visitExpression 

          | SynExpr.LetOrUse(_, _, bindings, body, _) ->
              // バインディングを走査
              // ('let .. = .. and .. = .. in ...' に対しては複数回走査されることがある)
              printfn "以下のバインディングを含むLetOrUse:"
              for binding in bindings do
                let (Binding(access, kind, inlin, mutabl, attrs, xmlDoc, 
                             data, pat, retInfo, init, m, sp)) = binding
                visitPattern pat 
                visitExpression init
              // 本体の式を走査
              printfn "本体は以下:"
              visitExpression body
          | expr -> printfn " - サポート対象外の式: %A" expr

        let visitDeclarations decls = 
            for declaration in decls do
            match declaration with
            | SynModuleDecl.Let(isRec, bindings, range) ->
                // 宣言としてのletバインディングは
                // (visitExpressionで処理したような)式としてのletバインディングと
                // 似ているが、本体を持たない
                for binding in bindings do
                    let (Binding(access, kind, inlin, mutabl, attrs, xmlDoc, 
                                data, pat, retInfo, body, m, sp)) = binding
                    visitPattern pat 
                    visitExpression body         
            | _ -> printfn " - サポート対象外の宣言: %A" declaration

        let visitModulesAndNamespaces modulesOrNss =
          for moduleOrNs in modulesOrNss do
            let (SynModuleOrNamespace(lid, isMod, decls, xml, attrs, _, m)) = moduleOrNs
            printfn "名前空間またはモジュール: %A" lid
            visitDeclarations decls

        match results.ParseTree.Value with
        | ParsedInput.ImplFile(implFile) ->
            // 宣言を展開してそれぞれを走査する
            let (ParsedImplFileInput(fn, script, name, _, _, modules, _)) = implFile
            visitModulesAndNamespaces modules
        | _ -> failwith "F# インターフェイスファイル (*.fsi) は未サポートです。"



        0 // 整数の終了コードを返します
