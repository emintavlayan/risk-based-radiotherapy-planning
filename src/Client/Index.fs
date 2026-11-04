module Index

open Elmish
open Feliz

/// Represents the initial application screen before domain modelling.
type Model = Ready

/// Represents the infrastructure-only message set.
type Msg = Refresh

/// Creates the initial screen without scientific inputs or calculations.
let init () = Ready, Cmd.none

/// Preserves the initial screen until workflows are defined.
let update Refresh model = model, Cmd.none

/// Renders the application foundation.
let view _ _ =
    Html.main [
        prop.className "workspace"
        prop.children [
            Html.header [
                Html.h1 "Risk-Based Radiotherapy Planning"
                Html.span [ prop.className "status"; prop.text "Work in progress" ]
            ]
            Html.section [
                Html.h2 "Life years lost"
                Html.p "The application foundation is ready. Scientific terminology, equations and domain types will be developed and reviewed before calculation workflows are implemented."
            ]
        ]
    ]
