namespace PerfUtil.CaseStudy

    open FsPickler
    open PerfUtil

    open System.IO
    open System.Runtime.Serialization
    open System.Runtime.Serialization.Formatters.Binary

    type ISerializer =
        inherit ITestable

        abstract Serialize<'T> : Stream -> 'T -> unit
        abstract Deserialize<'T> : Stream -> 'T

    type BFSerializer () =
        let bfs = new BinaryFormatter()

        interface ISerializer with
            member __.Name = "BinaryFormatter"
            member __.Serialize (stream : Stream) (t : 'T) = bfs.Serialize(stream, t)
            member __.Deserialize (stream : Stream) = bfs.Deserialize(stream) :?> 'T

    type NDCSerializer () =
        let ndc = new NetDataContractSerializer()

        interface ISerializer with
            member __.Name = "NetDataContractSerializer"
            member __.Serialize (stream : Stream) (t : 'T) = ndc.Serialize(stream, t)
            member __.Deserialize (stream : Stream) = ndc.Deserialize(stream) :?> 'T

    type FSPSerializer () =
        let fsp = new FsPickler()

        interface ISerializer with
            member __.Name = "FsPickler"
            member __.Serialize (stream : Stream) (t : 'T) = fsp.Serialize(stream, t)
            member __.Deserialize (stream : Stream) = fsp.Deserialize<'T>(stream)


    type SerializationPerf =

        static member CreateImplementationComparer (?throwOnError, ?warmup) =
            let this = new FSPSerializer() :> ISerializer
            let others = [ new BFSerializer() :> ISerializer ; new NDCSerializer() :> _ ]
            let comparer = new WeightedComparer(spaceFactor = 0.2, leastAcceptableImprovementFactor = 1.)
            new ImplementationComparer<ISerializer>(this, others, comparer = comparer, ?warmup = warmup, ?throwOnError = throwOnError)

        static member CreatePastVersionComparer (historyFile, ?throwOnError, ?warmup) =
            let this = new FSPSerializer () :> ISerializer
            let version = typeof<FsPickler.FsPickler>.Assembly.GetName().Version
            let comparer = new WeightedComparer(spaceFactor = 0.2, leastAcceptableImprovementFactor = 0.7)
            new PastImplementationComparer<ISerializer>(
                    this, version, historyFile = historyFile, comparer = comparer, ?warmup = warmup, ?throwOnError = throwOnError)
