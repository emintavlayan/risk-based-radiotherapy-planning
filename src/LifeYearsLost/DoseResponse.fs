namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents validated coefficients for a linear-exponential-quadratic dose response.
type ThyroidParameters = private {
    LinearSlope: float<1/Gy>
    QuadraticFalloff: float<1/Gy^2>
}

[<RequireQualifiedAccess>]
module ThyroidParameters =
    /// Validates supplied response coefficients independently of their epidemiological derivation.
    let create slope falloff = result {
        let! slope = Validation.nonNegative "Thyroid linear slope" slope
        let! falloff = Validation.nonNegative "Thyroid quadratic falloff" falloff
        return { LinearSlope = slope; QuadraticFalloff = falloff }
    }
    /// Returns the supplied ascending dose-response slope in inverse gray.
    let linearSlope parameters = parameters.LinearSlope
    /// Returns the supplied quadratic falloff in inverse gray squared.
    let quadraticFalloff parameters = parameters.QuadraticFalloff

[<RequireQualifiedAccess>]
module DoseResponse =
    /// Calculates excess relative hazard from a supplied non-negative slope and mean organ dose.
    let linear (slope: float<1/Gy>) dose = result {
        let! slope = Validation.nonNegative "Linear response slope" slope
        return! ExcessHazardRatio.create (slope * AbsorbedDose.value dose)
    }

    /// Averages a supplied nonlinear response over differential organ-volume fractions.
    let thyroid parameters dvh =
        DifferentialDvh.bins dvh
        |> List.sumBy (fun bin ->
            let d = AbsorbedDose.value bin.Dose
            bin.Fraction * parameters.LinearSlope * d * exp (-(parameters.QuadraticFalloff * d * d)))
        |> ExcessHazardRatio.create
