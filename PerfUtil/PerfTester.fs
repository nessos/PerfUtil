namespace PerfUtil

    /// inherit this interface to describe an abstracted test scenario
    type ITestable =
        abstract ImplementationName : string

    type IPerformanceTester<'Testable when 'Testable :> ITestable> =
        abstract TestedImplementation : 'Testable
        abstract Test : testId:string -> testF:('Testable -> unit) -> unit