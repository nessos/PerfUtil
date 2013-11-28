namespace PerfUtil

    open System
    open System.IO
    open System.Text

    type BenchmarkResult =
        {
            /// Test identifier
            TestId : string
            /// Test session identifier
            SessionId : string
            /// Execution date
            Date : DateTime

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

    and TestSession =
        {   
            Id : string
            Date : DateTime
            /// results indexed by test id
            Tests : Map<string, BenchmarkResult>
        }
    with
        member s.Append(br : BenchmarkResult, ?overwrite) =
            let overwrite = defaultArg overwrite true
            if overwrite && s.Tests.ContainsKey br.TestId then
                invalidOp <| sprintf "A test '%s' has already been recorded." br.TestId

            { s with Tests = s.Tests.Add(br.TestId, br) }

        static member Empty (id : string) =
            {
                Id = id
                Date = DateTime.Now
                Tests = Map.empty
            }

    type PerformanceException (message : string, this : BenchmarkResult, other : BenchmarkResult) =
        inherit System.Exception(message)

        do assert(this.TestId = other.TestId)

        member __.TestId = this.TestId
        member __.ThisPerformance = this
        member __.OtherPerformance = other


    /// inherit this interface to describe an abstracted test scenario
    type ITestable =
        abstract ImplementationName : string

    /// abstract performance test bed
    type IPerformanceTester<'Testable when 'Testable :> ITestable> =
        abstract TestedImplementation : 'Testable
        abstract Test : testId:string -> testF:('Testable -> unit) -> unit
        abstract GetTestSessions : unit -> TestSession list

    /// compare between two benchmark results
    type IPerformanceComparer =
        abstract IsBetterOrEquivalent : current:BenchmarkResult -> other:BenchmarkResult -> bool
        abstract GetComparisonMessage : current:BenchmarkResult -> other:BenchmarkResult -> string


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