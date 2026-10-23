namespace Shared

/// Represents the recorded sex supplied with a plan.
type Sex = Female | Male | NotRecorded

/// Represents patient and plan inputs accepted by the server.
type PlanInput = {
    PlanLabel: string
    AgeAtPlanCreation: float
    Sex: Sex
    MeanHeartDoseGy: float
    MeanLungDoseGy: float
    MeanLiverDoseGy: float
    CardiacRiskFactors: bool
    Chemotherapy: bool
}

/// Represents a named published coefficient with explicit units.
type ModelParameter = { Symbol: string; Value: float; Unit: string; Description: string }

/// Represents a source-linked model definition displayed by the catalogue.
type ModelDefinition = {
    Id: string
    Organ: string
    Endpoint: string
    Name: string
    Family: string
    Color: string
    Version: string
    Description: string
    Formula: string
    Parameters: ModelParameter list
    RequiredMetric: string
    Population: string
    DoseBasis: string
    TimeHorizon: string
    Limitations: string
    Citation: string
    PaperUrl: string
    ParameterSourceUrl: string
}

/// Represents one sampled point on a dose-response curve.
type CurvePoint = { DoseGy: float; Probability: float }

/// Represents a plan marker and the curve evaluated using the same equation.
type EndpointEstimate = {
    ModelId: string
    DoseGy: float
    Probability: float
    ReductionForOneGy: float
    Curve: CurvePoint list
}

/// Represents one evaluated plan without combining distinct clinical endpoints.
type PlanEvaluation = { PlanLabel: string; Estimates: EndpointEstimate list }

/// Represents the typed SAFE API for server-owned calculations.
type IRiskApi = {
    getModels: unit -> Async<ModelDefinition list>
    evaluatePlan: PlanInput -> Async<Result<PlanEvaluation, string>>
}

module Example =
    /// Represents synthetic patient data loaded on startup for the demonstration.
    let plan = {
        PlanLabel = "Example plan"; AgeAtPlanCreation = 42.0; Sex = Female
        MeanHeartDoseGy = 18.0; MeanLungDoseGy = 14.0; MeanLiverDoseGy = 26.0
        CardiacRiskFactors = false; Chemotherapy = true
    }
