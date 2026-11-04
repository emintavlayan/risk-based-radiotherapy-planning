module Shared.Tests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

/// Represents the shared infrastructure suite awaiting domain contracts.
let shared = testList "Shared infrastructure" []
