module Server.Tests

open System
open Expecto
open Shared
open RiskBasedRadiotherapyPlanning
open RiskBasedRadiotherapyPlanning.Domain

/// Extracts a successful workflow result or fails the current test with its error.
let private requireOk = function
    | Ok value -> value
    | Error error -> failtest error

/// Compares an evaluated endpoint with an independent numerical reference.
let private assertProbability id expected tolerance input =
    let estimate = (RiskCalculation.evaluate input |> requireOk).Estimates |> List.find (fun value -> value.ModelId = id)
    Expect.isLessThan (abs (estimate.Probability - expected)) tolerance "Probability should agree with the independent reference."

/// Represents numerical and validation checks for the published calculation workflow.
let server = testList "NTCP calculations" [
    testCase "Linear cardiac model agrees with the published slope" <| fun _ ->
        assertProbability "heart-hancock-brodin" 0.0189 1e-12 Example.plan

    testCase "QUANTEC logistic lung model agrees with independent reference" <| fun _ ->
        assertProbability "lung-quantec-marks" 0.10851501928151981 1e-12 Example.plan

    testCase "LKB liver model is fifty percent at TD50" <| fun _ ->
        assertProbability "liver-quantec-pan" 0.5 1e-7 { Example.plan with MeanLiverDoseGy = 39.8 }

    testCase "LKB liver model agrees with the standard normal CDF at z=-1" <| fun _ ->
        assertProbability "liver-quantec-pan" 0.158655253931457 1e-7 { Example.plan with MeanLiverDoseGy = 39.8 * (1.0 - 0.12) }

    testCase "Normal CDF agrees with independent tabulated values" <| fun _ ->
        for z, expected in [ -2.0, 0.0227501319481792; 0.0, 0.5; 1.0, 0.841344746068543; 2.0, 0.977249868051821 ] do
            Expect.isLessThan (abs (DoseResponse.normalCdf z - expected)) 1e-7 "CDF should agree with the reference."

    testCase "Plan markers and curve samples use the same equation" <| fun _ ->
        let result = RiskCalculation.evaluate Example.plan |> requireOk
        for estimate in result.Estimates do
            let curvePoint = estimate.Curve |> List.find (fun point -> point.DoseGy = estimate.DoseGy)
            Expect.equal curvePoint.Probability estimate.Probability "A marker must lie exactly on its curve."

    testCase "Clinical factors do not introduce unreported effects into dose-only models" <| fun _ ->
        let original = RiskCalculation.evaluate Example.plan |> requireOk
        let modified = RiskCalculation.evaluate { Example.plan with CardiacRiskFactors = true; Chemotherapy = false; AgeAtPlanCreation = 75.0; Sex = Male } |> requireOk
        Expect.equal modified.Estimates original.Estimates "These models are dose-only."

    testCase "Negative and non-finite doses are rejected" <| fun _ ->
        for dose in [ -1.0; Double.NaN; Double.PositiveInfinity; Double.NegativeInfinity; 50.1 ] do
            Expect.isError (RiskCalculation.evaluate { Example.plan with MeanHeartDoseGy = dose }) "Invalid mean dose must return Error."

    testCase "Missing labels and invalid ages are rejected" <| fun _ ->
        Expect.isError (RiskCalculation.evaluate { Example.plan with PlanLabel = " " }) "Blank label must return Error."
        Expect.isError (RiskCalculation.evaluate { Example.plan with AgeAtPlanCreation = Double.NaN }) "Non-finite age must return Error."
        Expect.isError (RiskCalculation.evaluate { Example.plan with AgeAtPlanCreation = 111.0 }) "Unsupported age must return Error."

    testCase "A zero-dose reduction does not create a negative dose" <| fun _ ->
        let result = RiskCalculation.evaluate { Example.plan with MeanHeartDoseGy = 0.0; MeanLungDoseGy = 0.0; MeanLiverDoseGy = 0.0 } |> requireOk
        for estimate in result.Estimates do
            Expect.equal estimate.ReductionForOneGy 0.0 "The boundary at zero dose has no one-gray reduction."

    testCase "All sampled curves are finite monotone probabilities" <| fun _ ->
        let result = RiskCalculation.evaluate Example.plan |> requireOk
        for estimate in result.Estimates do
            for point in estimate.Curve do
                Expect.isTrue (Double.IsFinite point.Probability && point.Probability >= 0.0 && point.Probability <= 1.0) "Samples must be probabilities."
            for left, right in List.pairwise estimate.Curve do
                Expect.isLessThanOrEqual left.Probability right.Probability "These three models must be monotone."
]

/// Represents the complete server test suite.
let all = testList "All" [ Shared.Tests.shared; server ]

/// Runs the numerical and validation tests.
[<EntryPoint>]
let main args = runTestsWithCLIArgs [] args all
