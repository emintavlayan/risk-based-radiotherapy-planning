module Shared.Tests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

/// Represents the shared contract suite, with numerical checks owned by the server tests.
let shared = testList "Shared contracts" []
