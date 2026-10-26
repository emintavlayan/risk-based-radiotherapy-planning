module Client.Tests

open Fable.Mocha
open State
open Shared

/// Represents client state checks for request ordering and screen navigation.
let client = testList "Risk calculator state" [
    testCase "Adaptive detail expands to fit a 30 Gy lung marker and its observed probability" <| fun _ ->
        let bounds = plotBounds true [ { ModelId = "lung"; DoseGy = 30.0; Probability = 0.4775; ReductionForOneGy = 0.0; Curve = [] } ]
        Expect.isTrue (bounds.MaxDoseGy > 30.0) "Dose marker must remain inside the x-axis."
        Expect.isTrue (bounds.MaxProbability > 0.4775) "Risk marker must remain inside the y-axis."

    testCase "Adaptive detail contracts axes for the current example without hiding markers" <| fun _ ->
        let example = [ { ModelId = "lung"; DoseGy = 14.0; Probability = 0.1085; ReductionForOneGy = 0.0; Curve = [] }
                        { ModelId = "liver"; DoseGy = 26.0; Probability = 0.0019; ReductionForOneGy = 0.0; Curve = [] } ]
        let bounds = plotBounds true example
        Expect.equal bounds.MaxDoseGy 30.0 "Example dose axis remains useful at 30 Gy."
        Expect.isTrue (abs (bounds.MaxProbability - 0.15) < 1e-12) "Example risk axis adapts to 15 percent."

    testCase "Full axes remain fixed and comparable between plan revisions" <| fun _ ->
        let bounds = plotBounds false [ { ModelId = "lung"; DoseGy = 30.0; Probability = 0.4775; ReductionForOneGy = 0.0; Curve = [] } ]
        Expect.equal bounds.MaxDoseGy 50.0 "Full dose axis must remain fixed."
        Expect.equal bounds.MaxProbability 1.0 "Full risk axis must remain at 100 percent."

    testCase "Focused and detailed layouts preserve patient inputs and evaluation state" <| fun _ ->
        let initial, _ = init ()
        Expect.equal initial.DisplayMode ClinicalFocus "New design opens the focused view."
        let edited, _ = update (SetForm { initial.Form with Lung = "30" }) initial
        let detailed, _ = update (SetDisplayMode ResearchDetail) edited
        let focused, _ = update (SetDisplayMode ClinicalFocus) detailed
        Expect.equal detailed.Form.Lung "30" "Research detail must use the same edited plan."
        Expect.equal focused.Form edited.Form "Returning to clinical focus must not reset the plan."
        Expect.equal focused.RequestId edited.RequestId "Presentation change must not trigger recomputation."


    testCase "Available OAR selectors change the server request" <| fun _ ->
        let original = exampleForm ()
        let changed = toggleAvailableOar "heart" original
        Expect.isFalse (List.contains "heart" changed.AvailableOars) "Heart must be excluded."
        match input changed with
        | Ok request -> Expect.equal request.AvailableOarIds changed.AvailableOars "Active organs must be sent to the server."
        | Error err -> failtest err


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
let all = testList "All" [ client ]

/// Runs client tests through the template's Mocha harness.
[<EntryPoint>]
let main _ = Mocha.runTests all
