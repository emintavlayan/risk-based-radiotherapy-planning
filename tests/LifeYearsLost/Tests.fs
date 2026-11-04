module LifeYearsLost.Tests

open LifeYearsLost
open FsToolkit.ErrorHandling

/// Extracts a successful research fixture or fails the test immediately.
let private ok =
    function
    | Ok value -> value
    | Error error -> failwith error

/// Checks an expected error without depending on implementation error wording.
let private rejects name result =
    match result with
    | Error _ -> ()
    | Ok _ -> failwith $"{name}: expected Error."

/// Checks a numeric result against an absolute tolerance.
let private near name expected actual =
    if
        not (System.Double.IsFinite actual)
        || abs (
            expected
            - actual
        ) > 1e-10
    then
        failwith $"{name}: expected {expected}, got {actual}."

/// Checks a scientific invariant with an explicit test name.
let private check name condition =
    if not condition then
        failwith name

/// Creates a validated absorbed-dose fixture.
let private dose value =
    AbsorbedDose.create value
    |> ok

/// Creates a validated remaining-life fixture.
let private life value =
    RemainingLife.create value
    |> ok

/// Creates a validated attained-age fixture.
let private age value =
    AttainedAge.create value
    |> ok

/// Creates a validated survival-probability fixture.
let private survival value =
    SurvivalProbability.create value
    |> ok

/// Creates an explicitly age-conditioned remaining-life fixture.
let private conditionalLife attained value =
    RemainingLifeAtAge.create (age attained) (life value)

/// Creates synthetic incidence evidence independently of the paper example.
let private syntheticEvidence attained = {
    Endpoint = Linear LungCancer
    Sex = Female
    Age = age attained
    SurvivorSurvival = survival 0.5
    Consequence =
        IncidentEvent(
            IncidenceHazard.create 0.01<1 / year>
            |> ok,
            conditionalLife attained 20.0<year>,
            conditionalLife attained 5.0<year>
        )
}

