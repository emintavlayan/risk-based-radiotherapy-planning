module Server

open SAFE
open Saturn
open Shared
open RiskBasedRadiotherapyPlanning
open Fable.Remoting.Server

/// Creates the typed API around the pure calculation workflow.
let riskApi _ : IRiskApi = {
    getModels = fun () -> async { return NtcpModels.definitions () }
    evaluatePlan = fun input -> async { return RiskCalculation.evaluate input }
}

/// Logs an unexpected API failure while keeping internal details out of client responses.
let private handleApiError (error: exn) _ =
    eprintfn "Risk API failure: %O" error
    Ignore

/// Represents the typed SAFE remoting routes.
let webApp = Api.make (riskApi, errorHandler = handleApiError)

/// Represents the application host serving calculations and bundled client assets.
let app = application {
    use_router webApp
    memory_cache
    use_static "public"
    use_gzip
}

/// Starts the SAFE application host.
[<EntryPoint>]
let main _ =
    run app
    0
