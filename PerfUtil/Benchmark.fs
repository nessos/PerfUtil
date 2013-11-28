namespace PerfUtil

    [<AutoOpen>]
    module Benchmark =

        open System

        let inline repeat times (f : 'State -> unit) (state : 'State) =
            for i = 1 to times do f state
            
        let private lockObj = box 42
        let private proc = System.Diagnostics.Process.GetCurrentProcess()
        let private numGC = System.GC.MaxGeneration

        // benchmark code, taken from FSI

        let benchmark sessionId testId (f : unit -> unit) =
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
                SessionId = sessionId

                Elapsed = stopwatch.Elapsed
                CpuTime = total
                GcDelta = spanGC
            })