# SUMRAY: population radiation-risk projection and uncertainty

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [JapanHealthPhysicsSociety/SUMRAY](https://github.com/JapanHealthPhysicsSociety/SUMRAY).
- Reviewed revision: [`58203f7299f9`](https://github.com/JapanHealthPhysicsSociety/SUMRAY/tree/58203f7299f9f5ce500e29ef2c4ed16290f7c1da).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Sasaki et al. (2023), *SUMRAY: R and Python Codes for Calculating Cancer Risk Due to Radiation Exposure of a Population*, Journal of Radiation Protection and Research 48(2), 90–99. DOI: [10.14407/jrpr.2022.00213](https://doi.org/10.14407/jrpr.2022.00213).
- Model provenance identified by the source: Ozasa et al. (2012), Life Span Study Report 14 mortality, 1950–2003; Grant et al. (2017), Life Span Study solid-cancer incidence, 1958–2009. Consult those primary studies before interpreting fitted coefficients.
- Inspected: README, complete R `CER.R`, mortality/incidence model definitions, and repository data inventory. Python implementation was not separately audited for numerical parity. MIT licence.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

R and Python implementations of radiation-associated attributable rates and cumulative excess risks for all solid cancers. It provides explicit ERR/EAR transfer, age effects, repeated exposures, survival weighting and correlated model-parameter sampling. These are directly relevant to evidence provenance and prospective event-rate calculation in our project.

It projects risk under population radiation-exposure models, not radiotherapy-specific organ toxicity or loss of remaining life after a complication.

## 3. Formulation and code workflow

### Response functions

The R [mortality model](https://github.com/JapanHealthPhysicsSociety/SUMRAY/blob/58203f7299f9f5ce500e29ef2c4ed16290f7c1da/R/R/RiskModel_LSSAllSolidCancerMortality.r) and [incidence model](https://github.com/JapanHealthPhysicsSociety/SUMRAY/blob/58203f7299f9f5ce500e29ef2c4ed16290f7c1da/R/R/RiskModel_LSSAllSolidCancerIncidence.r) use the same response structure with distinct coefficients and covariance matrices:

$$r(D,e,a,s)=\beta_D D\exp\{\beta_e(e-30)/10+\beta_a\log(a/70)\}
[1+z_s\beta_s]\,\ell(a-e),$$

$$\ell(a-e)=\frac{1}{1+\exp[-(a-e-7.5)]},\qquad z_s=-1\ (male),\ +1\ (female).$$

For ERR the result is dimensionless; EAR requires annual-rate units. The logistic factor gradually introduces latency rather than imposing a hard cutoff. Multiple exposures contribute additively to age-specific response. This is a population risk-model assumption, not a mechanistic treatment-fraction repair model.

### Survival and cumulative projection

[CER.R](https://github.com/JapanHealthPhysicsSociety/SUMRAY/blob/58203f7299f9f5ce500e29ef2c4ed16290f7c1da/R/R/CER.R) converts annual all-cause mortality to survival by exponentiating cumulative rates, then conditions on survival to the earliest exposure age. Its intended calculation can be summarized as

$$CER\approx\sum_{a>e_{min}}^{a_{max}}
\frac{S_{pop}(a)}{S_{pop}(e_{min})}
\left[w_{ERR}\lambda_{0,j}(a)\sum_m ERR_m(a)+w_{EAR}\sum_m EAR_m(a)\right]\Delta a.$$

The implementation uses annual sums after any five-year-table interpolation. This is baseline-survival-weighted excess risk, not a newly fitted exposed cohort's exact first-event probability. Incidence and mortality variants need different matching baseline rates.

### Uncertainty

`MASS::mvrnorm` samples correlated coefficients using each model's covariance matrix. The same sampled coefficient vector is reused across exposure ages within a simulation, preserving within-model correlation across the exposure history. ERR and EAR coefficients are sampled separately; cross-model covariance is not represented. Weighted draws yield age-specific and total risk summaries and quantiles.

## 4. Similarities and differences from our domain

| Dimension | SUMRAY R calculation | Our domain |
| --- | --- | --- |
| Relative-risk path | Baseline rate × ERR | Baseline hazard × excess hazard ratio |
| Absolute-risk path | Direct EAR | Could enter through distinct validated excess-rate evidence |
| Survival | General population, earliest-exposure conditioning | Declared survivor cohort |
| Exposures | Vector of ages and doses | Current Brodin adapter uses one exposure age |
| Endpoint | All solid cancer incidence or mortality | Organ cancers, MI and HF |
| Output | Integrated excess events/risk | Integrated years lost |
| Uncertainty | Coefficient covariance sampling | No built-in sampler in the current kernel |

## 5. What could be useful to us

### Parameter uncertainty design

Keep coefficient estimates, covariance, publication, population and units together. Sampling outside pure calculation functions would permit uncertainty propagation without changing deterministic kernels. Draws shared across plans would support paired plan-difference distributions; this would be a proposed extension, not an existing feature verified here.

### Explicit transfer strategy

Distinguish ERR-based transfer from EAR-based transfer and retain the mixture weights as assumptions. A source population's relative-risk model and its absolute rates do not automatically transfer together to Denmark. SUMRAY's arithmetic mixture also differs from the TOPAS extension's geometric mixture.

### Independent survival-weighted rate checks

With matched artificial baseline rates, a single exposure and fixed coefficients, its relative-risk branch could check our excess-rate and survival-weighting arithmetic. It cannot validate our prognosis difference or the entire LYL result without added, compatible consequence evidence.

## 6. What is substantially different or cannot be transferred

All-solid-cancer coefficients are not organ-specific Brodin slopes. Radiation-protection exposure histories cannot be assumed valid at therapeutic doses, where cell killing, fractionation and repopulation can alter the response. Code comments permit “Gy or Sv”, whereas our `AbsorbedDose` is explicitly Gy: radiation quality and weighting need separate, justified handling.

The default Japanese baseline and mortality tables are not Danish survivor evidence. The latency ramp is part of these LSS model definitions and must not silently replace our explicit integration window. Incidence and mortality models remain distinct despite their similar mathematical form.

## 7. Implementation observations and verification limits

- Survival normalization uses positional indexing `survp[min(agex)+1]`. Correctness depends on a compatible zero-based annual-age origin and integer exposure ages; the public checks do not fully establish that invariant for arbitrary supplied tables.
- Five-year interpolation constructs ages starting at one and uses a special initial interpolation rule. Its alignment with the denominator indexing needs explicit tests.
- Separate mortality and baseline age grids are checked for spacing but not comprehensively matched by an age-key join. Positional multiplication can misalign custom data.
- Weight checks enforce length and exact sum one but do not explicitly enforce nonnegative finite weights.
- Multivariate normal draws can generate nonphysical coefficient combinations or negative rates; interpretation and treatment of such draws need review, rather than automatic clipping.
- Parameter uncertainty is represented; uncertainty in baseline rates, life tables, transfer weights and model family is not jointly propagated by the inspected function.
- Python/R equivalence, published numerical examples and confidence-interval coverage were not reproduced here.

## 8. Recommendation and next verification

**Retain as a radiation-risk and uncertainty reference, not a ready-made therapeutic LYL model.** First check the single-exposure zero-dose case, age-key alignment, annual versus five-year inputs, coefficient units and covariance ordering. Independently verify incidence versus mortality tables. Only then consider a generic excess-rate adapter or an external uncertainty workflow, keeping all-solid-cancer model choice separate from Brodin.
