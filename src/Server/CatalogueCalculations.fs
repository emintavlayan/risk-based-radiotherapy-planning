module RiskBasedRadiotherapyPlanning.CatalogueCalculations

open System
open FsToolkit.ErrorHandling
open Shared
open RiskBasedRadiotherapyPlanning.Domain
open RiskBasedRadiotherapyPlanning.NtcpModels

/// Represents the published model IDs with unambiguous evaluable dose-response mathematics.
let executableIds =
    Set.ofList [
        "parotid-houweling-2010"; "parotid-beetz-2011"; "submandibular-beetz-2011"
        "thyroid-vogelius-2011"; "thyroid-boomsma-2012"; "neck-weakness-vanleeuwen-2012"
        "larynx-edema-rancati-2009"; "larynx-dysphagia-rancati-2010"
        "esophagus-esophagitis-belderbos-2005"; "esophagus-cancer-morton-2012"
        "lung-pneumonitis-huang-2011"; "lung-cancer-travis-2002"
        "breast-cancer-travis-2003"; "heart-valvular-schellong-2010"
        "heart-morbidity-schellong-2010"; "stomach-cancer-vandenbelt-2009"
        "small-bowel-toxicity-eich-2005"; "kidney-injury-cassady-1995"
    ]

/// Evaluates a stable logistic sigmoid without numerical overflow.
let logistic value =
    if value >= 0.0 then 1.0 / (1.0 + exp (-value))
    else let x = exp value in x / (1.0 + x)

/// Evaluates the published γ50 logistic family with dose and D50 in Gy.
let gammaLogistic d50 gamma dose =
    logistic (4.0 * gamma * (dose / d50 - 1.0))

/// Evaluates the dose-ratio logit model, distinct from the γ50 logistic function.
let logitPower d50 k dose =
    if dose <= 0.0 then 0.0
    else 1.0 / (1.0 + (d50 / dose) ** k)

/// Evaluates a generalised equivalent uniform dose for a normalized differential DVH.
let equivalentDose n (bins: DvhBin list) =
    bins |> List.sumBy (fun bin -> bin.VolumeFraction * bin.DoseGy ** (1.0 / n)) |> fun total -> total ** n

/// Evaluates an LKB response from a gEUD with source-specific TD50 and m.
let lkb td50 m effectiveDose =
    DoseResponse.normalCdf ((effectiveDose - td50) / (m * td50))

/// Resolves exactly one measurement and rejects missing, duplicate or invalid units/values.
let private metric oar kind (request: CatalogueEvaluationInput) =
    let matches = request.Measurements |> List.filter (fun item -> item.OarId = oar && item.Kind = kind)
    let key = if oar = "" then sprintf "%A" kind else sprintf "%s %A" oar kind
    match matches with
    | [ measurement ] when Double.IsFinite measurement.Value && measurement.Value >= 0.0 ->
        match kind with
        | ClinicalVariable name when name.StartsWith "baseline" && measurement.Value <> 0.0 && measurement.Value <> 1.0 ->
            Error (key + " must be explicitly coded 0 or 1.")
        | ClinicalVariable "age at treatment" when measurement.Value > 110.0 ->
            Error "Age must be between 0 and 110 years."
        | VolumeCm3 when measurement.Value <= 0.0 ->
            Error (key + " must be positive.")
        | MeanDoseGy | SparedGlandMeanDoseGy | DoseAtVolumePercent _ when measurement.Value > 150.0 ->
            Error (key + " must not exceed 150 Gy.")
        | _ -> Ok measurement.Value
    | [] -> Error ("Missing required input: " + key)
    | [ _ ] -> Error ("Invalid numeric measurement: " + key)
    | _ -> Error ("Duplicate numeric measurement: " + key)

