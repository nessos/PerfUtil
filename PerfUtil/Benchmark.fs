namespace PerfUtil

    open System
    open System.Reflection

    open PerfUtil.Utils

    // benchmarking code, taken from FSI timer implementation

    type Benchmark private () =
            
        static let lockObj = box 42
        static let proc = System.Diagnostics.Process.GetCurrentProcess()
        static let numGC = System.GC.MaxGeneration

        /// <summary>Benchmarks a given computation.</summary>
        /// <param name="testF">Test function.</param>
        /// <param name="state">Input state to the test function.</param>
        /// <param name="repetitions">Number of times to repeat the benchmark. Defaults to 1.</param>
        /// <param name="sessionId">Test session identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="testId">Test identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="catchExceptions">Catches exceptions raised by the test function. Defaults to false.</param>
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
                with e when catchExceptions -> Some e.Message

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
            

        /// <summary>Benchmarks a given computation.</summary>
        /// <param name="testF">Test function.</param>
        /// <param name="repetitions">Number of times to repeat the benchmark. Defaults to 1.</param>
        /// <param name="sessionId">Test session identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="testId">Test identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="catchExceptions">Catches exceptions raised by the test function. Defaults to false.</param>
        static member Run(testF : unit -> unit, ?repetitions, ?sessionId, ?testId, ?catchExceptions) =
            Benchmark.Run(testF, (), ?repetitions = repetitions, ?sessionId = sessionId, 
                                        ?testId = testId, ?catchExceptions = catchExceptions)

        /// <summary>Runs a given performance test.</summary>
        /// <param name="testF">Performance test.</param>
        /// <param name="impl">Implementation to run the performance test on.</param>
        /// <param name="repetitions">Number of times to repeat the benchmark. Defaults to 1.</param>
        /// <param name="catchExceptions">Catches exceptions raised by the test function. Defaults to false.</param>
        static member Run(perfTest : PerfTest<'Impl>, impl : 'Impl, ?repetitions, ?catchExceptions) =
            Benchmark.Run(perfTest.Test, impl, sessionId = impl.Name, testId = perfTest.Id, 
                                    ?repetitions = repetitions, ?catchExceptions = catchExceptions)