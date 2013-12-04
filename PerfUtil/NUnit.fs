namespace PerfUtil

    open NUnit.Framework

    open PerfUtil.Utils

    [<AbstractClass>]
    [<TestFixture>]
    /// Inheriting this class in an assembly defines a dynamic NUnit test fixture.
    type NUnitPerf<'Impl when 'Impl :> ITestable> () =

        /// specifies the performance testbed to be used.
        abstract PerfTester : PerformanceTester<'Impl>
        /// specifies the performance tests to be tested.
        abstract PerfTests : PerfTest<'Impl> list

        member internal u.GetTestCases () = 
            u.PerfTests |> Seq.map (fun t -> TestCaseData(t).SetName(quoteText t.Id))

        [<Test ; TestCaseSource("GetTestCases")>]
        member u.PerformanceTests(test : PerfTest<'Impl>) = u.PerfTester.RunTest test
