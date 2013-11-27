namespace PerfUtil

    open System

    open PerfUtil.Utils

    type IPerformanceComparer =
        abstract IsBetterOrEquivalent : current:BenchmarkResult -> other:BenchmarkResult -> bool
        abstract GetComparisonMessage : current:BenchmarkResult -> other:BenchmarkResult -> string

    type TimeComparer () =
        interface IPerformanceComparer with
            member __.IsBetterOrEquivalent (current : BenchmarkResult) (other : BenchmarkResult) =
                current.Elapsed >= other.Elapsed

            member __.GetComparisonMessage current other = defaultComparisonMessage current other

    and AbsoluteComparer () =
        interface IPerformanceComparer with
            member __.IsBetterOrEquivalent (current : BenchmarkResult) (other : BenchmarkResult) =
                current.Elapsed <= other.Elapsed
                    && current.CpuTime <= other.CpuTime 
                    && getSpace current <= getSpace other

            member __.GetComparisonMessage current other = defaultComparisonMessage current other

    and MeanComparer (?spaceFactor : float, ?leastAcceptableImprovementFactor) =
        let spaceFactor = defaultArg spaceFactor 0.2
        let timeFactor = 1. - spaceFactor
        let leastAcceptableImprovementFactor = defaultArg leastAcceptableImprovementFactor 1.

        do if spaceFactor < 0. || spaceFactor > 1. then invalidArg "spaceFactor" "value must be between 0 and 1."

        interface IPerformanceComparer with
            member __.IsBetterOrEquivalent (current : BenchmarkResult) (other : BenchmarkResult) =
                let dtime = getTimeSpanRatio current.Elapsed other.Elapsed
                let dspace = getSpaceRatio current other
                dtime * timeFactor + dspace * spaceFactor >= leastAcceptableImprovementFactor

            member __.GetComparisonMessage current other = defaultComparisonMessage current other