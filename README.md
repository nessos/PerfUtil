###PerfUtil

A collection of tools and abstractions for helping performance tests.
Two main operation modes are provided:
* Comparison of a given implementation against others.
* Comparison of current implementation against a history of past performance tests.

###Basic Usage

```fsharp
open PerfUtil

let result = benchmark "test suite foo" "test0" (repeat 100 (fun () -> Thread.Sleep 10))

val result : BenchmarkResult = {TestId = "test0";
                                ContextId = "test suite foo";
                                Date = 27/11/2013 5:34:55 pm;
                                Elapsed = 00:00:00.9998089;
                                CpuTime = 00:00:00;
                                GcDelta = [0; 0; 0];}
```

###Comparing implementations

Defining a test context:
```fsharp
type IOperation =
    inherit ITestable
    abstract Run : unit -> unit

let dummy name (interval:int) = 
    {
        new IOperation with
            member __.ImplementationName = name
            member __.Run () = System.Threading.Thread.Sleep(interval)
    }

let tested = dummy "foo" 10

```
#### Testing against other implementations
```fsharp
let testBed = new OtherImplemantationTester<IOperation>(tested, [dummy "bar" 5 ; dummy "baz" 20 ])

testBed.Test "test 0" (repeat 100 (fun o -> o.Run()))
// Output
// foo.'test 0' was 2.00x faster and 1.00x more memory efficient than bar.'test 0'
// foo.'test 0' was 0.50x faster and 1.00x more memory efficient than baz.'test 0'
```
#### Testing against past test runs
```fsharp
let test = new PastImplementationTester<IOperation>(tested, Version(0,3), historyFile = "persist.xml")

test.Test "test 0" (fun s -> s.Run())
// Output
// 'Version 0.3'.test0 was 1.00x faster and 1.00x more memory efficient than 'Version 0.2'.test0
// 'Version 0.3'.test0 was 1.00x faster and 1.00x more memory efficient than 'Version 0.1'.test0

// append current results to history file
test.PersistCurrentResults()
```
#### Defining abstract performance tests

Both `OtherImplemantationTester<'T>` and `PastImplementationTester<'T>` are instances of the
`IPerformanceTester<'T>` interface, so defining abstract performance comparison suites is actually possible.
