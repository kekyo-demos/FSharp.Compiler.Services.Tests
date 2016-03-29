module Persimmon.Sample

////////////////////////////////
// SynModuleDecl.Let() のbindingsがトップレベルのlet bindingなので、
// 最初のチェックはここから。

open Persimmon

////////////////////////////////
// 単純なBindingであれば、RhsのNamedがシンボル名となる。

// Persimmon.Sample.test1
let test1 = test "success test" {
  do! assertEquals 1 1
}

// Persimmon.Sample.test2
let test2 = test "failure test" {
  do! assertEquals 1 2
}

exception MyException

// Persimmon.Sample.test3
let test3 = test "exn test" {
  let f () =
    raise MyException
    42
  let! e = trap { it (f ()) }
  do! assertEquals "" e.Message
  do! assertEquals typeof<MyException> (e.GetType())
  do! assertEquals "" (e.StackTrace.Substring(0, 5))
}

let err () = failwith "oops!"

// Persimmon.Sample.test4
let test4 = test "error test" {
  do! assertEquals 1 2
  do! assertEquals (err ()) 1
}

////////////////////////////////
// Lhsが存在しない式が、RhsのConstStringからシンボルを生成する必要がある。
// その場合は、Const.Stringの位置がRangeとなる。

let tests = [
  // Persimmon.Sample.tests.success test(list)
  test "success test(list)" {
    do! assertEquals 1 1
  }
  // Persimmon.Sample.tests.failure test(list)
  test "failure test(list)" {
    do! assertEquals 1 2
  }
]

let tests2 = [|
  // Persimmon.Sample.tests2.success test1(array)
  test "success test1(array)" {
    do! assertEquals 1 1
  }
  // Persimmon.Sample.tests2.success test2(array)
  test "success test2(array)" {
    do! assertEquals 1 1
  }
  // Persimmon.Sample.tests2.failure test(array)
  test "failure test(array)" {
    do! assertEquals 1 2
  }
|]

let tests3 = seq {
  // Persimmon.Sample.tests3.failure test(seq)
  yield test "failure test(seq)" {
    do! assertEquals 1 2
  }
}

let tests32 = seq {
  // Persimmon.Sample.tests32.success test(seq)
  yield test "success test(seq)" {
    do! assertEquals 1 2
  }
  // Persimmon.Sample.tests32.failure test(seq)
  yield test "failure test(seq)" {
    do! assertEquals 1 2
  }
}

let tests4 = [
  // Persimmon.Sample.tests4.success test(list with value)
  test "success test(list with value)" {
    return 10
  }
  // Persimmon.Sample.tests4.failure test(list with value)
  test "failure test(list with value)" {
    do! assertEquals 1 2
    return 20
  }
]

let tests5 =
  parameterize {
    case (1, 1)
    case (1, 2)
    // TODO: パラメタライズドテストのパラメータ部はFQTNに含めたくない...
    // Persimmon.Sample.tests5.case parameterize test(1, 1)
    // Persimmon.Sample.tests5.case parameterize test(1, 2)
    run (fun (x, y) -> test "case parameterize test" {
      do! assertEquals x y
    })
  }

let tests6 =
  // Persimmon.Sample.tests6.innerTest(1, 1)
  // Persimmon.Sample.tests6.innerTest(1, 2)
  let innerTest (x, y) = test "source parameterize test" {
    do! assertEquals x y
  }
  parameterize {
    source [
      (1, 1)
      (1, 2)
    ]
    run innerTest
  }

let context1 =
    context "Hoge" [
        context "Piyo" [
            // Persimmon.Sample.context1.Hoge.Piyo.success(context)
            test "success(context)" {
                do! assertEquals 1 1
            }
            // Persimmon.Sample.context1.Hoge.Piyo.failure(context)
            test "failure(context)" {
                do! assertEquals 1 2
            }
        ]
    ]


type MyClass() =
  let test1 = test "success test(class let)" {
    do! assertEquals 1 1
  }

  // Persimmon.Sample.MyClass.test2
  member __.test2() = test "not execute because this is instance method" {
    do! assertEquals 1 2
  }
  // Persimmon.Sample.MyClass.test3
  static member test3 = test "failure test(static property)" {
    do! assertEquals 1 2
  }

// not execute because this is not a zero parameter function
let helperTest x = assertions {
  do! assertEquals 1 x
}
