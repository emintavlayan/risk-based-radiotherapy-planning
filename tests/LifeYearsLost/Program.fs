module LifeYearsLost.TestRunner

/// Runs the scientific checks and returns a failing process status when an assertion fails.
[<EntryPoint>]
let main _ =
    LifeYearsLost.Tests.run ()
    LifeYearsLost.Tests.reviewChecks ()
    0