namespace PerfUtil

    open NUnit.Framework

    open PerfUtil.Utils

    [<AbstractClass>]
    [<TestFixture>]
    type NUnitPerf<'Impl when 'Impl :> ITestable> () =

        abstract PerfTester : PerformanceTester<'Impl>
        abstract PerfTests : PerfTest<'Impl> list

        member internal u.TestCaseProvider = 
            u.PerfTests |> Seq.map (fun t -> TestCaseData(t).SetName(quoteText t.Id))

        [<Test ; TestCaseSource("TestCaseProvider")>]
        member u.PerformanceTests(test : PerfTest<'Impl>) = u.PerfTester.RunTest test
