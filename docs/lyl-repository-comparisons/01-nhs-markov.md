# NHS England: Markov modelling of years of life lost

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [nhsengland/Markov-modelling-for-years-of-life-lost](https://github.com/nhsengland/Markov-modelling-for-years-of-life-lost).
- Reviewed revision: [`b381f46f5073`](https://github.com/nhsengland/Markov-modelling-for-years-of-life-lost/tree/b381f46f5073722e3c8ece7479ef517eebd0dce2).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Gregg et al. (2024), *The burden of diabetes-associated multiple long-term conditions on years of life spent and lost*, Nature Medicine. DOI: [10.1038/s41591-024-03123-2](https://doi.org/10.1038/s41591-024-03123-2); [full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC11485235/).
- Inspected: `main_simulations.R`, `markov_helper_functions.R`, `top10s.R`, `median_impact_plots.R`, README and licence in the diabetes study folder. MIT licence.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

An age-dependent, three-state Markov analysis of diabetes together with another long-term condition. It estimates years lived with the pair and years lost relative to general-population survival. It is relevant because prognosis, competing mortality and time lived after disease onset also enter our LYL evidence.

The code's state name “Neither morbidity” actually means **not both**: a person with one of the two conditions belongs there. “Both morbidities” allows additional diseases. Death is absorbing; the published illness-death model excludes remission. This is not a detailed model of every comorbidity combination.

## 3. Formulation and code workflow

In [markov_helper_functions.R](https://github.com/nhsengland/Markov-modelling-for-years-of-life-lost/blob/b381f46f5073722e3c8ece7479ef517eebd0dce2/The%20Character%20and%20Burden%20of%20Years%20Spent%20and%20Lost%20with%20Diabetes-Related%20Multiple%20Long-term%20Conditions/markov_helper_functions.R), observed monthly transition counts become row-normalized probabilities:

$$P_{ij}(a)=n_{ij}(a)/\sum_j n_{ij}(a).$$

`get_age_transition_matrix` uses the matrix power $P(a)^{12}$ to construct an annual transition matrix. It is not an elementwise power. This assumes the monthly matrix is applicable throughout the year. Successive annual matrices are multiplied in attained-age order.

In [main_simulations.R](https://github.com/nhsengland/Markov-modelling-for-years-of-life-lost/blob/b381f46f5073722e3c8ece7479ef517eebd0dce2/The%20Character%20and%20Burden%20of%20Years%20Spent%20and%20Lost%20with%20Diabetes-Related%20Multiple%20Long-term%20Conditions/main_simulations.R), `fun_yll` subtracts the sum of future “Both morbidities” occupancy probabilities from the sum of a separately supplied general-population alive curve:

$$YLL(a)\approx\sum_{k=1}^{101-a}\{S_{pop}(k\mid a)-P(M\text{ at }a+k\mid M\text{ at }a)\}\,1\text{ year}.$$

With no remission, remaining in the illness state is equivalent to being alive after entering it. If recovery were allowed, illness occupancy would not equal survival and this subtraction would need reconsideration. `fun_ymm` sums illness occupancy alone. These are annual endpoint sums, not trapezoidal integration.

`top10s.R` also weights age-specific losses by the estimated age distribution of acquiring the pair, reporting averages among those acquiring it and burden per 1,000 people. Person-level and population-level quantities have different denominators.

## 4. Similarities and differences from our domain

| Dimension | NHS implementation | Our implementation |
| --- | --- | --- |
| Event evidence | Observed disease transitions | Supplied incidence or mortality hazards |
| Exposure | Existing diabetes-related disease pair | Organ radiation dose/DVH |
| Remaining life | State occupancy/survival sums | `RemainingLifeAtAge` supplied explicitly |
| Loss | Difference of survival areas | `LifeLoss.atAge` event-weighted density |
| Competing death | Explicit transition | Supplied `SurvivorSurvival` |
| Horizon | Through age 101 in propagation | Explicit `IntegrationWindow`; printed-paper helper ends at 110 |

A common unit does not establish equality of estimands. The diabetes-associated difference is not a radiation-attributable loss and cannot be supplied as an excess hazard ratio.

## 5. What could be useful to us

- An architectural example for a future comorbidity-specific prognosis adapter.
- Synthetic transition matrices can generate survival curves to check survival-area calculations, with integration rules aligned first.
- State occupancy can separate years alive with disease from years lost; our current endpoint kernel does not describe years lived with morbidity.
- Explicit onset-age weighting is useful if a future population report combines age-specific predictions.

## 6. What is substantially different or cannot be transferred

There is no radiation dose response, treatment comparator or organ DVH. Observational associations with disease pairs do not establish the effects of preventing them. The general-population comparator is not necessarily a matched disease-free counterfactual. The transition model's Markov assumption also omits duration since onset unless encoded in states or covariates.

Adding this capability would mean new disease-state evidence and a new workflow, rather than replacing `Brodin2012` coefficients.

## 7. Implementation observations and verification limits

- Required RDS input files, transition-generation inputs and SQL data are absent. Database placeholders prevent direct reproduction from the repository alone.
- For a state-age stratum with zero observed transitions, the code supplies a self-transition. This avoids division by zero but can imply no exit or death in that stratum.
- The finite endpoint omits any survival beyond age 101; it is not automatically full remaining life.
- `top10s.R` references `quartile_onset_age` without defining it in that script.
- The main script and later aggregation use different acquisition-weight constructions, including monthly versus annual inputs. Their alignment requires a dedicated data-level review before trusting population summaries.
- No automated tests or uncertainty propagation were found in the inspected scripts.

These observations concern the public implementation; they are not evidence that the published study used incorrect final results.

## 8. Recommendation and next verification

**Retain as a future comorbidity/multistate reference. No change to our Brodin kernel.** If pursued, build a small explicit transition example with known survival and check state ordering, stochastic row sums, no-remission behaviour, annualization and horizon loss before using any patient data.
