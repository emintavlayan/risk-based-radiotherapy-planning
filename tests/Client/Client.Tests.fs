module Client.Tests

open Fable.Mocha

/// Represents the empty infrastructure suite awaiting domain workflows.
let all = testList "Application foundation" []

/// Runs the client test harness.
[<EntryPoint>]
let main _ = Mocha.runTests all
