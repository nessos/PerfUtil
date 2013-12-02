#r "bin/Release/PerfUtil.dll"

open System
open System.Threading

open PerfUtil

// basic usage

let result = Benchmark.Run <| repeat 100 (fun () -> Thread.Sleep 10)

// build a test setting

type IOperation =
    inherit ITestable
    abstract Run : unit -> unit

let dummy name (interval:int) = 
    {
        new IOperation with
            member __.Name = name
            member __.Run () = System.Threading.Thread.Sleep(interval)
    }

let foo = dummy "foo" 10

// past version comparison

let tester = new PastImplementationComparer<IOperation>(foo, Version(0,1), historyFile = "D:/persist.xml", throwOnError = true)

tester.Run "test 0" (repeat 100 (fun o -> o.Run()))
tester.Run "test 1" (repeat 100 (fun o -> o.Run()))
tester.Run "test 2" (repeat 100 (fun o -> o.Run()))

tester.PersistCurrentResults()

// compare to other versions

let tester' = new ImplemantationComparer<IOperation>(foo, [dummy "bar" 5 ; dummy "baz" 20 ], throwOnError = true)

tester'.Run "test 0" (repeat 100 (fun o -> o.Run()))