/// Retrieves exactly one differential DVH with finite physical dose and unit-sum fractional volumes.
let private histogram oar (request: CatalogueEvaluationInput) =
    let matches = request.Histograms |> List.filter (fun h -> h.OarId = oar)
    match matches with
    | [] -> Error ("Missing full differential DVH for " + oar)
    | [ dvh ] ->
        let bins = dvh.Bins
        let total = bins |> List.sumBy _.VolumeFraction
        if List.isEmpty bins then Error ("Empty DVH for " + oar)
        elif bins |> List.exists (fun bin -> not (Double.IsFinite bin.DoseGy && Double.IsFinite bin.VolumeFraction) || bin.DoseGy < 0.0 || bin.DoseGy > 150.0 || bin.VolumeFraction <= 0.0 || bin.VolumeFraction > 1.0) then
            Error ("Invalid dose or differential volume fraction for " + oar)
        elif abs (total - 1.0) > 1e-6 then
            Error (sprintf "Differential DVH volume fractions for %s sum to %g, not 1." oar total)
        else Ok bins
    | _ -> Error ("Duplicate full differential DVH for " + oar)

/// Builds an endpoint-specific curve by varying its named dose predictor and holding other covariates fixed.
let private response id output predictor label f =
    let value = f predictor
    let curve =
        if output = AbsoluteProbability then
            [ for d in 0 .. 160 -> { DoseGy = float d / 2.0; Probability = f (float d / 2.0) } ]
        else []
    if not (Double.IsFinite value) || (output = AbsoluteProbability && (value < 0.0 || value > 1.0)) then
        Error ("Non-finite or invalid model output for " + id)
    elif curve |> List.exists (fun p -> not (Double.IsFinite p.Probability) || p.Probability < 0.0 || p.Probability > 1.0) then
        Error ("Invalid curve sample for " + id)
    else
        Ok { ModelId = id; OutputKind = output; Value = value
             DosePredictorGy = Some predictor; DosePredictorLabel = Some label; Curve = curve }

/// Evaluates one logistic γ50 model using an explicitly named dose statistic.
let private withGamma id oar kind d50 gamma label request =
    result {
        let! dose = metric oar kind request
        return! response id AbsoluteProbability dose label (gammaLogistic d50 gamma)
    }

/// Evaluates a differential-DVH LKB model without replacing EUD with mean dose.
let private withLkb id organ td50 m n request =
    result {
        let! bins = histogram organ request
        let effective = equivalentDose n bins
        return! response id AbsoluteProbability effective ("gEUD (" + organ + "), Gy") (lkb td50 m)
    }

/// Evaluates linear relative risks and odds ratios on their native scales.
let private withRelative id oar slope kind request =
    result {
        let! dose = metric oar MeanDoseGy request
        return! response id kind dose ("Mean dose (" + oar + "), Gy") (fun d -> 1.0 + slope * d)
    }

/// Evaluates Beetz patient-rated xerostomia, retaining all three published covariates.
let private parotidBeetz id request =
    result {
        let! d = metric "parotids" MeanDoseGy request
        let! age = metric "" (ClinicalVariable "age at treatment") request
        let! baseline = metric "" (ClinicalVariable "baseline xerostomia score") request
        return! response id AbsoluteProbability d "Mean parotid dose (Gy)" (fun x ->
            logistic (-5.27 + 0.066 * x + 0.050 * age + 0.916 * baseline))
    }

/// Evaluates Beetz sticky saliva using two distinct salivary-gland doses and two clinical factors.
let private stickyBeetz id request =
    result {
        let! d = metric "submandibular" MeanDoseGy request
        let! sublingual = metric "sublingual" MeanDoseGy request
        let! age = metric "" (ClinicalVariable "age at treatment") request
        let! baseline = metric "" (ClinicalVariable "baseline sticky-saliva score") request
        return! response id AbsoluteProbability d "Mean submandibular dose (Gy)" (fun x ->
            logistic (-10.70 + 0.091 * x + 0.107 * age + 1.218 * baseline - 0.041 * sublingual))
    }

