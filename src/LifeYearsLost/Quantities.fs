namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents sex categories used by the historical population data.
type Sex =
    | Male
    | Female

/// Represents age at irradiation, distinct from attained age.
type ExposureAge = private ExposureAge of float<year>

/// Represents age when an event may occur.
type AttainedAge = private AttainedAge of float<year>

/// Represents a non-negative absorbed dose, without an implicit RBE conversion.
type AbsorbedDose = private AbsorbedDose of float<Gy>

/// Represents survival probability relative to a specified starting cohort.
type SurvivalProbability = private SurvivalProbability of float

/// Represents remaining expected lifetime conditional on being alive now.
type RemainingLife = private RemainingLife of float<year>

/// Represents a non-negative excess relative hazard, HR minus one.
type ExcessHazardRatio = private ExcessHazardRatio of float

/// Represents a non-negative event rate per person-year, after cancelling people.
type AnnualRate = private AnnualRate of float<1 / year>

[<RequireQualifiedAccess>]
module ExposureAge =
    /// Creates an age at exposure measured in years.
    let create value =
        Validation.nonNegative "Exposure age" value
        |> Result.map ExposureAge

    /// Returns age at exposure in years.
    let value (ExposureAge value) =
        value

[<RequireQualifiedAccess>]
module AttainedAge =
    /// Creates an attained age measured in years.
    let create value =
        Validation.nonNegative "Attained age" value
        |> Result.map AttainedAge

    /// Returns attained age in years.
    let value (AttainedAge value) =
        value

[<RequireQualifiedAccess>]
module AbsorbedDose =
    /// Creates a finite non-negative absorbed dose.
    let create value =
        Validation.nonNegative "Absorbed dose" value
        |> Result.map AbsorbedDose

    /// Returns absorbed dose in gray.
    let value (AbsorbedDose value) =
        value

[<RequireQualifiedAccess>]
module SurvivalProbability =
    /// Creates a finite probability between zero and one.
    let create value =
        Validation.fraction "Survival probability" value
        |> Result.map SurvivalProbability

    /// Returns the dimensionless survival probability.
    let value (SurvivalProbability value) =
        value

[<RequireQualifiedAccess>]
module RemainingLife =
    /// Creates a non-negative remaining life expectancy in years.
    let create value =
        Validation.nonNegative "Remaining life" value
        |> Result.map RemainingLife

    /// Returns remaining life expectancy in years.
    let value (RemainingLife value) =
        value

[<RequireQualifiedAccess>]
module ExcessHazardRatio =
    /// Creates excess relative hazard without imposing a probability upper bound.
    let create value =
        Validation.nonNegative "Excess hazard ratio" value
        |> Result.map ExcessHazardRatio

    /// Returns excess relative hazard, which may exceed one.
    let value (ExcessHazardRatio value) =
        value

[<RequireQualifiedAccess>]
module AnnualRate =
    /// Creates a finite non-negative annual rate, distinct from annual probability.
    let create value =
        Validation.nonNegative "Annual rate" value
        |> Result.map AnnualRate

    /// Converts a registry rate per 100000 person-years to an annual rate.
    let fromPer100000 (value: float<person / year>) =
        create (
            value
            / 100000.0<person>
        )

    /// Returns the annual rate in inverse years.
    let value (AnnualRate value) =
        value

/// Represents total relative hazard HR=h_exposed/h_baseline, distinct from HR-1.
type HazardRatio = private HazardRatio of float

[<RequireQualifiedAccess>]
module HazardRatio =
    /// Creates a non-negative total hazard ratio, allowing protective associations.
    let create value =
        Validation.nonNegative "Hazard ratio" value
        |> Result.map HazardRatio

    /// Returns the dimensionless total hazard ratio.
    let value (HazardRatio value) =
        value

    /// Converts non-negative excess HR to total HR; retains Result for consistency with validated quantity constructors.
    let ofExcess excess =
        create (
            1.0
            + ExcessHazardRatio.value excess
        )

    /// Converts total relative hazard to excess relative hazard, rejecting protective effects in this non-negative model.
    let toExcess ratio =
        if value ratio < 1.0 then
            Error
                $"HR={value ratio} is protective. This non-negative excess-loss model requires HR >= 1; protective associations remain representable as HazardRatio but require a signed benefit/loss model. No clipping is applied."
        else
            ExcessHazardRatio.create (
                value ratio
                - 1.0
            )

/// Represents baseline incidence hazard in inverse years, never an annual probability.
type IncidenceHazard = private IncidenceHazard of AnnualRate

