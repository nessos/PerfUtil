namespace PerfUtil.CaseStudy

    open PerfUtil

    // create and NUNit test bed

    type SerializationTests() =
        inherit NUnitPerf<ISerializer> ()

        override __.PerfTester = SerializerComparer.Create(throwOnError = true)
        override __.PerfTests = PerfTest.OfModuleMarker<Tests.Marker> ()