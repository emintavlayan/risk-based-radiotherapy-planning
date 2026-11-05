# Charrier: life years lost among childhood cancer survivors

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [T-Charrier/Life-Years-Lost-Among-CCS](https://github.com/T-Charrier/Life-Years-Lost-Among-CCS).
- Reviewed revision: [`386cf03f0b5c`](https://github.com/T-Charrier/Life-Years-Lost-Among-CCS/tree/386cf03f0b5c1eb4ecff76d8aa5c69c22c05668e).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Charrier et al. (2024), *Life years lost by childhood cancer treatment and health related late effects among childhood cancer survivors*, Cancer Epidemiology 93, 102692. DOI: [10.1016/j.canep.2024.102692](https://doi.org/10.1016/j.canep.2024.102692); [PubMed](https://pubmed.ncbi.nlm.nih.gov/39514959/).
- Statistical foundation: Andersen (2013), *Decomposition of number of life years lost according to causes of death*. DOI: [10.1002/sim.5903](https://doi.org/10.1002/sim.5903).
- Inspected: the complete `Appendix B_V2.R`, README and GPL-3 licence. The paper abstract and publicly indexed article text establish context; this is not a line-by-line audit of the full published analysis.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

A cohort-analysis workflow for childhood cancer survivors. The associated study includes 7,670 five-year survivors and examines treatment and late effects through landmark analyses. This directly overlaps our clinical setting, but its statistical question is prognosis and associations among survivors, rather than mechanistic comparison of organ doses before treatment.

## 3. Formulation and code workflow

At a landmark age $a$, select subjects under observation and alive, then estimate restricted loss before age $\tau$:

$$LYL(a,\tau)=(\tau-a)-\int_a^\tau S(u\mid T>a)\,du.$$

For competing death cause $j$:

$$LYL_j(a,\tau)=\int_a^\tau F_j(u\mid T>a)\,du.$$

The [script](https://github.com/T-Charrier/Life-Years-Lost-Among-CCS/blob/386cf03f0b5c1eb4ecff76d8aa5c69c22c05668e/Appendix%20B_V2.R) calls `pseudo::pseudoyl(Tstop-lm_pt, status, tau-lm_pt)` from `pseudo_slm_lyl`, computes pseudo-observations and fits Gaussian identity-link GEE models in `estimate_model`. This permits regression effects on a years-lost scale. A pseudo-observation is not an observed individual lifespan and can lie outside the outcome's natural range.

Landmarks run from age 10 to 50. Configurations use a common upper age 60 or ten years after each landmark. Covariates include heart/brain radiation-dose categories, chemotherapy measures, sex, age at cancer, subsequent malignancy, cardiac disease, diabetes and renal disease. Disease-duration categories distinguish recent from earlier events. Missing dose information is handled through multiple imputation, with five imputations in the supplied configuration.

## 4. Similarities and differences from our domain

| Dimension | Charrier workflow | Our domain |
| --- | --- | --- |
| Population | Observed childhood cancer survivors | Declared survivor cohort supplied as evidence |
| Dose | Cohort treatment categories | Mean organ dose or differential thyroid DVH |
| Age origin | Current landmark | Exposure age and attained event age |
| Loss | Restricted survival/CIF integral | Excess event hazard × survival × life shortening |
| Estimation | Pseudo-observation regression | Deterministic functions with supplied parameters |
| Disease history | Updated at landmarks | Post-event prognosis supplied externally |

## 5. What could be useful to us

- A future empirical validation study with correctly matched survivor selection, horizons, endpoint definitions and comparators.
- A separate follow-up tool that updates prognosis after a late effect appears.
- Evidence for studying joint late effects and effect modification instead of automatically summing endpoint losses.
- A reference for modelling treatment and clinical covariates together, with uncertainty estimated from patient follow-up.

## 6. What is substantially different or cannot be transferred

The underlying outcome includes deaths within the age window. An adjusted radiotherapy coefficient is not automatically a causal dose-specific effect, and cannot be substituted for our continuous excess-hazard dose response. Reported lack of statistical interaction does not establish universal additivity of late-effect losses.

Ten-year or pre-age-60 loss is not full lifetime loss. `RestrictedMeanLife` deliberately preserves that distinction; its value must not be wrapped as full `RemainingLifeAtAge` merely to fit the current kernel.

## 7. Implementation observations and verification limits

- Patient RData files and fitted coefficients are not distributed, preventing reproduction of cohort findings from this repository alone.
- `cardio_rep` is recoded to the level `cardio_rep`, then tested against `cardio` when forming `cardio_rep_cut`. As written, this appears to mark the disease-duration category as “Never”.
- `estimate_model` constructs its model data by copying `pseudo` rather than the arbitrary response `y`; the alternative cause-response configuration therefore needs checking.
- Configured interaction expressions are also treated as literal column names when constructing model data. Those configurations need execution-level testing.
- `single_lm_fit_and_plots` returns `all_plot_time_results`, not defined in the distributed script.
- These are static observations about the distributed supplement, not reproduced failures or a judgment about the final study dataset/results.

## 8. Recommendation and next verification

**Retain as a clinically relevant cohort-method reference. Do not port the script into the calculation domain.** Reproduce a synthetic landmark example and resolve the categorical recoding, response selection and interaction handling before considering it a numerical comparator.
