namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents endpoints whose Table 1 response is linear in mean organ dose.
type LinearEndpoint =
    | BreastCancer
    | LungCancer
    | StomachCancer
    | HeartFailure
    | MyocardialInfarction

/// Represents all six endpoints without permitting a mean-dose shortcut for thyroid.
type Endpoint =
    | Linear of LinearEndpoint
    | ThyroidCancer

/// Represents the dosimetric descriptor required by each response family.
type DoseResponseInput =
    | MeanOrganDose of LinearEndpoint * AbsorbedDose
    | ThyroidDvh of DifferentialDvh

[<RequireQualifiedAccess>]
module Brodin2012 =
    /// Identifies the publication used for this research implementation.
    let sourceDoi = "10.1002/cncr.27536"

    /// Returns Table 1 slopes in inverse gray, with no automatic sex adjustment.
    let linearSlope =
        function
        | BreastCancer -> 0.149<1 / Gy>
        | LungCancer -> 0.194<1 / Gy>
        | StomachCancer -> 0.84<1 / Gy>
        | HeartFailure -> 0.117<1 / Gy>
        | MyocardialInfarction -> 0.096<1 / Gy>

    /// Returns the reported female-to-male ratios as evidence, not ready-to-use multipliers.
    let private reportedFemaleToMaleRatio =
        function
        | Linear BreastCancer -> None
        | Linear LungCancer
        | ThyroidCancer -> Some 1.7
        | Linear StomachCancer -> Some 3.7
        | Linear HeartFailure -> Some 1.4
        | Linear MyocardialInfarction -> Some 0.6

    /// Preserves Table 1 sex comparisons as evidence with unresolved statistical meaning.
    let reportedSexComparison endpoint =
        match reportedFemaleToMaleRatio endpoint with
        | None -> Ok None
        | Some ratio ->
            ReportedSexComparison.create
                ratio
                UnspecifiedRiskComparison
                (sourceDoi
                 + ", Table 1")
            |> Result.map Some

    /// Evaluates the printed age formula where its coefficients remain non-negative, without asserting clinical validity.
    let thyroidParameters exposureAge =
        let age = ExposureAge.value exposureAge

        let maximumAge =
            2.81
            / 0.15
            * 1.0<year>

        if age > maximumAge then
            Error
                $"Brodin 2012 thyroid age formula requires exposure age <= {float maximumAge:F6} years; received {float age:F6}. The printed slope becomes negative above this limit. Supply independently established ThyroidParameters to DoseResponse.thyroid for another age model."
        else
            ThyroidParameters.create
                (2.81<1 / Gy>
                 - 0.15<1 / Gy / year>
                   * age)
                0.00164<1 / Gy^2>

    /// Calculates unmodified published responses; supplied modifiers and source interpretation remain separate.
    let unmodifiedExcessHazardRatio exposureAge input =
        match input with
        | MeanOrganDose(endpoint, dose) -> DoseResponse.linear (linearSlope endpoint) dose
        | ThyroidDvh dvh ->
            result {
                let! parameters = thyroidParameters exposureAge
                return! DoseResponse.thyroid parameters dvh
            }