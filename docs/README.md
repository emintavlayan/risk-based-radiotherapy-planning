# Software documentation

The application models radiation-related late effects as expected life years lost. The scientific domain is independent of the SAFE client and server.

## Read the domain

Start with the [scientific workflow and detailed code maps](workflow.md).

1. [Domain vocabulary and calculation flow](domain.md): inputs, quantities, units and equations.
2. Read the source files in the order below.
3. [Brodin 2012 model adapter](brodin-2012/README.md): publication-specific coefficients and source references.

| File | Responsibility |
| --- | --- |
| [Units.fs](../src/LifeYearsLost/Units.fs) | Dose, time, organ volume and registry denominators |
| [Quantities.fs](../src/LifeYearsLost/Quantities.fs) | Validated ages, probabilities, hazards, ratios and life expectancy |
| [Evidence.fs](../src/LifeYearsLost/Evidence.fs) | Reported comparisons and supplied multipliers with provenance |
| [DoseVolume.fs](../src/LifeYearsLost/DoseVolume.fs) | Differential DVH validation and volume averaging |
| [DoseResponse.fs](../src/LifeYearsLost/DoseResponse.fs) | Dose-response equations with supplied coefficients |
| [Brodin2012.fs](../src/LifeYearsLost/Brodin2012.fs) | Publication-specific parameter transcription |
| [LifeLoss.fs](../src/LifeYearsLost/LifeLoss.fs) | Survival-weighted loss and attained-age integration |

## Verify the implementation

From the repository root:

```sh
dotnet run --project tests/LifeYearsLost/LifeYearsLost.Tests.fsproj
bash tests/LifeYearsLost/check-types.sh
```

The tests check synthetic calculations, numerical invariants, invalid inputs and compile-time distinctions between quantities. Clinical validation requires appropriate source data and independent evaluation.

## Additional references

- [F# formatting](formatting.md).

- [Risk and hazard relationships](risk-and-hazard.md).
- [Research questions and interpretation notes](research-questions.md), kept separately from the software's operating documentation.

## Article comparisons

- [Rechner 2020 compared with the LifeYearsLost domain](rechner-2020-comparison.md): equations, shared components, endpoint and parameter differences, mortality weighting, target-control coupling, and unresolved reproduction questions.

## External repository comparisons

- [Nine LYL, survival and radiation-risk repository reviews](lyl-repository-comparisons/README.md): formulation, code observations, similarities, differences, possible uses and verification limits.
