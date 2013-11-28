namespace PerfUtil

    open System
    open System.Collections.Generic

    open PerfUtil.Utils
    open PerfUtil.Persist

    /// Compares current implementation against a collection of past tests
    type PastImplementationComparer<'Testable when 'Testable :> ITestable>
        (currentImpl : 'Testable, testRunId : string, ?historyFile : string, 
            ?comparer : IPerformanceComparer, ?verbose : bool, ?throwOnError : bool) =

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

        let compareResultWithHistory (current : BenchmarkResult) =
            let olderRuns =
                pastSessions
                |> List.choose (fun s -> s.Tests.TryFind current.TestId)
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

            new PastImplementationComparer<'Testable>
                (currentImpl, sprintf "%s %O" currentImpl.ImplementationName version, ?historyFile = historyFile, 
                    ?comparer = comparer, ?verbose = verbose, ?throwOnError = throwOnError)

        member __.TestedImplementation = currentImpl

        member __.Test (testId : string) (testF : 'Testable -> unit) =
            if isCommited.Value then invalidOp "Test run has been finalized."
            lock currentSession (fun () ->
                let result = benchmark testRunId testId (fun () -> testF currentImpl)
                currentSession <- currentSession.Append(result)
                do compareResultWithHistory result)

        /// append current test results to persistence file
        member __.PersistCurrentResults () =
            lock isCommited (fun () ->
                match isCommited.Value with
                | true -> invalidOp "Cannot commit results twice."
                | false ->
                    sessionToFile currentImpl.ImplementationName historyFile (currentSession :: pastSessions)
                    isCommited := true)

        member __.GetTestSessions () = currentSession :: pastSessions

        interface IPerformanceTester<'Testable> with
            member __.TestedImplementation = __.TestedImplementation
            member __.Test testId testF = __.Test testId testF
            member __.GetTestSessions () = __.GetTestSessions ()