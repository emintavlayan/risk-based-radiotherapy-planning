# Mortality data weighted by remaining life expectancy

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [tinojona/Years-of-Life-Lost-from-mortality-data](https://github.com/tinojona/Years-of-Life-Lost-from-mortality-data).
- Reviewed revision: [`25690529d99b`](https://github.com/tinojona/Years-of-Life-Lost-from-mortality-data/tree/25690529d99b0858ff11ce9698864f44c5e462f8).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Teaching material for [TACTIC](https://www.tactic-tools.org/), as identified in the README. No associated journal article/DOI is identified in the inspected material.
- Data references in the notebook: [Mexican mortality records](http://www.dgis.salud.gob.mx/contenidos/basesdedatos/da_defunciones_gobmx.html) and [UN Population Data Portal](https://population.un.org/dataportal/).
- Inspected: `YLL_calculation.Rmd`, README, DESCRIPTION and the life-table header/sample. The mortality CSV was not successfully retrieved as text; its full contents were not audited. DESCRIPTION declares AGPL-3.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

A teaching notebook converting observed deaths into a daily series of years lost. The example describes Mexico City deaths in 2023 and national Mexican life expectancy from World Population Prospects 2024. It illustrates the life-expectancy weighting used in mortality burden reporting.

## 3. Formulation and code workflow

For observed deaths on day $d$ and sex $s$:

$$YLL(d,s)=\sum_{i\in deaths(d,s)}e(a_i,s,y_i),$$

where $e$ is remaining life expectancy at the death age and calendar year. It is not life expectancy at birth minus death age.

The [notebook](https://github.com/tinojona/Years-of-Life-Lost-from-mortality-data/blob/25690529d99b0858ff11ce9698864f44c5e462f8/YLL_calculation.Rmd) selects year, age, sex and expected remaining life from the table; joins deaths by those three keys; groups by day and sex; and sums `remaining_LE`. No survival fitting, dose-response modelling or hazard integration occurs. The output is an aggregate burden total, not an individual prospective expected loss.

## 4. Similarities and differences from our domain

| Dimension | Notebook | Our domain |
| --- | --- | --- |
| Deaths | Already observed | Expected excess mortality rate |
| Life weighting | Population remaining life | Population remaining life for fatal HF; prognosis difference for incidence |
| Radiation effect | Absent | Dose-derived excess hazard ratio |
| Denominator | Total across observed deaths | Expected loss per starting cohort member |
| Cohort survival | Implicit in which deaths occurred | Explicit `SurvivorSurvival` |

## 5. What could be useful to us

The UN source is a candidate for a versioned general-population life-expectancy adapter. Its provenance must retain country, calendar period, sex, exact-age convention, source revision and oldest-age handling. The example is also useful for explaining why the same fatal event at different ages can have different consequences.

Our fatal mortality pathway has the prospective counterpart:

$$LYL_{HF}=\int S_{surv}(a)\Delta\mu_{HF}(a)e_{pop}(a)\,da.$$

Observed death counts cannot replace $\Delta\mu_{HF}$ without changing the denominator and question.

## 6. What is substantially different or cannot be transferred

The national Mexican values are not evidence for Danish patients or post-cancer prognosis. The notebook supplies neither baseline incidence nor post-event remaining life. For an incident event, our consequence is $e_{pop}-e_{post}$, not $e_{pop}$ alone. Its resulting population burden cannot be inserted into `ExcessHazardRatio` or interpreted as radiotherapy-attributable loss.

## 7. Implementation observations and verification limits

- `sum(remaining_LE, na.rm=TRUE)` silently omits unmatched deaths. A clinical evidence adapter should report or reject incomplete matching.
- Every sex label other than `Male` is recoded as female; unexpected labels are not rejected.
- Life-table join-key uniqueness is not checked. Multiple source variants or duplicate keys could multiply records.
- Exact age matching has no explicit policy for fractional ages, unknown ages or the oldest group.
- Country and source metadata are discarded before matching; a general importer must preserve them.
- Full mortality data and rendered outputs were not independently checked.

## 8. Recommendation and next verification

**Keep as a data-source and explanatory reference. No core formula change.** If an importer is implemented, verify key uniqueness and complete matching and compare selected ages with the authoritative national or UN table before using the values.
