module LifeYearsLost.Tests

open LifeYearsLost
open FsToolkit.ErrorHandling

/// Extracts a successful research fixture or fails the test immediately.
let private ok = function Ok value -> value | Error error -> failwith error

/// Checks an expected error without depending on implementation error wording.
let private rejects name result =
    match result with Error _ -> () | Ok _ -> failwith $"{name}: expected Error."

/// Checks a numeric result against an absolute tolerance.
let private near name expected actual =
    if abs (expected - actual) > 1e-10 then failwith $"{name}: expected {expected}, got {actual}."

/// Checks a scientific invariant with an explicit test name.
let private check name condition =
    if not condition then failwith name

/// Creates a validated absorbed-dose fixture.
let private dose value = AbsorbedDose.create value |> ok

/// Creates a validated remaining-life fixture.
let private life value = RemainingLife.create value |> ok

/// Creates a validated attained-age fixture.
let private age value = AttainedAge.create value |> ok

/// Creates a validated survival-probability fixture.
let private survival value = SurvivalProbability.create value |> ok

/// Creates the Figure 2 evidence with the published life-expectancy difference of 18.6 years.
let private figureEvidence attained = {
    Endpoint = Linear LungCancer
    Sex = Female
    Age = age attained
    SurvivorSurvival = survival 0.54
    Consequence = IncidentEvent (IncidenceHazard.create 0.0013<1/year> |> ok, life 20.0<year>, life 1.4<year>)
}

