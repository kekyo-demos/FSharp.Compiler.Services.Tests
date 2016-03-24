// F# の詳細については、http://fsharp.net を参照してください
// 詳細については、'F# チュートリアル' プロジェクトを参照してください。

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

                let path0 = projOptions.OtherOptions.[22]

                return! checker.ParseFileInProject(path0, File.ReadAllText(path0), projOptions) 
            }

        //let fsprojPath = "D:\\PROJECT\\Persimmon\\tests\\Persimmon.Script.Tests\\Persimmon.Script.Tests.fsproj"
        //let fsprojPath = "D:\\PROJECT\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests.fsproj"
        let fsprojPath = "D:\\PROJECT\\Persimmon\examples\\Persimmon.Sample\\Persimmon.Sample.fsproj"

        let results = parseFromFsproj(fsprojPath) |> Async.RunSynchronously
//        let r2 = results.GetAllUsesOfAllSymbols() |> Async.RunSynchronously
//        let re = r2 |> Seq.filter (fun symbolUse -> symbolUse.IsFromDefinition) |> Seq.map (fun symbolUse -> symbolUse.Symbol.FullName, symbolUse.Symbol) |> Seq.toArray
//        let r17ref = results.GetUsesOfSymbol(re.[17] |> snd) |> Async.RunSynchronously
//        let r70ref = results.GetUsesOfSymbol(r2.[70].Symbol) |> Async.RunSynchronously

        0 // 整数の終了コードを返します
