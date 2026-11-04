namespace LifeYearsLost

/// Represents absorbed radiation dose in gray.
[<Measure>] type Gy
/// Represents elapsed time in years.
[<Measure>] type year
/// Represents absolute organ volume in cubic centimetres.
[<Measure>] type cm3
/// Represents people in registry denominators, distinct from probabilities.
[<Measure>] type person

[<RequireQualifiedAccess>]
module Validation =
    /// Checks finite measured values against a lower bound.
    let nonNegative name (value: float<'u>) =
        if System.Double.IsFinite(float value) && value >= 0.0<_> then Ok value
        else Error $"{name} must be finite and non-negative."

    /// Checks a dimensionless probability or fractional volume.
    let fraction name value =
        if System.Double.IsFinite value && value >= 0.0 && value <= 1.0 then Ok value
        else Error $"{name} must be finite and between zero and one."
