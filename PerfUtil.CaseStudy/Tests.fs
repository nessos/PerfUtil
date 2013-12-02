namespace PerfUtil.CaseStudy

    open System.IO
    open PerfUtil

    module Tests =

        type Marker = class end

        let roundtrip (times : int)  (x : 'T) (s : ISerializer) =
            // use a single stream for all tests to eliminate memorystream allocation impact
            use m = new MemoryStream()
            for i = 1 to times do
                s.Serialize m x
                m.Position <- 0L
                let x' = s.Deserialize<'T> m
                m.Position <- 0L

        [<PerfTest>]
        let ``int list`` (s : ISerializer) = roundtrip 1000 [1..1000] s


        let values = [1 .. 1000] |> List.map string

        [<PerfTest>]
        let ``string list`` (s : ISerializer) = roundtrip 1000 values s


        let array3D = Array3D.init 100 100 100 (fun i j k -> float (i * j + k))

        [<PerfTest>]
        let ``float [,,]`` s = roundtrip 10 array3D s

        let objArray = [| obj() ; box "string" ; box (Some 53) ; box 2 |]

        [<PerfTest>]
        let ``obj []`` s = roundtrip 1000 objArray s


        let arrayDU = [| for i in 1 .. 10000 -> (Some ("lorem ipsum" + string i, i)) |]

        [<PerfTest>]
        let ``(string * int) option []`` s =  roundtrip 100 arrayDU s

        
        type BinTree<'T> = Leaf | Node of 'T * BinTree<'T> * BinTree<'T>

        let rec mkTree d =
            if d = 0 then Leaf
            else
                let b1 = mkTree (d-1)
                let b2 = mkTree (d-1)
                Node(d, b1, b2)


        let tree = mkTree 10

        [<PerfTest>]
        let ``Binary Tree`` s = roundtrip 100 tree s


        let largeQuotation =
            <@
                let rec fibAsync n =
                    async {
                        match n with
                        | _ when n < 0 -> return invalidArg "n" "negative inputs not supported."
                        | _ when n <= 1 -> return n
                        | n ->
                            let! fn = fibAsync (n-1)
                            let! fnn = fibAsync (n-2)
                            return fn + fnn
                    }

                fibAsync 42
            @>

        [<PerfTest>]
        let ``Large Quotation`` s = roundtrip 1000 largeQuotation s