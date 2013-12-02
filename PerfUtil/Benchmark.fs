namespace PerfUtil

    open System
    open System.Reflection

    open PerfUtil.Utils

    type Benchmark private () =
            
        static let lockObj = box 42
        static let proc = System.Diagnostics.Process.GetCurrentProcess()
        static let numGC = System.GC.MaxGeneration

        // benchmark code, taken from FSI

        static member Run<'State>(testF : 'State -> unit, state : 'State, ?repetitions, ?sessionId, ?testId, ?catchExceptions) =
            let repetitions = defaultArg repetitions 1
            let catchExceptions = defaultArg catchExceptions false
            let testId = defaultArg testId ""
            let sessionId = defaultArg sessionId ""

            lock lockObj (fun () ->

            let stopwatch = new System.Diagnostics.Stopwatch()

            do 
                GC.Collect(3)
                GC.WaitForPendingFinalizers()
                GC.Collect(3)
                System.Threading.Thread.Sleep(100)


            let gcDelta = Array.zeroCreate<int> (numGC + 1)
            let inline computeGcDelta () =
                for i = 0 to numGC do
                    gcDelta.[i] <- System.GC.CollectionCount(i) - gcDelta.[i]

            do computeGcDelta ()
            let startTotal = proc.TotalProcessorTime
            let date = DateTime.Now
            stopwatch.Start()

            let error = 
                try 
                    for i = 1 to repetitions do testF state 
                    None 
                with e when catchExceptions -> Some e

            stopwatch.Stop()
            let total = proc.TotalProcessorTime - startTotal
            do computeGcDelta ()

            {
                Date = date
                TestId = testId
                SessionId = sessionId

                Error = error

                Elapsed = stopwatch.Elapsed
                CpuTime = total
                GcDelta = Array.toList gcDelta
            })
            

        static member Run(testF : unit -> unit, ?repetitions, ?sessionId, ?testId, ?catchExceptions) =
            Benchmark.Run(testF, (), ?repetitions = repetitions, ?sessionId = sessionId, 
                                        ?testId = testId, ?catchExceptions = catchExceptions)

        static member Run(perfTest : PerfTest<'Impl>, impl : 'Impl, ?repetitions, ?catchExceptions) =
            Benchmark.Run(perfTest.Test, impl, sessionId = impl.Name, testId = perfTest.Id, 
                                    ?repetitions = repetitions, ?catchExceptions = catchExceptions)