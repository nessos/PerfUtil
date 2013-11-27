namespace PerfUtil

    open System

    open PerfUtil.Utils

    /// Compares implementation performance against a list of other implementations
    type OtherImplemantationTester<'Testable when 'Testable :> ITestable>
        (testedImpl : 'Testable, otherImpls : 'Testable list, ?comparer : IPerformanceComparer, ?verbose, ?throwOnError) =

        let comparer = match comparer with Some c -> c | None -> new TimeComparer() :> _
        let verbose = defaultArg verbose true
        let throwOnError = defaultArg throwOnError false

        member __.TestedImplementation = testedImpl
        member __.Test (testId : string) (f : 'Testable -> unit) =
            let thisResult = benchmark testedImpl.ImplementationName testId (fun () -> f testedImpl)
            let otherResults = 
                otherImpls 
                |> List.map (fun o ->
                    let r = benchmark o.ImplementationName testId (fun () -> f o)
                    let isFaster = comparer.IsBetterOrEquivalent thisResult r
                    let msg = comparer.GetComparisonMessage thisResult r

                    if verbose then
                        if isFaster then Console.WriteLine(msg)
                        else Console.Error.WriteLine(msg)

                    msg, r, isFaster)

            if throwOnError then
                for msg, other, isFaster in otherResults do
                    if not isFaster then
                        raise <| new PerformanceException(msg, thisResult, other)

        interface IPerformanceTester<'Testable> with
            member __.TestedImplementation = __.TestedImplementation
            member __.Test testId testF = __.Test testId testF