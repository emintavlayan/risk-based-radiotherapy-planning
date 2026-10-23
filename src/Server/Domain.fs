module RiskBasedRadiotherapyPlanning.Domain

open System
open FsToolkit.ErrorHandling
open Shared

/// Represents a validated mean organ dose in gray.
type DoseGy = private DoseGy of float

/// Represents a validated age at the plan creation date.
type AgeAtPlanCreation = private AgeAtPlanCreation of float

/// Represents an organ independently of TPS structure names.
type Organ = Heart | Lungs | Liver

/// Represents the supported published equation families.
type DoseResponse =
    | Linear of slopePerGy: float
    | Logistic of intercept: float * slopePerGy: float
    | MeanDoseLkb of td50Gy: float * m: float

/// Represents validated inputs to the pure calculation workflow.
type ValidatedPlan = {
    Label: string; Age: AgeAtPlanCreation; Sex: Sex; Doses: Map<Organ, DoseGy>
    CardiacRiskFactors: bool; Chemotherapy: bool
}

module DoseGy =
    /// Creates a finite mean dose in the supported input range.
    let create value =
        if Double.IsFinite value && value >= 0.0 && value <= 50.0 then Ok (DoseGy value)
        else Error "Enter a finite mean dose between 0 and 50 Gy."

    /// Returns a validated dose in gray.
    let value (DoseGy dose) = dose

module AgeAtPlanCreation =
    /// Creates a finite age anchored to the plan creation date.
    let create value =
        if Double.IsFinite value && value >= 0.0 && value <= 110.0 then Ok (AgeAtPlanCreation value)
        else Error "Age at plan creation must be between 0 and 110 years."

module Validation =
    /// Validates every external field before evaluating a model.
    let plan (input: PlanInput) =
        result {
            do! (not (String.IsNullOrWhiteSpace input.PlanLabel)) |> Result.requireTrue "Enter a plan label."
            do! (input.PlanLabel.Length <= 80) |> Result.requireTrue "Plan label must be at most 80 characters."
            let! age = AgeAtPlanCreation.create input.AgeAtPlanCreation
            let! heart = DoseGy.create input.MeanHeartDoseGy |> Result.mapError (fun e -> "Heart: " + e)
            let! lung = DoseGy.create input.MeanLungDoseGy |> Result.mapError (fun e -> "Lungs: " + e)
            let! liver = DoseGy.create input.MeanLiverDoseGy |> Result.mapError (fun e -> "Liver: " + e)
            return {
                Label = input.PlanLabel.Trim(); Age = age; Sex = input.Sex
                Doses = Map [Heart, heart; Lungs, lung; Liver, liver]
                CardiacRiskFactors = input.CardiacRiskFactors; Chemotherapy = input.Chemotherapy
            }
        }

module DoseResponse =
    /// Evaluates the standard normal CDF using Abramowitz-Stegun 7.1.26 with absolute error below 1e-7.
    let normalCdf value =
        let x = abs value / sqrt 2.0
        let t = 1.0 / (1.0 + 0.3275911 * x)
        let polynomial = (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t
        let erf = 1.0 - polynomial * exp (-x * x)
        0.5 * (1.0 + (if value < 0.0 then -erf else erf))

    /// Evaluates a published equation without unreported clinical modifiers.
    let evaluate response doseGy =
        match response with
        | Linear slope -> slope * doseGy
        | Logistic (intercept, slope) -> 1.0 / (1.0 + exp (-(intercept + slope * doseGy)))
        | MeanDoseLkb (td50, m) -> normalCdf ((doseGy - td50) / (m * td50))