/// Represents baseline cause-specific mortality hazard in inverse years.
type MortalityHazard = private MortalityHazard of AnnualRate

/// Represents radiation-attributable excess incidence hazard in inverse years.
type ExcessIncidenceHazard = private ExcessIncidenceHazard of AnnualRate

/// Represents radiation-attributable excess mortality hazard in inverse years.
type ExcessMortalityHazard = private ExcessMortalityHazard of AnnualRate

/// Represents excess event hazards without erasing the incidence versus death distinction.
type ExcessEventHazard =
    | ExcessIncidence of ExcessIncidenceHazard
    | ExcessMortality of ExcessMortalityHazard

[<RequireQualifiedAccess>]
module IncidenceHazard =
    /// Creates a baseline incidence hazard in inverse years.
    let create value =
        AnnualRate.create value
        |> Result.map IncidenceHazard

    /// Returns the incidence hazard in inverse years.
    let value (IncidenceHazard rate) =
        AnnualRate.value rate

    /// Scales baseline incidence by total HR to obtain total exposed incidence.
    let exposed ratio baseline =
        create (
            HazardRatio.value ratio
            * value baseline
        )

[<RequireQualifiedAccess>]
module MortalityHazard =
    /// Creates a baseline cause-specific mortality hazard in inverse years.
    let create value =
        AnnualRate.create value
        |> Result.map MortalityHazard

    /// Returns the mortality hazard in inverse years.
    let value (MortalityHazard rate) =
        AnnualRate.value rate

    /// Scales baseline mortality by total HR to obtain total exposed mortality.
    let exposed ratio baseline =
        create (
            HazardRatio.value ratio
            * value baseline
        )

[<RequireQualifiedAccess>]
module ExcessIncidenceHazard =
    /// Implements Equation 1 for excess incidence rather than total incidence.
    let fromBaseline ratio baseline =
        AnnualRate.create (
            ExcessHazardRatio.value ratio
            * IncidenceHazard.value baseline
        )
        |> Result.map ExcessIncidenceHazard

    /// Converts total HR to excess incidence, explicitly rejecting protective associations.
    let fromHazardRatio ratio baseline =
        result {
            let! excess = HazardRatio.toExcess ratio
            return! fromBaseline excess baseline
        }

    /// Returns excess incidence hazard in inverse years.
    let value (ExcessIncidenceHazard rate) =
        AnnualRate.value rate

[<RequireQualifiedAccess>]
module ExcessMortalityHazard =
    /// Implements the excess mortality interpretation of Equation 4.
    let fromBaseline ratio baseline =
        AnnualRate.create (
            ExcessHazardRatio.value ratio
            * MortalityHazard.value baseline
        )
        |> Result.map ExcessMortalityHazard

    /// Converts total HR to excess mortality, explicitly rejecting protective associations.
    let fromHazardRatio ratio baseline =
        result {
            let! excess = HazardRatio.toExcess ratio
            return! fromBaseline excess baseline
        }

    /// Returns excess mortality hazard in inverse years.
    let value (ExcessMortalityHazard rate) =
        AnnualRate.value rate

[<RequireQualifiedAccess>]
module ExcessEventHazard =
    /// Returns the annual rate while retaining its event semantics in the enclosing value.
    let value =
        function
        | ExcessIncidence rate -> ExcessIncidenceHazard.value rate
        | ExcessMortality rate -> ExcessMortalityHazard.value rate

/// Represents expected years lost in a cohort, distinct from remaining life expectancy.
type ExpectedLifeYearsLost = private ExpectedLifeYearsLost of float<year>

[<RequireQualifiedAccess>]
module ExpectedLifeYearsLost =
    /// Creates a finite non-negative expected life loss in years.
    let create value =
        Validation.nonNegative "Expected life years lost" value
        |> Result.map ExpectedLifeYearsLost

    /// Returns expected life years lost in years.
    let value (ExpectedLifeYearsLost value) =
        value

/// Represents remaining life conditional on being alive at a declared attained age.
type RemainingLifeAtAge = private {
    Age: AttainedAge
    Life: RemainingLife
}

[<RequireQualifiedAccess>]
module RemainingLifeAtAge =
    /// Declares the conditioning age of a supplied remaining life expectancy; provenance must establish this interpretation.
    let create age life = { Age = age; Life = life }

    /// Returns the attained age on which the supplied remaining life is conditioned.
    let age input =
        input.Age

    /// Returns conditional remaining life in years.
    let value input =
        RemainingLife.value input.Life