# pseudorel: relative-survival pseudo-observation methods

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [pseudorel/supp_material](https://github.com/pseudorel/supp_material).
- Reviewed revision: [`065be7c7e9e2`](https://github.com/pseudorel/supp_material/tree/065be7c7e9e22e0d4a6a5ce115b79c88aac44183).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Kipourou, Pohar Perme, Rachet and Belot, *Direct modeling of the crude probability of cancer death and the number of life years lost due to cancer without the need of cause of death: a pseudo-observation approach in the relative survival setting*. Biostatistics 23(1), 101–119 (2022; online 2020). DOI: [10.1093/biostatistics/kxaa017](https://doi.org/10.1093/biostatistics/kxaa017).
- [relsurv reference manual](https://cran.r-universe.dev/relsurv/doc/manual.html), particularly `cmp.rel` and `summary.cmp.rel`; current documentation is not proof of identical behaviour in the historical version.
- Inspected: README, complete Rnw tutorial and simulation script. No explicit software licence was found in the reviewed tree.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

A method for estimating cancer-associated and background mortality components using observed all-cause patient survival and matched population mortality when individual cause of death is unavailable or unreliable. It can potentially support evidence preparation for post-cancer prognosis in our incidence pathway.

## 3. Formulation and code workflow

The conceptual relative-survival decomposition is

$$h_{obs}(t)=h_{pop}(t)+h_{excess}(t).$$

For each component, crude cumulative mortality incorporates survival from both components:

$$F_j(t)=\int_0^t S_{obs}(u)h_j(u)\,du,\qquad LYL_j(\tau)=\int_0^\tau F_j(t)\,dt.$$

The [tutorial](https://github.com/pseudorel/supp_material/blob/065be7c7e9e22e0d4a6a5ce115b79c88aac44183/Supplementary_material_B%5Bpseudo_turorial%5D.Rnw) calls `relsurv::cmp.rel` once on everyone and once per excluded subject, then computes

$$\widetilde\theta_i=n\widehat\theta-(n-1)\widehat\theta_{-i}.$$

It fits identity-link GEE models to the pseudo-observations, using age, sex and diagnosis year. Crude probabilities are evaluated at several times; the restricted LYL example uses a ten-year horizon. Pseudo-observations are estimation quantities, not observed patient lifetimes or probabilities.

The [simulation](https://github.com/pseudorel/supp_material/blob/065be7c7e9e22e0d4a6a5ce115b79c88aac44183/comprisk_simul_sdh_rs.R) combines a specified subdistribution hazard with population mortality, derives a compatible cause-specific hazard numerically and samples event times and causes. It is a data generator, not a radiotherapy dose-response model.

## 4. Similarities and differences from our domain

| Dimension | Relative-survival supplement | Our domain |
| --- | --- | --- |
| Data | Patient all-cause follow-up + population life tables | Caller-supplied prognosis and hazards |
| Excess | Patient mortality above population background | Radiation-associated excess event hazard |
| Loss | Cause-component restricted CIF areas | Event rate × remaining-life consequence |
| Cause labels | Not needed for estimation under assumptions | Endpoint must be explicitly matched |
| Dose | Absent | Required by dose-response adapter |

## 5. What could be useful to us

A future evidence adapter could use relative-survival methods to estimate post-event survival when death certificates are unreliable. With sufficiently justified follow-up and tail modelling, those survival estimates could contribute to post-event remaining life evidence. The population-mortality linkage is also relevant to age/calendar-time matching.

## 6. What is substantially different or cannot be transferred

Cancer-associated excess mortality is not automatically radiation-attributable mortality. Patient/reference differences in smoking, deprivation and comorbidity can affect that attribution. A two-component cancer/background model cannot separately identify every cardiac and second-cancer endpoint in our catalogue.

Ten-year loss is not full remaining life. The current kernel requires matched remaining-life evidence; it does not infer survival beyond the observed horizon or distinguish treatment causation from cancer-related excess mortality.

## 7. Implementation observations and verification limits

- `times_c` is computed in years but passed directly into `summary(fit, times=times_c)` after `cmp.rel` is fitted in days. The current interface expects days: the crude-probability evaluation appears mis-scaled. The separate `tau=3652.41` is correctly expressed in days for ten years. Current documentation says areas are returned in years; an extra division of those areas would be inappropriate.
- A local author filesystem path and historical package versions prevent clean reproduction without adaptation.
- The simulation starts a cluster without an explicit shutdown, and substitutes administrative censoring when event-time root finding fails. Numerical failures and true event-free follow-up should be distinguished.
- Its derived cause-specific hazard has a denominator requiring positivity; supplied subdistribution and population hazards are not automatically compatible under arbitrary parameters.
- No automated test suite was found. Historical-version behaviour and generated outputs were not executed here.

## 8. Recommendation and next verification

**Retain as a post-event evidence-estimation reference, not a domain replacement.** Confirm time units against the exact installed `relsurv` version, reproduce a synthetic example with known components, and establish population comparability before estimating prognosis from registry data.
