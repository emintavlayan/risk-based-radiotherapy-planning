# Brodin 2012: domain and scientific review

Research model of **Life Years Lost—Comparing Potentially Fatal Late Complications After Radiotherapy for Pediatric Medulloblastoma on a Common Scale**, Cancer 118 (2012), 5432–5440. DOI: [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536).

The supplied final published PDF is the primary transcription source. Equations 1–5, Table 1 and Figure 2 were inspected. The paper was read through its discussion and references. This is an explicit implementation of selected equations with an audit of their interpretation. It does not establish that the published patient results are reproducible or clinically validated.

## Reading order for colleagues

1. [Sex-normalization investigation](sex-normalization.md): why 1.21 is not yet a demonstrated error.
2. [Clinical vocabulary and calculation flow](domain.md): what each quantity means.
3. [Equation audit and unresolved questions](audit.md): suspected inconsistencies, interpretations and limitations.
4. [Source map](sources.md): coefficients, equations and external verification.
5. Read the F# types in this order:

| File | What to inspect |
| --- | --- |
| [Units.fs](../../src/LifeYearsLost/Units.fs) | Gray, years, cubic centimetres and registry denominators |
| [Quantities.fs](../../src/LifeYearsLost/Quantities.fs) | Total HR, excess HR, incidence and mortality hazards |
| [Evidence.fs](../../src/LifeYearsLost/Evidence.fs) | Reported comparisons versus supplied excess-hazard multipliers |
| [DoseResponse.fs](../../src/LifeYearsLost/DoseResponse.fs) | Generic equations with explicit supplied coefficients |
| [DoseVolume.fs](../../src/LifeYearsLost/DoseVolume.fs) | Absolute versus fractional differential DVHs |
| [Brodin2012.fs](../../src/LifeYearsLost/Brodin2012.fs) | Table 1 coefficients and Equations 1/5 |
| [LifeLoss.fs](../../src/LifeYearsLost/LifeLoss.fs) | Equations 2/3/4 and integration |

## Run

From the repository root, with .NET 8:

```sh
dotnet run --project tests/LifeYearsLost/LifeYearsLost.Tests.fsproj
```

The scientific project is independent of the application UI and uses FsToolkit.ErrorHandling. Tests use synthetic evidence to check equations, type boundaries and numerical invariants. Reproducing Figure 2 is not required.

## Scope

Implemented: all five linear dose responses, the unmodified thyroid differential-DVH response, excess incidence and excess mortality pathways, survival-weighted age-specific loss, trapezoidal integration, and remaining life from a complete conditional survival curve.

Not supplied: original population hazard tables, post-event survival tables, fitted CCSS survival/extrapolation data, ten patient DVHs, coefficient confidence distributions, or the paired Monte Carlo/bootstrap analysis. Consequently no published lifetime totals, modality comparisons, clinical predictions, or uncertainty intervals are claimed.

Generic dose-response functions accept explicit coefficients with units. A supplied ExcessHazardMultiplier carries its source or declared assumption and can only multiply excess HR. It is not inferred from a reported sex comparison. Table 1 sex comparisons retain their unresolved statistical meaning as typed evidence.

The printed thyroid age formula is available separately as a source transcription and rejects negative resulting coefficients. This mathematical boundary is not a clinical validity range. The paper applied the model to ages 4–15; wider applicability remains a scientific question.

See the [RR-to-HR derivation](risk-and-hazard.md): that conversion does not explain the 1.21 modifier.
