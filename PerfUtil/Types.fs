namespace PerfUtil

    open System
    open System.IO
    open System.Text

    /// An abstract implementation interface
    type ITestable =
        /// Implementation name.
        abstract Name : string

    /// Represents a performance test for a given class of implementations.
    type PerfTest<'Testable when 'Testable :> ITestable> =
        {
            Id : string
            Test : 'Testable -> unit
        }

    /// abstract performance tester
    [<AbstractClass>]
    type PerformanceTester<'Testable when 'Testable :> ITestable> () =

        /// The implementation under test.
        abstract TestedImplementation : 'Testable
        /// Run a performance test.
        abstract RunTest : PerfTest<'Testable> -> unit
        /// Get accumulated test results.
        abstract GetTestResults : unit -> TestSession list
        /// Benchmarks given function.
        member __.Run testId testF = __.RunTest { Id = testId ; Test = testF }

    /// compares between two performance results
    and IPerformanceComparer =
        /// Decides if current performance is better or equivalent to the other/older performance.
        abstract IsBetterOrEquivalent : current:PerfResult -> other:PerfResult -> bool
        /// Returns a message based on comparison of the two benchmarks.
        abstract GetComparisonMessage : current:PerfResult -> other:PerfResult -> string

    /// Represents a collection of tests performed in a given run.
    and TestSession =
        {   
            Id : string
            Date : DateTime
            /// results indexed by test id
            Results : Map<string, PerfResult>
        }
    with
        member s.Append(br : PerfResult, ?overwrite) =
            let overwrite = defaultArg overwrite true
            if not overwrite && s.Results.ContainsKey br.TestId then
                invalidOp <| sprintf "A test '%s' has already been recorded." br.TestId

            { s with Results = s.Results.Add(br.TestId, br) }

        static member Empty (id : string) =
            {
                Id = id
                Date = DateTime.Now
                Results = Map.empty
            }

    /// Contains performance information
    and PerfResult =
        {
            /// Test identifier
            TestId : string
            /// Test session identifier
            SessionId : string
            /// Execution date
            Date : DateTime

            /// Catch potential error
            Error : exn option

            Elapsed : TimeSpan
            CpuTime : TimeSpan
            /// Garbage collect differential per generation
            GcDelta : int list
        }
    with
        override r.ToString () =
            let sb = new StringBuilder()
            sb.Append(sprintf "%s: Real: %O, CPU: %O" r.TestId r.Elapsed r.CpuTime) |> ignore
            r.GcDelta |> List.iteri (fun g i -> sb.Append(sprintf ", gen%d: %d" g i) |> ignore)
            sb.Append(sprintf ", Date: %O" r.Date) |> ignore
            sb.ToString()

        member r.HasFailed = r.Error.IsSome


    type PerformanceException (message : string, this : PerfResult, other : PerfResult) =
        inherit System.Exception(message)

        do assert(this.TestId = other.TestId)

        member __.TestId = this.TestId
        member __.CurrentTestResult = this
        member __.OtherTestResult = other

    /// indicates that given method is a performance test
    type PerfTestAttribute() =
        inherit System.Attribute()

    type PerfUtil private () =
        static let mutable result = 
            let libPath = 
                System.Reflection.Assembly.GetExecutingAssembly().Location 
                |> Path.GetDirectoryName

            Path.Combine(libPath, "perfResults.xml")

        /// gets or sets the default persistence file used by the PastSessionComparer
        static member DefaultPersistenceFile
            with get () = result
            and set path =
                if not <| File.Exists(Path.GetDirectoryName path) then
                    invalidOp <| sprintf "'%s' is not a valid path." path

                lock result (fun () -> result <- path)