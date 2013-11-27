namespace PerfUtil

    /// inherit this interface to describe an abstracted test scenario
    type ITestable =
        abstract ImplementationName : string

    [<AbstractClass>]
    type PerformanceTester<'Testable when 'Testable :> ITestable> (testedImplementation : 'Testable) =

        member __.TestedImplementation = testedImplementation
        abstract Test : testId:string -> testF:('Testable -> unit) -> unit