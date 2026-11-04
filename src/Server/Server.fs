module Server

open SAFE
open Saturn
open Shared
open Fable.Remoting.Server

/// Returns the infrastructure status without performing scientific calculations.
let applicationApi _ : IApplicationApi = {
    getStatus = fun () -> async {
        return { Name = "Risk-Based Radiotherapy Planning"; Stage = "Domain modelling pending" }
    }
}

/// Logs unexpected infrastructure failures.
let private handleApiError (error: exn) _ =
    eprintfn "Application API failure: %O" error
    Ignore

/// Represents the infrastructure remoting routes.
let webApp = Api.make (applicationApi, errorHandler = handleApiError)

/// Represents the SAFE host and bundled client assets.
let app = application {
    use_router webApp
    memory_cache
    use_static "public"
    use_gzip
}

/// Starts the application host.
[<EntryPoint>]
let main _ =
    run app
    0
