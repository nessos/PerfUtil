namespace PerfUtil

    open PerfUtil.Persist

    [<AutoOpen>]
    module Extensions =

        [<RequireQualifiedAccess>]
        module TestSession =

            let groupByTest (tests : TestSession list) =
                tests 
                |> Seq.collect (fun t -> t.Tests |> Map.toSeq |> Seq.map snd)
                |> Seq.groupBy (fun br -> br.TestId)
                |> Seq.map (fun (k,vs) -> (k, Seq.toList vs))
                |> Map.ofSeq

            let toFile (path : string) (tests : TestSession list) =
                sessionToFile "" path tests

            let ofFile (path : string) = sessionOfFile path