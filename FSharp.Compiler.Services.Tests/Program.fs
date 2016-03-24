// F# の詳細については、http://fsharp.net を参照してください
// 詳細については、'F# チュートリアル' プロジェクトを参照してください。

open System
open System.IO
open Microsoft.FSharp.Compiler.SourceCodeServices

[<EntryPoint>]
let main argv = 

    let parseAndTypeCheckFSProj (fsprojPath) =
        async {
            // インタラクティブチェッカーのインスタンスを作成
            let checker = FSharpChecker.Create()

            // スタンドアロンの(スクリプト)ファイルを表すコンテキストを取得
            let projOptions =
                ProjectCracker.GetProjectOptionsFromProjectFile(fsprojPath)

            return! checker.ParseAndCheckProject(projOptions) 
        }

    let fsprojPath = "D:\\PROJECT\\Persimmon\\tests\\Persimmon.Script.Tests\\Persimmon.Script.Tests.fsproj"
    let fsprojPath = "D:\\PROJECT\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests\\FSharp.Compiler.Services.Tests.fsproj"

    let results = 
        parseAndTypeCheckFSProj(fsprojPath) |> Async.RunSynchronously

    0 // 整数の終了コードを返します
