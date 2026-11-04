namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents endpoints whose Table 1 response is linear in mean organ dose.
type LinearEndpoint = BreastCancer | LungCancer | StomachCancer | HeartFailure | MyocardialInfarction
/// Represents all six endpoints without permitting a mean-dose shortcut for thyroid.
type Endpoint = Linear of LinearEndpoint | ThyroidCancer
/// Represents the dosimetric descriptor required by each response family.
type DoseResponseInput = MeanOrganDose of LinearEndpoint * AbsorbedDose | ThyroidDvh of DifferentialDvh
/// Represents typed thyroid response parameters transcribed from Table 1.
type ThyroidParameters = { LinearSlope: float<1/Gy>; QuadraticFalloff: float<1/Gy^2> }

[<RequireQualifiedAccess>]
module Brodin2012 =
    /// Identifies the publication used for this research implementation.
    let sourceDoi = "10.1002/cncr.27536"

    /// Returns Table 1 slopes in inverse gray, with no automatic sex adjustment.
    let linearSlope = function
        | BreastCancer -> 0.149<1/Gy>
        | LungCancer -> 0.194<1/Gy>
        | StomachCancer -> 0.84<1/Gy>
        | HeartFailure -> 0.117<1/Gy>
        | MyocardialInfarction -> 0.096<1/Gy>

    /// Returns the reported female-to-male ratios as evidence, not ready-to-use multipliers.
    let reportedFemaleToMaleRatio = function
        | Linear BreastCancer -> None
        | Linear LungCancer | ThyroidCancer -> Some 1.7
        | Linear StomachCancer -> Some 3.7
        | Linear HeartFailure -> Some 1.4
        | Linear MyocardialInfarction -> Some 0.6

    /// Restricts Table 1 transcription to the paper's treated ages of four through fifteen.
    let thyroidParameters exposureAge = result {
        let age = ExposureAge.value exposureAge
        do! Result.requireTrue "Thyroid age extrapolation is unreviewed; supported study ages are 4-15 years." (age >= 4.0<year> && age <= 15.0<year>)
        return { LinearSlope = 2.81<1/Gy> - 0.15<1/Gy/year> * age; QuadraticFalloff = 0.00164<1/Gy^2> }
    }

    /// Evaluates the dimensionless thyroid response at one dose before volume averaging.
    let private thyroidAtDose parameters dose =
        let d = AbsorbedDose.value dose
        parameters.LinearSlope * d * exp (-(parameters.QuadraticFalloff * d * d))

    /// Calculates the unmodified Table 1 response; unresolved sex normalization is deliberately excluded.
    let unmodifiedExcessHazardRatio exposureAge input = result {
        let! unmodified =
            match input with
            | MeanOrganDose (endpoint, dose) ->
                ExcessHazardRatio.create (linearSlope endpoint * AbsorbedDose.value dose)
            | ThyroidDvh dvh -> result {
                let! parameters = thyroidParameters exposureAge
                let value = DifferentialDvh.bins dvh |> List.sumBy (fun bin -> bin.Fraction * thyroidAtDose parameters bin.Dose)
                return! ExcessHazardRatio.create value
              }
        return unmodified
    }
