#r "nuget: FsToolkit.ErrorHandling, 4.18.0"
#load "../src/LifeYearsLost/Units.fs"
#load "../src/LifeYearsLost/Quantities.fs"
#load "../src/LifeYearsLost/DoseVolume.fs"
#load "../src/LifeYearsLost/Brodin2012.fs"
#load "../src/LifeYearsLost/LifeLoss.fs"

open LifeYearsLost
open FsToolkit.ErrorHandling

/// Reproduces Figure 2 without rounding intermediate quantities.
let figure2 = result {
    let! exposure = ExposureAge.create 4.0<year>
    let! attained = AttainedAge.create 60.0<year>
    // Diagnostic transcription of the displayed example, not a resolved sex modifier.
    let! ratio = ExcessHazardRatio.create (0.194 * 9.4 * 1.21)
    let! baseline = IncidenceHazard.create 0.0013<1/year>
    let! survival = SurvivalProbability.create 0.54
    // Only the difference 18.6 is published; 20 and 1.4 are illustrative, not registry observations.
    let! populationLife = RemainingLife.create 20.0<year>
    let! postEventLife = RemainingLife.create 1.4<year>
    let evidence = {
        Endpoint = Linear LungCancer; Sex = Female; Age = attained
        SurvivorSurvival = survival
        Consequence = IncidentEvent (baseline, populationLife, postEventLife)
    }
    return! LifeLoss.atAge exposure Female (Linear LungCancer) ratio evidence
}

match figure2 with
| Error error -> failwith error
| Ok trace ->
    printfn "Excess HR: %.9f (dimensionless)" (ExcessHazardRatio.value trace.ExcessRatio)
    printfn "Total HR:  %.9f (dimensionless)" (HazardRatio.ofExcess trace.ExcessRatio |> Result.map HazardRatio.value |> Result.defaultWith failwith)
    printfn "Excess incidence hazard: %.9f /year" (float (ExcessEventHazard.value trace.ExcessEventHazard))
    printfn "Survival-weighted rate:  %.9f /year" (float (AnnualRate.value trace.SurvivalWeightedExcessRate))
    printfn "Life shortening: %.1f years" (float trace.LifeShortening)
    printfn "Loss density: %.12f years lost per year of attained age" (LifeLossDensity.value trace.Density)
    printfn "Printed staged rounding: 0.0015/year * 18.6 years = 0.0279."
    printfn "This is one age-specific integrand, not lifetime loss."
