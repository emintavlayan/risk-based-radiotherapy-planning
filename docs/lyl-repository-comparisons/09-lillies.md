# lillies: empirical remaining life and excess life years lost

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [cran/lillies](https://github.com/cran/lillies).
- Reviewed revision: [`aae61536277f`](https://github.com/cran/lillies/tree/aae61536277f6c4a3858eefcb33f6c36269781e1).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- This GitHub repository is a CRAN source mirror, not necessarily the author's development repository. Inspected package version: 0.2.12.
- Plana-Ripoll et al. (2020), *lillies: An R package for the estimation of excess Life Years Lost among patients with a given disease or condition*. DOI: [10.1371/journal.pone.0228073](https://doi.org/10.1371/journal.pone.0228073); [full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC7059906/).
- Andersen (2017), *Life years lost among patients with a given disease*. DOI: [10.1002/sim.7357](https://doi.org/10.1002/sim.7357).
- Inspected: DESCRIPTION, source inventory, `lyl_aux.R`, `lyl_diff_ref.R`, `lyl_aggregated_aux.R` and exported API documentation. MIT licence. Bootstrap execution and all reporting paths were not audited.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

An R package estimating remaining lifetime and years lost before a fixed upper age for a population with a condition. It supports age-specific and age-range estimates, comparison with another population or a life table, cause-of-death decomposition and bootstrap intervals.

It is relevant to the evidence side of our project: estimating the remaining life of a cancer-survivor or post-event population. It contains no radiation dose-response model.

## 3. Formulation and code workflow

For starting age $a$ and terminal age $\tau$:

$$RMST(a,\tau)=\int_a^\tau S(u\mid a)\,du,$$

$$LYL(a,\tau)=(\tau-a)-RMST(a,\tau).$$

In a competing-death analysis the loss can be decomposed into cumulative-incidence areas:

$$LYL_j(a,\tau)=\int_a^\tau F_j(u\mid a)\,du.$$

Subtracting condition-population and reference-population loss yields excess loss; with identical horizons this corresponds to reference remaining life minus condition remaining life. The comparison direction still needs checking at the particular reporting API before using a returned signed difference.

### Individual follow-up

[estimate_lyl in lyl_aux.R](https://github.com/cran/lillies/blob/aae61536277f6c4a3858eefcb33f6c36269781e1/R/lyl_aux.R) calls `survival::survfit` on entry/exit ages. For a single death event it uses survival and its complement; with competing causes it uses state probabilities. It integrates those curves with `pracma::trapz`. The `t0`/entry-age mechanism is important: observed follow-up after disease onset is not automatically survival from birth or exposure.

### Reference evidence

[lyl_diff_ref.R](https://github.com/cran/lillies/blob/aae61536277f6c4a3858eefcb33f6c36269781e1/R/lyl_diff_ref.R) accepts population survival or age-specific rates, conditions reference survival at each requested age, and calculates a restricted life area. Cause-specific patient results are collapsed to all-cause loss when comparing against an all-cause life table. It does not manufacture reference cause-specific decompositions.

Age-range summaries may be weighted by the onset-age distribution. The weighting changes the target population and must not be confused with evaluating a single patient's age.

### Aggregated rate conversion

[lyl_aggregated_aux.R](https://github.com/cran/lillies/blob/aae61536277f6c4a3858eefcb33f6c36269781e1/R/lyl_aggregated_aux.R) uses

$$S_{k+1}=S_k(1-r_k\Delta a_k).$$

This is a first-order rate-to-survival approximation. It differs from the piecewise-constant continuous-hazard identity $S_{k+1}=S_k\exp(-r_k\Delta a_k)$. If inputs are interval death probabilities, they need explicit corresponding semantics. The public variable name “rate” is not sufficient to establish an exact continuous-hazard interpretation.

## 4. Similarities and differences from our domain

| Dimension | lillies | Our domain |
| --- | --- | --- |
| Evidence | Individual follow-up or aggregated survival/rates | Validated, caller-supplied evidence |
| Life area | Restricted to upper age | `RestrictedMeanLife` explicitly distinct from full life |
| Patient loss | Observed-population survival and reference comparison | Radiation-attributable excess-event model |
| Cause handling | Empirical cause-of-death state probabilities | Separate incidence and mortality pathways |
| Dose | Absent | Mean dose/DVH and response coefficients |
| Uncertainty | Patient bootstrap | No built-in estimator in the current domain |

## 5. What could be useful to us

### Post-event prognosis preparation

Use a suitably selected population to estimate conditional post-event survival, keeping attained age, time since event, sex, calendar period and disease definition explicit. Its finite-horizon output could inform a restricted-prognosis analysis. Full `RemainingLifeAtAge` requires a justified tail, not relabelling the package's restricted area.

### Independent numerical comparisons

Feed the same explicit survival grid to both implementations and align conditioning and trapezoidal integration. Verify remaining area, terminal survival, upper-age boundary and cause decomposition separately. This tests numerical handling, not Brodin's scientific attribution assumptions.

### Reporting vocabulary

Separate years lost before an age limit, excess loss against a reference, expected remaining years and onset-age-weighted population averages. Retaining all four quantities avoids ambiguous “LYL” labels in scientific discussions.

## 6. What is substantially different or cannot be transferred

Excess mortality in an observed disease population is not identified as radiation-caused. A bootstrap cannot fix selection bias, confounding, inadequate follow-up or an unsupported survival tail. General-population life tables cannot supply cancer-specific post-event prognosis by themselves.

Cause-specific loss before a fixed age is not the counterfactual gain from eliminating that cause: competing deaths may replace it. A weighted cohort average is not automatically an individual prediction. No value from this package substitutes for dose, HR-1, or a baseline incidence hazard.

## 7. Implementation observations and verification limits

- The individual-data path linearly integrates step-function survival estimates. Trapezoids differ from exact step-function area, especially with coarse gaps; comparisons must agree on the convention.
- `estimate_lyl` filters curve rows at or before `tau` and replaces the final retained time with `tau`, rather than explicitly appending a boundary while preserving the previous knot. Boundary cases require synthetic tests before treating it as an integration oracle.
- Aggregated rate conversion can give invalid survival if $r\Delta a>1$; the approximation also accumulates differences at high mortality. Do not import that transformation without unit/range checks.
- When both survival and rates are supplied, the inspected branching prefers survival, although its informational message says survival is ignored. Supply only one representation to avoid ambiguity.
- Reference life-table uncertainty is not automatically included when only patient bootstrap results are compared with a fixed reference curve.
- Presence on CRAN and association with a methodological paper do not establish numerical correctness for every grid, horizon or rate definition. No package was executed in this review.

## 8. Recommendation and next verification

**Retain as an empirical survival/prognosis reference and restricted-area comparator.** Start with explicit survival inputs, not inferred “annual rates”. Check irregular knots, censoring, delayed entry, competing events and the horizon boundary against hand-computed areas. Do not replace our typed remaining-life distinctions or automatically port its rate approximation.
