namespace PerfUtil

    open System

    open PerfUtil.Utils

    type TimeComparer (?errorMargin : TimeSpan) =
        let errorMargin = defaultArg errorMargin TimeSpan.Zero
        interface IPerformanceComparer with
            member __.IsBetterOrEquivalent (current : PerfResult) (other : PerfResult) =
                if other.HasFailed then true
                else
                    other.Elapsed - current.Elapsed >= errorMargin

            member __.GetComparisonMessage current other = defaultComparisonMessage current other

    and AbsoluteComparer () =
        interface IPerformanceComparer with
            member __.IsBetterOrEquivalent (current : PerfResult) (other : PerfResult) =
                if other.HasFailed then true
                else
                    current.Elapsed <= other.Elapsed
                        && current.CpuTime <= other.CpuTime 
                        && getSpace current <= getSpace other

            member __.GetComparisonMessage current other = defaultComparisonMessage current other

    and WeightedComparer (?spaceFactor : float, ?leastAcceptableImprovementFactor) =
        let spaceFactor = defaultArg spaceFactor 0.2
        let timeFactor = 1. - spaceFactor
        let leastAcceptableImprovementFactor = defaultArg leastAcceptableImprovementFactor 1.

        do if spaceFactor < 0. || spaceFactor > 1. then invalidArg "spaceFactor" "value must be between 0 and 1."

        interface IPerformanceComparer with
            member __.IsBetterOrEquivalent (current : PerfResult) (other : PerfResult) =
                if other.HasFailed then true
                else
                    let dtime = getTimeSpanRatio current.Elapsed other.Elapsed
                    let dspace = getSpaceRatio current other
                    dtime * timeFactor + dspace * spaceFactor >= leastAcceptableImprovementFactor

            member __.GetComparisonMessage current other = defaultComparisonMessage current other