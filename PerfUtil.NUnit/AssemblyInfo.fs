namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("PerfUtil.NUnit")>]
[<assembly: AssemblyProductAttribute("PerfUtil.NUnit")>]
[<assembly: AssemblyDescriptionAttribute("A simple F# utility for testing performance")>]
[<assembly: AssemblyVersionAttribute("0.1.7")>]
[<assembly: AssemblyFileVersionAttribute("0.1.7")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.7"
