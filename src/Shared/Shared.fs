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
    AvailableOarIds: string list
    CardiacRiskFactors: bool
    Chemotherapy: bool
}

/// Represents a named published coefficient with explicit units.
type ModelParameter = { Symbol: string; Value: float; Unit: string; Description: string }

/// Represents the interpretation of a model's numerical output.
type RiskOutputKind = AbsoluteProbability | RelativeRisk | OddsRatio | BaselineAdjustedIncidence | NoRiskEstimate

/// Represents whether the catalogue entry is ready for server evaluation.
type ModelAvailability = Implemented | RequiresImplementation | MissingParameters | NoDoseResponse

/// Represents a typed requirement for a future ESAPI/DVH or clinical-data input.
type ModelInputKind = MeanDoseGy | DoseAtVolumePercent of float | DifferentialDvh | VolumeCm3 | ClinicalVariable of string | BaselineIncidence

/// Represents one required metric with a canonical OAR identifier.
type ModelInput = { OarId: string; Kind: ModelInputKind; Description: string }

/// Represents a source-linked model definition displayed by the catalogue.
type ModelDefinition = {
    Id: string
    Organ: string
    OarId: string
    Region: string
    OutputKind: RiskOutputKind
    Availability: ModelAvailability
    Inputs: ModelInput list
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

/// Represents a scalar measurement with a canonical OAR identifier and typed interpretation.
type MetricMeasurement = { OarId: string; Kind: ModelInputKind; Value: float }

/// Represents a differential DVH bin with fractional organ volume (not percent).
type DvhBin = { DoseGy: float; VolumeFraction: float }

/// Represents an organ differential DVH with bins summing to unity.
type OrganDvh = { OarId: string; Bins: DvhBin list }

/// Represents all explicit measurements required to evaluate one catalogue entry.
type CatalogueEvaluationInput = { ModelId: string; Measurements: MetricMeasurement list; Histograms: OrganDvh list }

/// Represents one numerical result with its original risk semantics and an optional dose-response.
type CatalogueEvaluation = {
    ModelId: string
    OutputKind: RiskOutputKind
    Value: float
    DosePredictorGy: float option
    DosePredictorLabel: string option
    Curve: CurvePoint list
}

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
    evaluateCatalogueModel: CatalogueEvaluationInput -> Async<Result<CatalogueEvaluation, string>>
}

module Example =
    /// Represents synthetic patient data loaded on startup for the demonstration.
    let plan = {
        PlanLabel = "Example plan"; AgeAtPlanCreation = 42.0; Sex = Female
        MeanHeartDoseGy = 18.0; MeanLungDoseGy = 14.0; MeanLiverDoseGy = 26.0
        AvailableOarIds = [ "heart"; "lungs"; "liver" ]
        CardiacRiskFactors = false; Chemotherapy = true
    }
