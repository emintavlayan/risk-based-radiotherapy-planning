# Brodin 2012: domain and scientific review

Research model of **Life Years Lost—Comparing Potentially Fatal Late Complications After Radiotherapy for Pediatric Medulloblastoma on a Common Scale**, Cancer 118 (2012), 5432–5440. DOI: [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536).

The supplied final published PDF is the primary transcription source. Equations 1–5, Table 1 and Figure 2 were inspected. The paper was read through its discussion and references. This is an explicit implementation of selected equations with an audit of their interpretation. It does not establish that the published patient results are reproducible or clinically validated.

## Reading order for colleagues

1. [Sex-normalization investigation](sex-normalization.md): why 1.21 is not yet a demonstrated error.
2. [Clinical vocabulary and calculation flow](domain.md): what each quantity means.
3. [Equation audit and unresolved questions](audit.md): suspected inconsistencies, interpretations and limitations.
4. [Source map](sources.md): coefficients, equations and external verification.
5. [Executable Figure 2 example](../../examples/Brodin2012.fsx).
6. Read the F# types in this order:

| File | What to inspect |
| --- | --- |
| [Units.fs](../../src/LifeYearsLost/Units.fs) | Gray, years, cubic centimetres and registry denominators |
| [Quantities.fs](../../src/LifeYearsLost/Quantities.fs) | Total HR, excess HR, incidence and mortality hazards |
| [DoseVolume.fs](../../src/LifeYearsLost/DoseVolume.fs) | Absolute versus fractional differential DVHs |
| [Brodin2012.fs](../../src/LifeYearsLost/Brodin2012.fs) | Table 1 coefficients and Equations 1/5 |
| [LifeLoss.fs](../../src/LifeYearsLost/LifeLoss.fs) | Equations 2/3/4 and integration |

## Run

From the repository root, with .NET 8:

```sh
dotnet run --project tests/LifeYearsLost/LifeYearsLost.Tests.fsproj
dotnet fsi examples/Brodin2012.fsx
```

The scientific project is independent of the application UI and uses FsToolkit.ErrorHandling. It does not change the running calculator. The example uses only Figure 2 inputs and explicitly labels two illustrative life expectancies whose difference is the published 18.6 years.

## Scope

Implemented: all five linear dose responses, the unmodified thyroid differential-DVH response, excess incidence and excess mortality pathways, survival-weighted age-specific loss, trapezoidal integration, and remaining life from a complete conditional survival curve.

Not supplied: original population hazard tables, post-event survival tables, fitted CCSS survival/extrapolation data, ten patient DVHs, coefficient confidence distributions, or the paired Monte Carlo/bootstrap analysis. Consequently no published lifetime totals, modality comparisons, clinical predictions, or uncertainty intervals are claimed.

The 4–15-year thyroid age restriction is an implementation boundary based on the treated sample, not a published validation range. Sex normalization is unresolved; no generic sex-modifier API is exposed. The loss kernel accepts an already specified excess HR independently of the dose-response transcription. The paper's bounds e+1 to 110 can be requested explicitly; other bounds are also explicit.