/// Runs scientific regression and boundary tests using independent expected quantities.
let run () =
    let exposure =
        ExposureAge.create 4.0<year>
        |> ok

    let suppliedMultiplier =
        ExcessHazardMultiplier.create 1.5 "Synthetic test input; not a published estimate."
        |> ok

    let unmodifiedRatio =
        ExcessHazardRatio.create 2.0
        |> ok

    let adjustedRatio =
        ExcessHazardMultiplier.apply suppliedMultiplier unmodifiedRatio
        |> ok

    let incidenceLoss =
        LifeLoss.atAge exposure Female (Linear LungCancer) adjustedRatio (syntheticEvidence 60.0<year>)
        |> ok

    near "Supplied multiplier scales excess HR" 3.0 (ExcessHazardRatio.value adjustedRatio)

    near
        "Synthetic excess incidence hazard"
        0.03
        (ExcessEventHazard.value incidenceLoss.ExcessEventHazard
         |> float)

    near
        "Synthetic survival-weighted incidence"
        0.015
        (AnnualRate.value incidenceLoss.SurvivalWeightedExcessRate
         |> float)

    near "Synthetic loss density" 0.225 (LifeLossDensity.value incidenceLoss.Density)
    rejects "Multiplier requires provenance" (ExcessHazardMultiplier.create 1.5 "")
    rejects "Negative multiplier" (ExcessHazardMultiplier.create -1.0 "Synthetic assumption.")

    let reported =
        Brodin2012.reportedSexComparison (Linear LungCancer)
        |> ok
        |> Option.get

    check
        "Reported comparison does not claim hazard-ratio semantics"
        (ReportedSexComparison.measure reported = UnspecifiedRiskComparison)

    let suppliedSlope = 0.2<1 / Gy>

    near
        "Supplied linear slope"
        2.0
        (DoseResponse.linear suppliedSlope (dose 10.0<Gy>)
         |> ok
         |> ExcessHazardRatio.value)

    rejects "Negative response coefficient" (DoseResponse.linear -0.2<1 / Gy> (dose 10.0<Gy>))
    rejects "Negative nonlinear falloff" (ThyroidParameters.create 1.0<1 / Gy> -0.01<1 / Gy^2>)

    let excess =
        ExcessHazardRatio.create 2.0
        |> ok

    let total =
        HazardRatio.ofExcess excess
        |> ok

    near "Total HR includes baseline" 3.0 (HazardRatio.value total)

    near
        "HR to excess round trip"
        2.0
        (HazardRatio.toExcess total
         |> ok
         |> ExcessHazardRatio.value)

    let baseline =
        IncidenceHazard.create 0.01<1 / year>
        |> ok

    near
        "Total incidence uses HR"
        0.03
        (IncidenceHazard.exposed total baseline
         |> ok
         |> IncidenceHazard.value
         |> float)

    near
        "Excess incidence uses HR minus one"
        0.02
        (ExcessIncidenceHazard.fromBaseline excess baseline
         |> ok
         |> ExcessIncidenceHazard.value
         |> float)

    let noExcess =
        HazardRatio.create 1.0
        |> ok
        |> HazardRatio.toExcess
        |> ok

    near
        "HR one gives zero excess"
        0.0
        (ExcessIncidenceHazard.fromBaseline noExcess baseline
         |> ok
         |> ExcessIncidenceHazard.value
         |> float)

    rejects
        "Protective HR is outside this excess-loss model"
        (HazardRatio.create 0.8
         |> ok
         |> HazardRatio.toExcess)

    near
        "Registry per-100000 conversion"
        0.0013
        (AnnualRate.fromPer100000 130.0<person / year>
         |> ok
         |> AnnualRate.value
         |> float)

    check
        "A hazard is not constrained to probability range"
        (AnnualRate.create 2.0<1 / year>
         |> Result.isOk)

    rejects "Probability greater than one" (SurvivalProbability.create 1.1)

    rejects
        "NaN hazard"
        (IncidenceHazard.create (
            System.Double.NaN
            * 1.0<1 / year>
        ))

    rejects
        "Infinite dose"
        (AbsorbedDose.create (
            System.Double.PositiveInfinity
            * 1.0<Gy>
        ))

    rejects "Negative exposure age" (ExposureAge.create -1.0<year>)

    for endpoint, slope in
        [
            BreastCancer, 0.149
            LungCancer, 0.194
            StomachCancer, 0.84
            HeartFailure, 0.117
            MyocardialInfarction, 0.096
        ] do
        let ratio =
            Brodin2012.unmodifiedExcessHazardRatio exposure (MeanOrganDose(endpoint, dose 10.0<Gy>))
            |> ok

        near
            $"Table 1 {endpoint}"
            (slope
             * 10.0)
            (ExcessHazardRatio.value ratio)

        let zero =
            Brodin2012.unmodifiedExcessHazardRatio exposure (MeanOrganDose(endpoint, dose 0.0<Gy>))
            |> ok

        near $"Zero dose {endpoint}" 0.0 (ExcessHazardRatio.value zero)

    let fractional =
        DifferentialDvh.ofFractions [
            { Dose = dose 0.0<Gy>; Fraction = 0.5 }
            { Dose = dose 20.0<Gy>; Fraction = 0.5 }
        ]
        |> ok

    let absolute =
        DifferentialDvh.ofAbsoluteVolumes [
            {
                Dose = dose 0.0<Gy>
                Volume = 10.0<cm3>
            }
            {
                Dose = dose 20.0<Gy>
                Volume = 10.0<cm3>
            }
        ]
        |> ok

    let scaled =
        DifferentialDvh.ofAbsoluteVolumes [
            {
                Dose = dose 0.0<Gy>
                Volume = 100.0<cm3>
            }
            {
                Dose = dose 20.0<Gy>
                Volume = 100.0<cm3>
            }
        ]
        |> ok

    /// Evaluates the thyroid response for a test histogram.
    let thyroid dvh =
        Brodin2012.unmodifiedExcessHazardRatio exposure (ThyroidDvh dvh)
        |> ok
        |> ExcessHazardRatio.value

    let suppliedThyroid =
        ThyroidParameters.create 0.5<1 / Gy> 0.002<1 / Gy^2>
        |> ok

    near
        "Supplied nonlinear coefficients"
        (0.5
         * 0.5
         * 20.0
         * exp (
             -0.002
             * 400.0
         ))
        (DoseResponse.thyroid suppliedThyroid fractional
         |> ok
         |> ExcessHazardRatio.value)

    near
        "Thyroid direct voxel average"
        (0.5
         * 2.21
         * 20.0
         * exp (
             -0.00164
             * 400.0
         ))
        (thyroid fractional)

    near "Absolute and fractional DVH equivalence" (thyroid fractional) (thyroid absolute)
    near "Organ-volume scale invariance" (thyroid fractional) (thyroid scaled)

    let homogeneous =
        DifferentialDvh.ofFractions [ { Dose = dose 10.0<Gy>; Fraction = 1.0 } ]
        |> ok

    check
        "Thyroid nonlinear response cannot use mean-dose shortcut"
        (abs (
            thyroid homogeneous
            - thyroid fractional
        ) > 0.1)

    near
        "Mean DVH dose"
        10.0
        (DifferentialDvh.meanDose fractional
         |> ok
         |> AbsorbedDose.value
         |> float)

    rejects "Incomplete fractional DVH" (DifferentialDvh.ofFractions [ { Dose = dose 1.0<Gy>; Fraction = 0.5 } ])
    rejects "Empty DVH" (DifferentialDvh.ofFractions [])

    rejects
        "Zero organ volume"
        (DifferentialDvh.ofAbsoluteVolumes [
            {
                Dose = dose 1.0<Gy>
                Volume = 0.0<cm3>
            }
        ])

    rejects
        "Thyroid adult extrapolation"
        (Brodin2012.thyroidParameters (
            ExposureAge.create 20.0<year>
            |> ok
        ))

    let hfEvidence = {
        syntheticEvidence 60.0<year> with
            Endpoint = Linear HeartFailure
            Consequence =
                FatalHeartFailure(
                    MortalityHazard.create 0.001<1 / year>
                    |> ok,
                    conditionalLife 60.0<year> 20.0<year>
                )
    }

    let hf =
        LifeLoss.atAge
            exposure
            Female
            (Linear HeartFailure)
            (ExcessHazardRatio.create 1.17
             |> ok)
            hfEvidence
        |> ok

    near
        "Equation 4 uses excess mortality, not all baseline mortality"
        (0.5
         * 1.17
         * 0.001
         * 20.0)
        (LifeLossDensity.value hf.Density)

    rejects
        "HF incidence cannot enter mortality pathway"
        (LifeLoss.atAge
            exposure
            Female
            (Linear HeartFailure)
            (ExcessHazardRatio.create 1.17
             |> ok)
            {
                hfEvidence with
                    Consequence = (syntheticEvidence 60.0<year>).Consequence
            })

    rejects
        "Population endpoint mismatch"
        (LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio hfEvidence)

    rejects
        "Population sex mismatch"
        (LifeLoss.atAge exposure Male (Linear LungCancer) unmodifiedRatio (syntheticEvidence 60.0<year>))

    rejects
        "Event before irradiation"
        (LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio (syntheticEvidence 3.0<year>))

    rejects
        "Negative life shortening"
        (LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio {
            syntheticEvidence 60.0<year> with
                Consequence =
                    IncidentEvent(baseline, conditionalLife 60.0<year> 1.0<year>, conditionalLife 60.0<year> 2.0<year>)
        })

    let window =
        IntegrationWindow.create exposure 1.0<year> (age 15.0<year>)
        |> ok

    /// Evaluates a constant synthetic age-specific evidence grid.
    let sample attained =
        LifeLoss.atAge exposure Female (Linear LungCancer) adjustedRatio (syntheticEvidence attained)
        |> ok

    let constantDensity = LifeLossDensity.value incidenceLoss.Density

    near
        "Constant integrand over ten years"
        (constantDensity
         * 10.0)
        (LifeLoss.integrate window [ sample 5.0<year>; sample 15.0<year> ]
         |> ok
         |> ExpectedLifeYearsLost.value
         |> float)

    near
        "Irregular age grid gives same constant integral"
        (constantDensity
         * 10.0)
        (LifeLoss.integrate window [ sample 5.0<year>; sample 7.0<year>; sample 15.0<year> ]
         |> ok
         |> ExpectedLifeYearsLost.value
         |> float)

    let alternative =
        LifeLoss.atAge exposure Female (Linear LungCancer) unmodifiedRatio (syntheticEvidence 15.0<year>)
        |> ok

    near
        "Attained-age-dependent excess HR can be integrated"
        1.875
        (LifeLoss.integrate window [ sample 5.0<year>; alternative ]
         |> ok
         |> ExpectedLifeYearsLost.value
         |> float)

    rejects "Missing lower bound" (LifeLoss.integrate window [ sample 6.0<year>; sample 15.0<year> ])
    rejects "Duplicate ages" (LifeLoss.integrate window [ sample 5.0<year>; sample 5.0<year>; sample 15.0<year> ])

    rejects
        "Unsorted ages"
        (LifeLoss.integrate window [ sample 5.0<year>; sample 9.0<year>; sample 7.0<year>; sample 15.0<year> ])

    rejects "Reversed bounds" (IntegrationWindow.create exposure 5.0<year> (age 8.0<year>))

    near
        "Conditional survival area"
        5.0
        (LifeLoss.remainingLifeFromSurvival [ 0.0<year>, survival 1.0; 10.0<year>, survival 0.0 ]
         |> ok
         |> RemainingLife.value
         |> float)

    rejects
        "Restricted mean is not full life expectancy"
        (LifeLoss.remainingLifeFromSurvival [ 0.0<year>, survival 1.0; 10.0<year>, survival 0.2 ])

    rejects
        "Increasing survival"
        (LifeLoss.remainingLifeFromSurvival [
            0.0<year>, survival 1.0
            1.0<year>, survival 0.5
            2.0<year>, survival 0.6
            3.0<year>, survival 0.0
        ])

    printfn "Scientific domain checks passed."

