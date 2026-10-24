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
    testCase "Gamma-50 logistic has fifty percent at published D50 for all six models" <| fun _ ->
        for id, organ, d50 in [
            "thyroid-vogelius-2011", "thyroid", 33.6
            "neck-weakness-vanleeuwen-2012", "neck-muscles", 27.2
            "heart-valvular-schellong-2010", "heart", 40.1
            "heart-morbidity-schellong-2010", "heart", 48.2
            "kidney-injury-cassady-1995", "kidneys", 27.84 ] do
            let request = { ModelId = id; Measurements = [ { OarId = organ; Kind = MeanDoseGy; Value = d50 } ]; Histograms = [] }
            let got = CatalogueCalculations.evaluate request |> requireOk
            Expect.isLessThan (abs (got.Value - 0.5)) 1e-12 "The logistic D50 should produce 50%."
        let spared = { ModelId = "parotid-houweling-2010"
                       Measurements = [ { OarId = "parotids"; Kind = SparedGlandMeanDoseGy; Value = 32.4 } ]
                       Histograms = [] }
        let got = CatalogueCalculations.evaluate spared |> requireOk
        Expect.isLessThan (abs (got.Value - 0.5)) 1e-12 "Spared-gland model should be 50% at D50."

    testCase "Multivariable Beetz endpoint uses all covariates" <| fun _ ->
        let req = { ModelId = "parotid-beetz-2011"
                    Measurements = [
                        { OarId = "parotids"; Kind = MeanDoseGy; Value = 25.0 }
                        { OarId = ""; Kind = ClinicalVariable "age at treatment"; Value = 60.0 }
                        { OarId = ""; Kind = ClinicalVariable "baseline xerostomia score"; Value = 1.0 } ]
                    Histograms = [] }
        let response = CatalogueCalculations.evaluate req |> requireOk
        let independent = 1.0 / (1.0 + exp (-(-5.27 + 25.0*0.066 + 60.0*0.050 + 0.916)))
        Expect.isLessThan (abs (response.Value - independent)) 1e-12 "Beetz coefficients should agree with direct regression."

    testCase "Thyroid volume and heart D10 change their respective multivariable risks" <| fun _ ->
        let thyroid volume = { ModelId = "thyroid-boomsma-2012"; Measurements = [
                                  { OarId = "thyroid"; Kind = MeanDoseGy; Value = 25.0 }
                                  { OarId = "thyroid"; Kind = VolumeCm3; Value = volume } ]; Histograms = [] }
        let small = CatalogueCalculations.evaluate (thyroid 5.0) |> requireOk
        let large = CatalogueCalculations.evaluate (thyroid 20.0) |> requireOk
        Expect.isGreaterThan small.Value large.Value "Boomsma should decrease as gland volume increases."
        let lung heartD10 = { ModelId = "lung-pneumonitis-huang-2011"; Measurements = [
                                  { OarId = "lungs"; Kind = MeanDoseGy; Value = 20.0 }
                                  { OarId = "heart"; Kind = DoseAtVolumePercent 10.0; Value = heartD10 } ]; Histograms = [] }
        let lower = CatalogueCalculations.evaluate (lung 5.0) |> requireOk
        let higher = CatalogueCalculations.evaluate (lung 30.0) |> requireOk
        Expect.isGreaterThan higher.Value lower.Value "Huang should increase with heart D10."

    testCase "LKB full differential DVH equals D50 for uniform irradiation" <| fun _ ->
        for id, organ, d50 in [
            "larynx-edema-rancati-2009", "larynx", 47.3
            "esophagus-esophagitis-belderbos-2005", "esophagus", 47.0
            "small-bowel-toxicity-eich-2005", "small-bowel", 32.0 ] do
            let bins = [ { DoseGy = d50; VolumeFraction = 0.3 }
                         { DoseGy = d50; VolumeFraction = 0.7 } ]
            let req = { ModelId = id; Measurements = []; Histograms = [ { OarId = organ; Bins = bins } ] }
            let result = CatalogueCalculations.evaluate req |> requireOk
            Expect.isLessThan (abs (result.Value - 0.5)) 1e-7 "Uniform DVH at TD50 must give 50%."
        let invalid = { ModelId = "larynx-edema-rancati-2009"; Measurements = []
                        Histograms = [ { OarId = "larynx"; Bins = [ { DoseGy = 40.0; VolumeFraction = 0.8 } ] } ] }
        Expect.isError (CatalogueCalculations.evaluate invalid) "Non-unit DVHs must fail."

    testCase "Logit power is zero at zero dose and fifty percent at D50" <| fun _ ->
        let request dose = { ModelId = "larynx-dysphagia-rancati-2010"; Measurements = [
                                 { OarId = "supraglottic-larynx"; Kind = MeanDoseGy; Value = dose } ]; Histograms = [] }
        let zero = CatalogueCalculations.evaluate (request 0.0) |> requireOk
        let mid = CatalogueCalculations.evaluate (request 57.5) |> requireOk
        Expect.equal zero.Value 0.0 "Logit model reaches zero at zero dose."
        Expect.isLessThan (abs (mid.Value - 0.5)) 1e-12 "Logit model must have exact D50."

    testCase "Dose-related RR and OR are never reported as NTCP probabilities" <| fun _ ->
        for id, organ, slope, kind in [
            "lung-cancer-travis-2002", "lungs", 0.141, RelativeRisk
            "breast-cancer-travis-2003", "breast", 0.149, RelativeRisk
            "esophagus-cancer-morton-2012", "esophagus", 0.09, OddsRatio
            "stomach-cancer-vandenbelt-2009", "stomach", 0.84, RelativeRisk ] do
            let req = { ModelId = id; Measurements = [ { OarId = organ; Kind = MeanDoseGy; Value = 10.0 } ]; Histograms = [] }
            let output = CatalogueCalculations.evaluate req |> requireOk
            Expect.equal output.OutputKind kind "Risk measure must remain unchanged."
            Expect.isLessThan (abs (output.Value - (1.0 + slope*10.0))) 1e-12 "Published linear slope."
            Expect.isEmpty output.Curve "Relative measures must not appear on probability axes."

    testCase "Missing age, baseline and DVH are rejected instead of silently defaulted" <| fun _ ->
        let parotid = { ModelId = "parotid-beetz-2011"
                        Measurements = [ { OarId = "parotids"; Kind = MeanDoseGy; Value = 15.0 } ]
                        Histograms = [] }
        Expect.isError (CatalogueCalculations.evaluate parotid) "No synthetic default age."
        let lung = { ModelId = "lung-pneumonitis-huang-2011"
                     Measurements = [ { OarId = "lungs"; Kind = MeanDoseGy; Value = 15.0 } ]
                     Histograms = [] }
        Expect.isError (CatalogueCalculations.evaluate lung) "Heart D10 is required."
        Expect.isError (CatalogueCalculations.evaluate { ModelId = "small-bowel-toxicity-eich-2005"; Measurements = []; Histograms = [] }) "No mean-dose fallback."
        Expect.isError (CatalogueCalculations.evaluate { ModelId = "esophagus-stricture-alevronta-2010"; Measurements = []; Histograms = [] }) "Unverified relative seriality must remain blocked."


    testCase "Brodin Table 1 inventory includes all 29 entries" <| fun _ ->
        let definitions = Catalogue.definitions ()
        Expect.equal definitions.Length 29 "The paper contains 29 model and evidence entries."
        Expect.equal (definitions |> List.map _.Id |> Set.ofList |> Set.count) 29 "Model identifiers must be unique."
        Expect.equal (definitions |> List.map _.OarId |> Set.ofList |> Set.count) 17 "Model catalogue must cover 17 named OAR categories."

    testCase "Risk output type and readiness are explicit for every catalogue entry" <| fun _ ->
        let definitions = Catalogue.definitions ()
        let ready = definitions |> List.filter (fun m -> m.Availability = Implemented)
        Expect.equal ready.Length 21 "Eighteen newly specified models plus the original three must be executable."
        let notCurves = definitions |> List.filter (fun m -> m.Availability = NoDoseResponse)
        Expect.equal notCurves.Length 3 "Three entries have no patient-specific dose-response model."
        Expect.isTrue (definitions |> List.exists (fun m -> m.OutputKind = RelativeRisk)) "RR entries are preserved without conversion."
        Expect.isTrue (definitions |> List.exists (fun m -> m.OutputKind = OddsRatio)) "OR entry is preserved without conversion."

    testCase "Canonical OAR filtering returns multiple endpoints for the same structure" <| fun _ ->
        let heart = Catalogue.forOrgans [ "heart" ]
        Expect.equal heart.Length 3 "The catalogue records three heart endpoints."
        Expect.isTrue (heart |> List.forall (fun m -> m.OarId = "heart")) "Filter must use the canonical OAR ID."
        let thyroid = Catalogue.forOrgans [ "thyroid" ]
        Expect.equal thyroid.Length 3 "The catalogue records three thyroid models."

    testCase "Only available OARs are evaluated, and excluded models cannot appear in curves" <| fun _ ->
        let input = { Example.plan with AvailableOarIds = [ "lungs" ] }
        let evaluation = RiskCalculation.evaluate input |> requireOk
        Expect.equal (evaluation.Estimates |> List.map _.ModelId) [ "lung-quantec-marks" ] "Only lung should be evaluated."
        let none = RiskCalculation.evaluate { Example.plan with AvailableOarIds = [] } |> requireOk
        Expect.isEmpty none.Estimates "No selected OAR means no calculated curve."
        let invalid = RiskCalculation.evaluate { Example.plan with AvailableOarIds = [ "unrecognized" ] }
        Expect.isError invalid "Unknown OARs must not be silently accepted."


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
