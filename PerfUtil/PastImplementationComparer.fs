namespace PerfUtil

    open System
    open System.Collections.Generic

    open PerfUtil.Utils
    open PerfUtil.Persist

    /// <summary>Compares current implementation against a collection of past tests.</summary>
    /// <param name="currentImpl">Implementation under test.</param>
    /// <param name="testRunId">Unique identifier of current implementation.</param>
    /// <param name="historyFile">Specifies path to persisted past test results. Defaults to 'PerfUtil.DefaultPersistenceFile'.</param>
    /// <param name="verbose">Print performance results to stdout.</param>
    /// <param name="throwOnError">Raise an exception if performance comparison fails. Defaults to false.</param>
    type PastImplementationComparer<'Testable when 'Testable :> ITestable>
        (currentImpl : 'Testable, testRunId : string, ?historyFile : string, 
            ?comparer : IPerformanceComparer, ?verbose : bool, ?throwOnError : bool) =

        inherit PerformanceTester<'Testable> ()

        let comparer = match comparer with Some p -> p | None -> new TimeComparer() :> _ 
        let verbose = defaultArg verbose true
        let throwOnError = defaultArg throwOnError false
        let historyFile = defaultArg historyFile PerfUtil.DefaultPersistenceFile

        let mutable currentSession = TestSession.Empty testRunId
        let pastSessions = sessionOfFile historyFile
        let isCommited = ref false

        do 
            let duplicates =
                currentSession :: pastSessions
                |> Seq.map (fun s -> s.Id)
                |> getDuplicates
                |> Seq.toList

            match duplicates with
            | [] -> ()
            | hd :: _ -> 
                invalidArg "otherImpls" <|
                    sprintf "Found duplicate implementation id '%s'." hd

        let compareResultWithHistory (current : PerfResult) =
            let olderRuns =
                pastSessions
                |> List.choose (fun s -> s.Results.TryFind current.TestId)
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

        /// <summary>Compares current implementation against a collection of past tests.</summary>
        /// <param name="currentImpl">Implementation under test.</param>
        /// <param name="version">Version number of current implementation.</param>
        /// <param name="historyFile">Specifies path to persisted past test results. Defaults to 'PerfUtil.DefaultPersistenceFile'.</param>
        /// <param name="verbose">Print performance results to stdout.</param>
        /// <param name="throwOnError">Raise an exception if performance comparison fails. Defaults to false.</param>
        new (currentImpl : 'Testable, version : Version, ?historyFile : string, 
                ?comparer : IPerformanceComparer, ?verbose : bool, ?throwOnError : bool) =

            new PastImplementationComparer<'Testable>
                (currentImpl, sprintf "%s v.%O" currentImpl.Name version, ?historyFile = historyFile, 
                    ?comparer = comparer, ?verbose = verbose, ?throwOnError = throwOnError)

        override __.TestedImplementation = currentImpl

        override __.RunTest (perfTest : PerfTest<'Testable>) =
            if isCommited.Value then invalidOp "Test run has been finalized."
            lock currentSession (fun () ->
                let result = Benchmark.Run(perfTest.Test, currentImpl, sessionId = testRunId, testId = perfTest.Id)
                currentSession <- currentSession.Append(result)
                do compareResultWithHistory result)

        override __.GetTestResults () = currentSession :: pastSessions

        /// append current test results to persistence file
        member __.PersistCurrentResults () =
            lock isCommited (fun () ->
                match isCommited.Value with
                | true -> invalidOp "Cannot commit results twice."
                | false ->
                    sessionToFile currentImpl.Name historyFile (currentSession :: pastSessions)
                    isCommited := true)