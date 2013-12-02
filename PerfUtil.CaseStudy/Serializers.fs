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


    type SerializerComparer =

        static member Create (?throwOnError) =
            let this = new FSPSerializer() :> ISerializer
            let others = [ new BFSerializer() :> ISerializer ; new NDCSerializer() :> _ ]
            new ImplemantationComparer<ISerializer>(this, others, ?throwOnError = throwOnError) :> PerformanceTester<ISerializer>