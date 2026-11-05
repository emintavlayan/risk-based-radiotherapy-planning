# Ziersen and Martinussen: causal effects on cause-specific life years lost

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [simonziersen/causal-effect-on-the-number-of-life-years-lost](https://github.com/simonziersen/causal-effect-on-the-number-of-life-years-lost).
- Reviewed revision: [`18b1b91ff46b`](https://github.com/simonziersen/causal-effect-on-the-number-of-life-years-lost/tree/18b1b91ff46b975f47a68b56dc62e858fb6c9e62).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Ziersen and Martinussen (2025), *Causal effect on the number of life years lost due to a specific event: Average treatment effect and variable importance*. Reviewed formulation: [arXiv v1](https://arxiv.org/html/2502.00120v1); DOI [10.48550/arXiv.2502.00120](https://doi.org/10.48550/arXiv.2502.00120). This reference identifies the reviewed preprint, not a claim that no subsequent journal publication exists.
- Inspected: `example-simulation.R` and all six files under `functions/`. No explicit software licence was found in the reviewed tree.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

A semiparametric estimator of the effect of a binary baseline treatment on restricted time lost due to an event, accounting for competing events and censoring. It also estimates a partially linear projection describing treatment-effect modification by a covariate. This is relevant to a future observational comparison of treatment strategies, not a source of radiotherapy dose-response coefficients.

The paper's application concerns antidepressants and event-free days. In that setting the event need not be death. Time after nonfatal treatment failure is not shortened life expectancy.

## 3. Formulation and code workflow

For treatment $b$, covariates $x$, horizon $\tau$ and cause $j$:

$$L_j^b(\tau\mid x)=\int_0^\tau F_j^b(t\mid x)\,dt,$$

$$ATE_j=E_X[L_j^1(\tau\mid X)-L_j^0(\tau\mid X)].$$

Positive ATE means more restricted loss under treatment 1. Identification requires consistency, conditional exchangeability, positivity and conditionally independent censoring. Statistical estimation does not establish those assumptions from the data.

[phiLyl.R](https://github.com/simonziersen/causal-effect-on-the-number-of-life-years-lost/blob/18b1b91ff46b975f47a68b56dc62e858fb6c9e62/functions/phiLyl.R) fits into a workflow that estimates both cause-specific cumulative hazards, censoring survival and treatment propensity. Survival is constructed as $\exp(-\Lambda_1-\Lambda_2)$, cumulative incidence is accumulated from survival-weighted hazard increments, and areas are contrasted. Influence-function terms augment those plug-in areas. [covVimLyl.R](https://github.com/simonziersen/causal-effect-on-the-number-of-life-years-lost/blob/18b1b91ff46b975f47a68b56dc62e858fb6c9e62/functions/covVimLyl.R) supports fitting on other folds and evaluating held-out observations; the example uses one and ten folds with Cox models or random survival forests.

The variable-modification target is a residualized covariance divided by residualized covariate variance. It is not a universal importance score: its magnitude depends on covariate units and a zero linear projection does not rule out every nonlinear interaction.

## 4. Similarities and differences from our domain

| Dimension | Causal estimator | Our domain |
| --- | --- | --- |
| Comparator | Binary treatment intervention | Radiation excess relative to supplied baseline |
| Evidence | Individual follow-up, treatment and covariates | Published parameters and external age-specific evidence |
| Outcome | Integrated cause-specific cumulative incidence contrast | Excess-event attribution kernel |
| Competing events | Jointly enter each scenario | Enter via supplied survivor survival |
| Signed differences | Allowed | Current excess-loss path rejects protective excess HR |

## 5. What could be useful to us

- Define future cohort comparisons of two treatment strategies on an interpretable time scale.
- Estimate population-average effects while adjusting for measured baseline confounders.
- Study heterogeneity of treatment effects without confusing prognosis with treatment benefit.
- Build synthetic competing-risk scenarios for checking a future joint survival implementation.

## 6. What is substantially different or cannot be transferred

The estimator is not a patient-specific mechanistic DVH calculator. A dose-response model plus external life tables does not provide the observational inputs needed to run it. Conversely, an ATE cannot be passed as `ExcessHazardRatio`.

A treatment can change cause-specific loss by changing competing mortality even with no change in that cause's own hazard. The estimand is not the direct biological effect of radiation on one organ, nor the life gained by eliminating a cause while holding everything else fixed. Cross-fitting helps estimation but does not remove unmeasured confounding. Asymptotic inference depends on the paper's stated nuisance-estimator conditions.

## 7. Implementation observations and verification limits

- Modifier extraction uses column `i` instead of requested `l[i]`; nonconsecutive requested subsets need checking.
- The non-cross-fitted T-learner branch later reads `phi_tmp`, which that branch does not create.
- `rfsrcTune` uses the selected `nodesize` for `mtry` too.
- `phiLyl` contains scalar horizon conditionals; multiple requested horizons need dedicated tests.
- Treatment/censoring/survival weights can divide by very small values. Commented propensity clipping is not an active positivity diagnostic and clipping would itself need reporting.
- Source includes a runnable synthetic example, but no automated test suite or dependency lock was found. No calculations were executed for this review.

## 8. Recommendation and next verification

**Retain for future causal cohort research; keep separate from the deterministic LYL domain.** Before using its estimates, reproduce the S-learner example, compare with known generating truth, audit the alternative branches and establish exposure overlap and censoring assumptions.
