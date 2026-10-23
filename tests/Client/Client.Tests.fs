module Client.Tests

open Fable.Mocha
open State
open Shared

/// Represents client state checks for request ordering and screen navigation.
let client = testList "Risk calculator state" [
    testCase "A stale response cannot replace the current plan" <| fun _ ->
        let model, _ = init ()
        let current, _ = update (SetForm { model.Form with Heart = "20" }) model
        let stale, _ = update (Calculated (model.RequestId, Error "obsolete response")) current
        Expect.equal stale current "Only the most recent request may update results."

    testCase "Model navigation preserves edited patient and plan data" <| fun _ ->
        let model, _ = init ()
        let edited, _ = update (SetForm { model.Form with Label = "Edited plan"; Heart = "22" }) model
        let selected, _ = update (SelectModel "lung-quantec-marks") edited
        let returned, _ = update (Navigate Calculator) selected
        Expect.equal returned.Form edited.Form "Catalogue navigation must preserve the form."
        Expect.equal selected.Page Models "Selecting a model should open the catalogue."

    testCase "Empty numeric input is an error rather than zero" <| fun _ ->
        let form = { exampleForm () with Heart = "" }
        match input form with
        | Error _ -> ()
        | Ok _ -> failtest "An empty dose must not be accepted."

    testCase "Reset example restores the complete synthetic input" <| fun _ ->
        let model, _ = init ()
        let edited, _ = update (SetForm { model.Form with Age = "60"; Chemotherapy = false }) model
        let reset, _ = update ResetExample edited
        Expect.equal reset.Form (exampleForm ()) "Reset must restore all example inputs."
]

/// Represents the complete client test suite.
let all = testList "All" [ Shared.Tests.shared; client ]

/// Runs client tests through the template's Mocha harness.
[<EntryPoint>]
let main _ = Mocha.runTests all
