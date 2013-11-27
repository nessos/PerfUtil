namespace PerfUtil

    open System.IO

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

    module internal Persist =

        open System
        open System.IO
        open System.Xml.Linq

        type TestRun =
            {   
                Id : string
                IndexNumber : int
                Results : Map<string, BenchmarkResult>
            }

        and TestHistory =
            {
                TestSuiteName : string
                TestRuns : Map<string, TestRun>
            }

        let private xn name = XName.Get name

        let resultToXml (br : BenchmarkResult) =
            XElement(xn "testResult",
                XAttribute(xn "testId", br.TestId),
                XAttribute(xn "testDate", br.Date),
                XElement(xn "elapsedTime", br.Elapsed.Ticks),
                XElement(xn "cpuTime", br.CpuTime.Ticks),
                XElement(xn "gcDelta",
                    br.GcDelta 
                    |> List.mapi (fun gen delta -> XElement(xn <| sprintf "gen%d" gen, delta)))
                )

        let resultOfXml testRunId (xEl : XElement) =
            {
                TestId = xEl.Attribute(xn "testId").Value
                ContextId = testRunId
                Date = xEl.Attribute(xn "testDate").Value |> DateTime.Parse

                Elapsed = xEl.Element(xn "elapsedTime").Value |> int64 |> TimeSpan.FromTicks
                CpuTime = xEl.Element(xn "cpuTime").Value |> int64 |> TimeSpan.FromTicks
                GcDelta =
                    xEl.Element(xn "gcDelta").Elements()
                    |> Seq.map (fun gc -> int gc.Value)
                    |> Seq.toList
            }

        let testsToXml (tests : TestRun) =
            XElement(xn "testRun",
                XAttribute(xn "id", tests.Id),
                XAttribute(xn "count", tests.IndexNumber),
                tests.Results |> Map.toSeq |> Seq.map (snd >> resultToXml))

        let testsOfXml (xEl : XElement) =
            let id = xEl.Attribute(xn "id").Value
            let index = xEl.Attribute(xn "count").Value |> int
            let results = 
                xEl.Elements(xn "testResult") 
                |> Seq.map (resultOfXml id) 
                |> Seq.map (fun tr -> tr.TestId, tr)
                |> Map.ofSeq
            {
                Id = id
                IndexNumber = index
                Results = results
            }

        let testHistoryToXml (history : TestHistory) =
            XDocument(
                XElement(xn "testSuite",
                    XAttribute(xn "name", history.TestSuiteName),
                    history.TestRuns |> Map.toSeq |> Seq.map snd |> Seq.sortBy(fun n -> n.IndexNumber) |> Seq.map testsToXml))

        let testHistoryOfXml (root : XDocument) =
            let xEl = root.Element(xn "testSuite")
            {
                TestSuiteName = xEl.Attribute(xn "name").Value
                TestRuns = 
                    xEl.Elements(xn "testRun") 
                    |> Seq.map testsOfXml 
                    |> Seq.map (fun tr -> tr.Id, tr)
                    |> Map.ofSeq
            }

        let historyOfFile (name : string) (path : string) =
            if File.Exists(path) then
                let history = XDocument.Load(path) |> testHistoryOfXml
                { history with TestSuiteName = name }
            else
                {
                    TestSuiteName = name
                    TestRuns = Map.empty 
                }

        let historyToFile (path : string) (history : TestHistory) =
            let doc = testHistoryToXml history
            doc.Save(path)