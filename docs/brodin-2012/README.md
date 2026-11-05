# Brodin 2012 model adapter

The adapter records dose-response parameters from *Life Years Lost—Comparing Potentially Fatal Late Complications After Radiotherapy for Pediatric Medulloblastoma on a Common Scale*, Cancer 118 (2012), 5432–5440. DOI: [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536).

For the general software model, start with the [documentation guide](../README.md) and [domain vocabulary](../domain.md).

## Responsibilities

[Brodin2012.fs](../../src/LifeYearsLost/Brodin2012.fs) supplies the five linear organ-dose coefficients, the printed thyroid age-dependent coefficients, and reported sex comparisons with their source references. Generic dose-response equations and life-loss calculations remain in separate modules.

Parameters and multipliers can also be supplied explicitly. Reported associations preserve their statistical meaning and provenance; the software does not automatically reinterpret them as another effect measure.

See the [source map](sources.md) for equation and coefficient references.

## Inputs and scope

The calculation requires organ doses or a differential thyroid DVH, patient context, age-specific baseline event hazards, survivor survival and remaining life expectancy. Multipliers carry a source or declared assumption.

The implemented pathways cover excess incidence and excess mortality, survival-weighted life-loss density, and integration over attained age. The thyroid age transcription rejects negative resulting coefficients; that mathematical check does not define a clinical validity range.

Population tables, fitted survivor curves, plan datasets and uncertainty distributions are supplied separately. The repository does not currently load these datasets or provide a clinical calculation workflow.

Publication-specific clarification items are maintained in [Research questions](../research-questions.md).
