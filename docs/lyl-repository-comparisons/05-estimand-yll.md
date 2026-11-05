# estimandYLL: standardized survival differences via the g-formula

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [ShuntaroS/estimandYLL](https://github.com/ShuntaroS/estimandYLL).
- Reviewed revision: [`35a350345fbd`](https://github.com/ShuntaroS/estimandYLL/tree/35a350345fbd00273b1a08c0af4584d4aeb5de84).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Software citation: Shuntaro Sato, *estimandYLL: Years of Life Lost via the G-Formula*, version 0.2.0. DOI declared in `CITATION.cff`: [10.5281/zenodo.23653505](https://doi.org/10.5281/zenodo.23653505). This is a software DOI, not a verified journal-article DOI.
- Sandwich-method reference linked by the package: [10.48550/arXiv.2504.13291](https://doi.org/10.48550/arXiv.2504.13291).
- Inspected: README, DESCRIPTION, `engine.R`, `counterfactual.R`, `hazard-model.R`, `validation.R`, target-population/getting-started vignettes, algorithm tests and validation reports. MIT licence.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

An R package estimating the difference in restricted expected residual lifetime between two sustained binary exposure scenarios. It standardizes predictions to a declared baseline population: all participants, those observed exposed, or those observed unexposed. This provides an alternative survival-based estimand and a useful example of explicit calculation contracts.

## 3. Formulation and code workflow

$$LYL(a,\tau\mid P)=E_{X\sim P}\left[\int_a^\tau\{S^0(u\mid a,X)-S^1(u\mid a,X)\}\,du\right].$$

[hazard-model.R](https://github.com/ShuntaroS/estimandYLL/blob/35a350345fbd00273b1a08c0af4584d4aeb5de84/R/hazard-model.R) fits pooled logistic mortality with exposure, an attained-age natural spline, exposure–age interaction and baseline covariates. The predicted quantity is annual death probability, not a continuous annual hazard rate:

$$S^b(a+k\mid a,X)=\prod_{r=0}^{k-1}[1-p^b(a+r,X)].$$

[counterfactual.R](https://github.com/ShuntaroS/estimandYLL/blob/35a350345fbd00273b1a08c0af4584d4aeb5de84/R/counterfactual.R) lags the cumulative product to align survival with interval starts. [engine.R](https://github.com/ShuntaroS/estimandYLL/blob/35a350345fbd00273b1a08c0af4584d4aeb5de84/R/engine.R) restarts each person's survival at one for each requested age, then averages curves and integrates them. The default is an annual left-rectangle rule; trapezoidal integration is optional. Reporting every five years does not coarsen the internal annual integration grid.

Uncertainty comes from a participant bootstrap or stacked-equation sandwich variance. The former refits knots; the latter treats knots as fixed. Both produce pointwise rather than simultaneous intervals.

## 4. Similarities and differences from our domain

| Dimension | estimandYLL | Our domain |
| --- | --- | --- |
| Computation | Difference between scenario survival areas | Survival-weighted excess-event consequence |
| Exposure | Binary sustained scenario | Organ dose/DVH |
| Population | Explicit baseline standardization set | Caller-declared survivor evidence cohort |
| Probability/rate | Discrete death probability | Distinct annual rate and survival probability types |
| Horizon | Restricted upper age | Restricted survival type distinct from full life |
| Sign | Signed contrast | Nonnegative expected excess loss |

## 5. What could be useful to us

- A complementary future scenario-survival module, retaining separate expected lifetimes and their difference.
- Tests for label-swap sign reversal, zero-width windows, fixed-population weighting, known-model truth and independence from earlier requested ages.
- An example of preserving bootstrap failures and metadata rather than returning only a final number.
- Numerical comparisons of restricted survival integration when input curves and rules are identical.

## 6. What is substantially different or cannot be transferred

The averaging population is **not** the set actually observed alive at the requested age. Everyone in the baseline target contributes at each age. Conditional individual curves are averaged; conditioning an already averaged curve would change weights.

The exposure scenarios do not represent exposure newly initiated at the starting age. Causal interpretation requires exchangeability, positivity, appropriate cohort-entry/censoring assumptions and an adequate hazard model. It supplies no dose-response coefficients or endpoint-specific post-event evidence for Brodin. The implemented binary exposure does not represent an arbitrary continuous organ-dose intervention.

## 7. Implementation observations and verification limits

- The inspected core survival and integration operations match their documented algorithm.
- A public test suite includes synthetic recovery and null-effect cases. Some large algorithm tests are skipped on CRAN; test presence is not evidence that every current branch passed locally.
- The sandwich report records agreement with bootstrap standard errors in selected synthetic scenarios but explicitly states that confidence-interval coverage was not evaluated.
- The comparator report documents extreme Royston–Parmar bootstrap instability. Those intervals are unsuitable as validation targets even if old and new implementations agree.
- Annual splitting permits partial final follow-up intervals; the pooled logistic fit contains no explicit duration adjustment. Its handling of fractional follow-up needs investigation with known continuous-time data.
- Age extrapolation and sparse covariate/exposure support remain model concerns. No external package was executed here.

## 8. Recommendation and next verification

**Retain for calculation contracts and a future restricted-survival comparator.** Match conditioning, standardization, grid and horizon before expecting numerical equality. Do not convert its signed contrast into our nonnegative excess-loss type or interpret bootstrap agreement as clinical validation.