/// Runs scientific regression and boundary tests using independent expected quantities.
let run () =
    let exposure = ExposureAge.create 4.0<year> |> ok
    // Figure transcription only, not a general sex-adjustment rule.
    let figureRatio = ExcessHazardRatio.create (0.194 * 9.4 * 1.21) |> ok
    let unmodifiedRatio = ExcessHazardRatio.create (0.194 * 9.4) |> ok
    let figure = LifeLoss.atAge exposure Female (Linear LungCancer) figureRatio (figureEvidence 60.0<year>) |> ok

    near "Unrounded Figure 2 excess ratio" 2.206556 (ExcessHazardRatio.value figure.ExcessRatio)
    near "Figure 2 excess incidence hazard" 0.0028685228 (ExcessEventHazard.value figure.ExcessEventHazard |> float)
    near "Figure 2 survival-weighted rate" (0.0028685228 * 0.54) (AnnualRate.value figure.SurvivalWeightedExcessRate |> float)
    near "Figure 2 printed staged rounding" 0.0279 (0.0015 * 18.6)
    near "Figure 2 density from unrounded inputs" (0.194 * 9.4 * 1.21 * 0.0013 * 0.54 * 18.6) (LifeLossDensity.value figure.Density)
    check "Unrounded Figure 2 inputs round to 0.029 rather than the printed 0.028" (System.Math.Round(LifeLossDensity.value figure.Density, 3) = 0.029)

    let excess = ExcessHazardRatio.create 2.0 |> ok
    let total = HazardRatio.ofExcess excess |> ok
    near "Total HR includes baseline" 3.0 (HazardRatio.value total)
    near "HR to excess round trip" 2.0 (HazardRatio.toExcess total |> ok |> ExcessHazardRatio.value)
    let baseline = IncidenceHazard.create 0.01<1/year> |> ok
    near "Total incidence uses HR" 0.03 (IncidenceHazard.exposed total baseline |> ok |> IncidenceHazard.value |> float)
    near "Excess incidence uses HR minus one" 0.02 (ExcessIncidenceHazard.fromBaseline excess baseline |> ok |> ExcessIncidenceHazard.value |> float)
    let noExcess = HazardRatio.create 1.0 |> ok |> HazardRatio.toExcess |> ok
    near "HR one gives zero excess" 0.0 (ExcessIncidenceHazard.fromBaseline noExcess baseline |> ok |> ExcessIncidenceHazard.value |> float)
    rejects "Protective HR is outside this excess-loss model" (HazardRatio.create 0.8 |> ok |> HazardRatio.toExcess)
    near "Registry per-100000 conversion" 0.0013 (AnnualRate.fromPer100000 130.0<person/year> |> ok |> AnnualRate.value |> float)
    check "A hazard is not constrained to probability range" (AnnualRate.create 2.0<1/year> |> Result.isOk)
    rejects "Probability greater than one" (SurvivalProbability.create 1.1)
    rejects "NaN hazard" (IncidenceHazard.create (System.Double.NaN * 1.0<1/year>))
    rejects "Infinite dose" (AbsorbedDose.create (System.Double.PositiveInfinity * 1.0<Gy>))
    rejects "Negative exposure age" (ExposureAge.create -1.0<year>)

    for endpoint, slope in [BreastCancer, 0.149; LungCancer, 0.194; StomachCancer, 0.84; HeartFailure, 0.117; MyocardialInfarction, 0.096] do
        let ratio = Brodin2012.unmodifiedExcessHazardRatio exposure (MeanOrganDose (endpoint, dose 10.0<Gy>)) |> ok
        near $"Table 1 {endpoint}" (slope * 10.0) (ExcessHazardRatio.value ratio)
        let zero = Brodin2012.unmodifiedExcessHazardRatio exposure (MeanOrganDose (endpoint, dose 0.0<Gy>)) |> ok
        near $"Zero dose {endpoint}" 0.0 (ExcessHazardRatio.value zero)

    let fractional = DifferentialDvh.ofFractions [{ Dose = dose 0.0<Gy>; Fraction = 0.5 }; { Dose = dose 20.0<Gy>; Fraction = 0.5 }] |> ok
    let absolute = DifferentialDvh.ofAbsoluteVolumes [{ Dose = dose 0.0<Gy>; Volume = 10.0<cm3> }; { Dose = dose 20.0<Gy>; Volume = 10.0<cm3> }] |> ok
    let scaled = DifferentialDvh.ofAbsoluteVolumes [{ Dose = dose 0.0<Gy>; Volume = 100.0<cm3> }; { Dose = dose 20.0<Gy>; Volume = 100.0<cm3> }] |> ok
    /// Evaluates the thyroid response for a test histogram.
    let thyroid dvh = Brodin2012.unmodifiedExcessHazardRatio exposure (ThyroidDvh dvh) |> ok |> ExcessHazardRatio.value
    near "Thyroid direct voxel average" (0.5 * 2.21 * 20.0 * exp (-0.00164 * 400.0)) (thyroid fractional)
    near "Absolute and fractional DVH equivalence" (thyroid fractional) (thyroid absolute)
    near "Organ-volume scale invariance" (thyroid fractional) (thyroid scaled)
    let homogeneous = DifferentialDvh.ofFractions [{ Dose = dose 10.0<Gy>; Fraction = 1.0 }] |> ok
    check "Thyroid nonlinear response cannot use mean-dose shortcut" (abs (thyroid homogeneous - thyroid fractional) > 0.1)
    near "Mean DVH dose" 10.0 (DifferentialDvh.meanDose fractional |> ok |> AbsorbedDose.value |> float)
    rejects "Incomplete fractional DVH" (DifferentialDvh.ofFractions [{ Dose = dose 1.0<Gy>; Fraction = 0.5 }])
    rejects "Empty DVH" (DifferentialDvh.ofFractions [])
    rejects "Zero organ volume" (DifferentialDvh.ofAbsoluteVolumes [{ Dose = dose 1.0<Gy>; Volume = 0.0<cm3> }])
    rejects "Thyroid adult extrapolation" (Brodin2012.thyroidParameters (ExposureAge.create 20.0<year> |> ok))

    let hfEvidence = { figureEvidence 60.0<year> with Endpoint = Linear HeartFailure; Consequence = FatalHeartFailure (MortalityHazard.create 0.001<1/year> |> ok, life 20.0<year>) }
    let hf = LifeLoss.atAge exposure Female (Linear HeartFailure) (ExcessHazardRatio.create 1.17 |> ok) hfEvidence |> ok
    near "Equation 4 uses excess mortality, not all baseline mortality" (0.54 * 1.17 * 0.001 * 20.0) (LifeLossDensity.value hf.Density)
    rejects "HF incidence cannot enter mortality pathway" (LifeLoss.atAge exposure Female (Linear HeartFailure) (ExcessHazardRatio.create 1.17 |> ok) { hfEvidence with Consequence = (figureEvidence 60.0<year>).Consequence })
    rejects "Population endpoint mismatch" (LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio hfEvidence)
    rejects "Population sex mismatch" (LifeLoss.atAge exposure Male (Linear LungCancer) unmodifiedRatio (figureEvidence 60.0<year>))
    rejects "Event before irradiation" (LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio (figureEvidence 3.0<year>))
    rejects "Negative life shortening" (LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio { figureEvidence 60.0<year> with Consequence = IncidentEvent (baseline, life 1.0<year>, life 2.0<year>) })

    let window = IntegrationWindow.create exposure 1.0<year> (age 15.0<year>) |> ok
    /// Evaluates a constant synthetic age-specific evidence grid.
    let sample attained = LifeLoss.atAge exposure Female (Linear LungCancer) figureRatio (figureEvidence attained) |> ok
    let constantDensity = LifeLossDensity.value figure.Density
    near "Constant integrand over ten years" (constantDensity * 10.0) (LifeLoss.integrate window [sample 5.0<year>; sample 15.0<year>] |> ok |> ExpectedLifeYearsLost.value |> float)
    near "Irregular age grid gives same constant integral" (constantDensity * 10.0) (LifeLoss.integrate window [sample 5.0<year>; sample 7.0<year>; sample 15.0<year>] |> ok |> ExpectedLifeYearsLost.value |> float)
    let alternative = LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio (figureEvidence 15.0<year>) |> ok
    rejects "Mixed dose-response contexts" (LifeLoss.integrate window [sample 5.0<year>; alternative])
    rejects "Missing lower bound" (LifeLoss.integrate window [sample 6.0<year>; sample 15.0<year>])
    rejects "Duplicate ages" (LifeLoss.integrate window [sample 5.0<year>; sample 5.0<year>; sample 15.0<year>])
    rejects "Unsorted ages" (LifeLoss.integrate window [sample 5.0<year>; sample 9.0<year>; sample 7.0<year>; sample 15.0<year>])
    rejects "Reversed bounds" (IntegrationWindow.create exposure 5.0<year> (age 8.0<year>))
    near "Conditional survival area" 5.0 (LifeLoss.remainingLifeFromSurvival [0.0<year>, survival 1.0; 10.0<year>, survival 0.0] |> ok |> RemainingLife.value |> float)
    rejects "Restricted mean is not full life expectancy" (LifeLoss.remainingLifeFromSurvival [0.0<year>, survival 1.0; 10.0<year>, survival 0.2])
    rejects "Increasing survival" (LifeLoss.remainingLifeFromSurvival [0.0<year>, survival 1.0; 1.0<year>, survival 0.5; 2.0<year>, survival 0.6; 3.0<year>, survival 0.0])
    printfn "Scientific domain checks passed."