/// Runs focused regression checks for review findings and boundary semantics.
let reviewChecks () =
    let exposure =
        ExposureAge.create 4.0<year>
        |> ok

    let ratio =
        HazardRatio.create 1.5
        |> ok

    let incidence =
        IncidenceHazard.create 0.02<1 / year>
        |> ok

    let mortality =
        MortalityHazard.create 0.01<1 / year>
        |> ok

    near
        "Total-HR incidence convenience"
        0.01
        (ExcessIncidenceHazard.fromHazardRatio ratio incidence
         |> ok
         |> ExcessIncidenceHazard.value
         |> float)

    near
        "Total-HR mortality convenience"
        0.005
        (ExcessMortalityHazard.fromHazardRatio ratio mortality
         |> ok
         |> ExcessMortalityHazard.value
         |> float)

    let protective =
        HazardRatio.create 0.8
        |> ok

    match HazardRatio.toExcess protective with
    | Error message ->
        check
            "Protective decision is explicit"
            (message.Contains("protective")
             && message.Contains("signed"))
    | Ok _ -> failwith "Protective HR must not enter non-negative excess loss."

    rejects "Protective incidence convenience" (ExcessIncidenceHazard.fromHazardRatio protective incidence)
    rejects "Protective mortality convenience" (ExcessMortalityHazard.fromHazardRatio protective mortality)

    for years in
        [
            18.7<year>
            2.81
            / 0.15
            * 1.0<year>
        ] do
        check
            "Non-negative thyroid age boundary"
            (Brodin2012.thyroidParameters (
                ExposureAge.create years
                |> ok
             )
             |> Result.isOk)

    match
        Brodin2012.thyroidParameters (
            ExposureAge.create 18.8<year>
            |> ok
        )
    with
    | Error message ->
        check
            "Thyroid age error explains limit"
            (message.Contains("exposure age")
             && message.Contains("negative"))
    | Ok _ -> failwith "Negative thyroid slope accepted."

    rejects "Negative absorbed dose" (AbsorbedDose.create -1.0<Gy>)

    let restricted =
        LifeLoss.restrictedMeanFromSurvival [ 0.0<year>, survival 1.0; 10.0<year>, survival 0.2 ]
        |> ok

    near
        "Finite-horizon area"
        6.0
        (RestrictedMeanLife.value restricted
         |> float)

    near
        "Finite-horizon duration"
        10.0
        (RestrictedMeanLife.horizon restricted
         |> float)

    near
        "Finite-horizon terminal survival"
        0.2
        (RestrictedMeanLife.terminalSurvival restricted
         |> SurvivalProbability.value)

    check "Tail is explicitly flagged" (RestrictedMeanLife.hasUnintegratedTail restricted)

    let complete =
        LifeLoss.restrictedMeanFromSurvival [ 0.0<year>, survival 1.0; 10.0<year>, survival 0.0 ]
        |> ok

    check "Zero tail is marked complete" (not (RestrictedMeanLife.hasUnintegratedTail complete))

    rejects
        "Restricted curve still needs valid ordering"
        (LifeLoss.restrictedMeanFromSurvival [ 0.0<year>, survival 1.0; 0.0<year>, survival 0.2 ])

    let wrongPopulation = {
        syntheticEvidence 60.0<year> with
            Consequence =
                IncidentEvent(incidence, conditionalLife 59.0<year> 20.0<year>, conditionalLife 60.0<year> 5.0<year>)
    }

    let wrongPostEvent = {
        syntheticEvidence 60.0<year> with
            Consequence =
                IncidentEvent(incidence, conditionalLife 60.0<year> 20.0<year>, conditionalLife 61.0<year> 5.0<year>)
    }

    let excess =
        ExcessHazardRatio.create 0.5
        |> ok

    rejects
        "Population conditioning age mismatch"
        (LifeLoss.atAge exposure Female (Linear LungCancer) excess wrongPopulation)

    rejects
        "Post-event conditioning age mismatch"
        (LifeLoss.atAge exposure Female (Linear LungCancer) excess wrongPostEvent)

    let wrongDeathAge = {
        syntheticEvidence 60.0<year> with
            Endpoint = Linear HeartFailure
            Consequence = FatalHeartFailure(mortality, conditionalLife 59.0<year> 20.0<year>)
    }

    rejects
        "Fatal-event conditioning age mismatch"
        (LifeLoss.atAge exposure Female (Linear HeartFailure) excess wrongDeathAge)

    let maxExcess =
        ExcessHazardRatio.create System.Double.MaxValue
        |> ok

    check
        "Extreme finite excess still converts safely"
        (HazardRatio.ofExcess maxExcess
         |> Result.isOk)

    printfn "Review regression checks passed."