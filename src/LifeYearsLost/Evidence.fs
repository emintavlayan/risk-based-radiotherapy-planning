namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents the statistical meaning established for a reported between-group comparison.
type ReportedAssociationMeasure =
    | CumulativeRiskRatio
    | HazardRatioMeasure
    | ExcessHazardRatioMeasure
    | UnspecifiedRiskComparison

/// Represents reported female-to-male evidence without assuming its statistical interpretation.
type ReportedSexComparison = private {
    FemaleRelativeToMale: float
    Measure: ReportedAssociationMeasure
    Source: string
}

/// Represents an explicit supplied multiplier of excess relative hazard, not a total hazard ratio.
type ExcessHazardMultiplier = private {
    Multiplier: float
    Source: string
}

[<RequireQualifiedAccess>]
module ReportedSexComparison =
    /// Creates a reported comparison with its source and explicitly identified or unresolved measure.
    let create femaleRelativeToMale measure source =
        result {
            let! ratio = Validation.nonNegative "Reported female-to-male comparison" femaleRelativeToMale

            do!
                Result.requireTrue
                    "A source for the reported comparison is required."
                    (not (System.String.IsNullOrWhiteSpace source))

            return {
                FemaleRelativeToMale = ratio
                Measure = measure
                Source = source
            }
        }

    /// Returns the reported female-to-male number without converting its meaning.
    let value comparison =
        comparison.FemaleRelativeToMale

    /// Returns the statistical meaning established from the source.
    let measure comparison =
        comparison.Measure

    /// Returns the reference for the reported comparison.
    let source (comparison: ReportedSexComparison) =
        comparison.Source

[<RequireQualifiedAccess>]
module ExcessHazardMultiplier =
    /// Creates an explicitly supplied excess-hazard multiplier with a source or declared assumption.
    let create multiplier source =
        result {
            let! multiplier = Validation.nonNegative "Excess-hazard multiplier" multiplier

            do!
                Result.requireTrue
                    "A source or declared assumption for the multiplier is required."
                    (not (System.String.IsNullOrWhiteSpace source))

            return {
                Multiplier = multiplier
                Source = source
            }
        }

    /// Returns the dimensionless multiplier without treating it as a hazard ratio.
    let value input =
        input.Multiplier

    /// Returns the source or declared assumption supporting the supplied multiplier.
    let source (input: ExcessHazardMultiplier) =
        input.Source

    /// Applies only an independently justified excess-HR multiplier for the target endpoint, sex and reference; a female/male comparison is not such a multiplier.
    /// Numeric validation and provenance do not verify applicability: callers must establish the target context before applying this formula.
    let apply input excess =
        ExcessHazardRatio.create (
            input.Multiplier
            * ExcessHazardRatio.value excess
        )