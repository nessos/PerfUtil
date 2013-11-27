namespace PerfUtil

    open System
    open System.Text

    type BenchmarkResult =
        {
            /// Specific test id
            TestId : string
            /// Execution context id for given test
            ContextId : string
            /// Execution date
            Date : DateTime

            Elapsed : TimeSpan
            CpuTime : TimeSpan
            GcDelta : int list
        }
    with
        override r.ToString () =
            let sb = new StringBuilder()
            sb.Append(sprintf "%s: Real: %O, CPU: %O" r.TestId r.Elapsed r.CpuTime) |> ignore
            r.GcDelta |> List.iteri (fun g i -> sb.Append(sprintf ", gen%d: %d" g i) |> ignore)
            sb.Append(sprintf ", Date: %O" r.Date) |> ignore
            sb.ToString()

    type PerformanceException (message : string, this : BenchmarkResult, other : BenchmarkResult) =
        inherit System.Exception(message)

        do assert(this.TestId = other.TestId)

        member __.TestId = this.TestId
        member __.ThisPerformance = this
        member __.OtherPerformance = other


    [<AutoOpen>]
    module Benchmark =

        let inline repeat times (f : 'State -> unit) (state : 'State) =
            for i = 1 to times do f state
            
        let private lockObj = box 42
        let private proc = System.Diagnostics.Process.GetCurrentProcess()
        let private numGC = System.GC.MaxGeneration

        // benchmark code, taken from FSI

        let benchmark contextId testId (f : unit -> unit) =
            lock lockObj (fun () ->

            let stopwatch = new System.Diagnostics.Stopwatch()

            do 
                GC.Collect(3)
                GC.WaitForPendingFinalizers()
                GC.Collect(3)
                System.Threading.Thread.Sleep(100)

            let startGC = [| for i in 0 .. numGC -> System.GC.CollectionCount(i) |]
            let startTotal = proc.TotalProcessorTime
            let date = DateTime.Now
            stopwatch.Start()

            let res = f ()

            stopwatch.Stop()
            let total = proc.TotalProcessorTime - startTotal
            let spanGC = [ for i in 0 .. numGC -> System.GC.CollectionCount(i) - startGC.[i] ]

            {
                Date = date
                TestId = testId
                ContextId = contextId

                Elapsed = stopwatch.Elapsed
                CpuTime = total
                GcDelta = spanGC
            })