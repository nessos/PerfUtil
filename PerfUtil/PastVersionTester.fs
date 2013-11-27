namespace PerfUtil

    open System
    open System.Collections.Generic

    open PerfUtil.Utils
    open PerfUtil.Persist

    /// Compares current implementation against a collection of past tests
    type PastImplementationTester<'Testable when 'Testable :> ITestable>
        (currentImpl : 'Testable, testRunId : string, ?historyFile : string, 
            ?comparer : IPerformanceComparer, ?verbose : bool, ?throwOnError : bool) =

        let comparer = match comparer with Some p -> p | None -> new TimeComparer() :> _ 
        let verbose = defaultArg verbose true
        let throwOnError = defaultArg throwOnError false

        let currentTests = ref Map.empty<string, BenchmarkResult>
        let historyFile = defaultArg historyFile PerfUtil.DefaultPersistenceFile
        let history = historyOfFile currentImpl.ImplementationName historyFile
        let isCommited = ref false

        do if history.TestRuns.ContainsKey testRunId then
            invalidOp <| sprintf "A test run with id '%s' has already been recorded." testRunId

        let appendResult (result : BenchmarkResult) =
            currentTests := currentTests.Value.Add(result.TestId, result)

        let compareResultWithHistory (current : BenchmarkResult) =
            let olderRuns = 
                history.TestRuns
                |> Map.toList
                |> List.choose (fun (_,testRun) -> testRun.Results.TryFind current.TestId)
                |> List.map (fun older -> 
                    let isFaster = comparer.IsBetterOrEquivalent current older
                    let msg = comparer.GetComparisonMessage current older
                    older, isFaster, msg)

            if verbose then
                for _, isFaster, msg in olderRuns do
                    if isFaster then Console.WriteLine(msg)
                    else Console.Error.WriteLine(msg)

            if throwOnError then
                for older, isFaster, msg in olderRuns do
                    if not isFaster then 
                        raise <| new PerformanceException(msg, current, older)


        new (currentImpl : 'Testable, version : Version, ?historyFile : string, 
                ?comparer : IPerformanceComparer, ?verbose : bool, ?throwOnError : bool) =

            new PastImplementationTester<'Testable>
                (currentImpl, sprintf "Version %O" version, ?historyFile = historyFile, 
                    ?comparer = comparer, ?verbose = verbose, ?throwOnError = throwOnError)

        member __.TestedImplementation = currentImpl

        member __.Test (testId : string) (testF : 'Testable -> unit) =
            if isCommited.Value then invalidOp "Test run has been finalized."
            lock currentTests (fun () ->
                let result = benchmark testRunId testId (fun () -> testF currentImpl)
                do appendResult result
                do compareResultWithHistory result)

        /// append current test results to persistence file
        member __.PersistCurrentResults () =
            lock isCommited (fun () ->
                match isCommited.Value with
                | true -> invalidOp "Cannot commit results twice."
                | false ->
                    let currentRun = { Id = testRunId ; IndexNumber = history.TestRuns.Count ; Results = !currentTests }
                    let history = { history with TestRuns = history.TestRuns.Add(testRunId, currentRun) }
                    historyToFile historyFile history
                    isCommited := true)


        interface IPerformanceTester<'Testable> with
            member __.TestedImplementation = __.TestedImplementation
            member __.Test testId testF = __.Test testId testF