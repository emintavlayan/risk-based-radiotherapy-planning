module Server.Tests

open Expecto

/// Represents the shared infrastructure suite awaiting scientific workflows.
let all = testList "Application foundation" [ Shared.Tests.shared ]

/// Runs the server test harness.
[<EntryPoint>]
let main args = runTestsWithCLIArgs [] args all