/// Evaluates the Boomsma hypothyroidism model with organ volume in cubic centimetres.
let private thyroidBoomsma id request =
    result {
        let! d = metric "thyroid" MeanDoseGy request
        let! volume = metric "thyroid" VolumeCm3 request
        return! response id AbsoluteProbability d "Mean thyroid dose (Gy)" (fun x ->
            logistic (0.011 + 0.062 * x - 0.19 * volume))
    }

/// Evaluates the Huang pneumonitis model requiring both mean lung dose and heart D10%.
let private lungHuang id request =
    result {
        let! d = metric "lungs" MeanDoseGy request
        let! heartD10 = metric "heart" (DoseAtVolumePercent 10.0) request
        return! response id AbsoluteProbability d "Mean lung dose (Gy)" (fun x ->
            logistic (-3.5 + 0.0649 * x + 0.0234 * heartD10))
    }

/// Evaluates the Rancati dysphagia dose-ratio logit on the supraglottic larynx, not whole larynx.
let private larynxDysphagia id request =
    result {
        let! dose = metric "supraglottic-larynx" MeanDoseGy request
        return! response id AbsoluteProbability dose "Mean supraglottic larynx dose (Gy)" (logitPower 57.5 6.57)
    }

/// Runs one mathematically specified Table 1 model with all required measurements validated.
let evaluate (request: CatalogueEvaluationInput) : Result<CatalogueEvaluation, string> =
    let id = request.ModelId
    match id with
    | "parotid-houweling-2010" -> withGamma id "parotids" SparedGlandMeanDoseGy 32.4 0.97 "Mean spared parotid dose (Gy)" request
    | "thyroid-vogelius-2011" -> withGamma id "thyroid" MeanDoseGy 33.6 0.58 "Mean thyroid dose (Gy)" request
    | "neck-weakness-vanleeuwen-2012" -> withGamma id "neck-muscles" MeanDoseGy 27.2 0.56 "Mean neck muscle dose (Gy)" request
    | "heart-valvular-schellong-2010" -> withGamma id "heart" MeanDoseGy 40.1 4.01 "Mean heart dose (Gy)" request
    | "heart-morbidity-schellong-2010" -> withGamma id "heart" MeanDoseGy 48.2 1.36 "Mean heart dose (Gy)" request
    | "kidney-injury-cassady-1995" -> withGamma id "kidneys" MeanDoseGy 27.84 1.924 "Mean kidney dose (Gy)" request
    | "parotid-beetz-2011" -> parotidBeetz id request
    | "submandibular-beetz-2011" -> stickyBeetz id request
    | "thyroid-boomsma-2012" -> thyroidBoomsma id request
    | "lung-pneumonitis-huang-2011" -> lungHuang id request
    | "larynx-dysphagia-rancati-2010" -> larynxDysphagia id request
    | "larynx-edema-rancati-2009" -> withLkb id "larynx" 47.3 0.23 1.17 request
    | "esophagus-esophagitis-belderbos-2005" -> withLkb id "esophagus" 47.0 0.36 0.69 request
    | "small-bowel-toxicity-eich-2005" -> withLkb id "small-bowel" 32.0 0.17 0.09 request
    | "esophagus-cancer-morton-2012" -> withRelative id "esophagus" 0.09 OddsRatio request
    | "lung-cancer-travis-2002" -> withRelative id "lungs" 0.141 RelativeRisk request
    | "breast-cancer-travis-2003" -> withRelative id "breast" 0.149 RelativeRisk request
    | "stomach-cancer-vandenbelt-2009" -> withRelative id "stomach" 0.84 RelativeRisk request
    | "heart-hancock-brodin" | "lung-quantec-marks" | "liver-quantec-pan" ->
        result {
            let model = NtcpModels.all |> List.find (fun item -> item.Definition.Id = id)
            let organId = model.Definition.OarId
            let! dose = metric organId MeanDoseGy request
            return! response id AbsoluteProbability dose ("Mean " + organId + " dose (Gy)") (DoseResponse.evaluate model.Response)
        }
    | _ -> Error ("This model has not been validated for computation: " + id)
