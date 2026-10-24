module RiskBasedRadiotherapyPlanning.RiskCalculation

open FsToolkit.ErrorHandling
open Shared
open RiskBasedRadiotherapyPlanning.Domain
open RiskBasedRadiotherapyPlanning.NtcpModels

/// Calculates a plan marker and curve from the same published equation.
let private endpoint (plan: ValidatedPlan) (model: NtcpModel) =
    result {
        let! dose = Map.tryFind model.Organ plan.Doses |> Result.requireSome "A required organ dose is missing."
        let d = DoseGy.value dose
        let p = DoseResponse.evaluate model.Response d
        do! (System.Double.IsFinite p && p >= 0.0 && p <= 1.0) |> Result.requireTrue "The model returned an invalid probability."
        return {
            ModelId = model.Definition.Id; DoseGy = d; Probability = p
            ReductionForOneGy = p - DoseResponse.evaluate model.Response (max 0.0 (d - 1.0))
            Curve = [ for i in 0 .. 250 do
                        let x = float i / 5.0
                        yield { DoseGy = x; Probability = DoseResponse.evaluate model.Response x } ]
        }
    }

/// Validates a plan and evaluates only implemented models with an available OAR.
let evaluate input : Result<PlanEvaluation, string> =
    result {
        let! validated = Validation.plan input
        let selectedModels = NtcpModels.all |> List.filter (fun model -> Set.contains model.Definition.OarId validated.AvailableOars)
        let! estimates = selectedModels |> List.traverseResultM (endpoint validated)
        return { PlanLabel = validated.Label; Estimates = estimates }
    }
