#r "bin/Release/PerfUtil.dll"

open System
open System.Threading

open PerfUtil

// basic usage

let result = benchmark "test suite foo" "test0" (repeat 100 (fun () -> Thread.Sleep 10))

// build a test setting

type IOperation =
    inherit ITestable
    abstract Run : unit -> unit

let dummy name (interval:int) = 
    {
        new IOperation with
            member __.ImplementationName = name
            member __.Run () = System.Threading.Thread.Sleep(interval)
    }

let foo = dummy "foo" 10

// past version comparison

let test = new PastImplementationTester<IOperation>(foo, Version(0,1), historyFile = "D:/persist.xml", throwOnError = true)

test.Test "A" (repeat 100 (fun o -> o.Run()))
test.Test "B" (repeat 100 (fun o -> o.Run()))
test.Test "C" (repeat 100 (fun o -> o.Run()))

test.PersistCurrentResults()

// compare to other versions

let test' = new OtherImplemantationTester<IOperation>(foo, [dummy "bar" 5 ; dummy "baz" 20 ], throwOnError = true)

test'.Test "A" (repeat 100 (fun o -> o.Run()))