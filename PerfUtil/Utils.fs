namespace PerfUtil

    open System

    module internal Utils =

        [<Literal>]
        let gcGenWeight = 10
        
        // computes a performance improvement factor out of two given timespans
        // add + 1L to eliminate the slim possibility of division by zero's and NaN's
        // ticks register large numbers so this shouldn't skew the final result significantly.
        let getTimeSpanRatio (this : TimeSpan) (that : TimeSpan) =
            float (decimal (this.Ticks + 1L) / decimal (that.Ticks + 1L))

        // computes a polynomial value out of gc garbage collection data
        // [ gen0 ; gen1 ; gen2 ] -> gen0 + 10 * gen1 + 10^2 * gen2
        let getSpace (r : BenchmarkResult) = 
            r.GcDelta
            |> List.mapi (fun i g -> g * pown gcGenWeight i) 
            |> List.sum

        let getSpaceRatio (this : BenchmarkResult) (that : BenchmarkResult) =
            (float (getSpace this) + 0.1) / (float (getSpace that) + 0.1)

        // add single quotes if text contains whitespace
        let quoteText (text : string) =
            if text |> Seq.exists Char.IsWhiteSpace then
                sprintf "'%s'" text
            else
                text

        let defaultComparisonMessage (this : BenchmarkResult) (other : BenchmarkResult) =
            assert(this.TestId = other.TestId)

            sprintf "%s.%s was %.2fx faster and %.2fx more memory efficient than %s.%s."
                (quoteText this.SessionId) (quoteText other.TestId)
                    (getTimeSpanRatio this.Elapsed other.Elapsed)
                    (getSpaceRatio this other)
                (quoteText other.SessionId) (quoteText other.TestId)

        let getDuplicates xs =
            xs
            |> Seq.groupBy id 
            |> Seq.choose (fun (id, vs) -> if Seq.length vs > 1 then Some id else None)