namespace PerfUtil.CaseStudy

    open PerfUtil

    // create an NUnit test bed

    type SerializationTests() =
        inherit NUnitPerf<ISerializer> ()

        override __.PerfTester = SerializerComparer.Create(throwOnError = true)
        override __.PerfTests = PerfTest.OfModuleMarker<Tests.Marker> ()