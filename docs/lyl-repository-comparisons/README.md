# External code and formulation comparisons for LifeYearsLost

Nine repository reviews supporting scientific discussion of our LYL domain. Each review separates the scientific question, formulation, implementation observations, possible reuse and limits. The last three contain deeper analysis of the dose-response, uncertainty and survival-evidence components most relevant to us.

Reviewed on 2026-11-05. Each file pins the inspected external revision. This is a static source review, not reproduced external numerical validation or clinical approval. No third-party implementation is added to the application, and no domain equations are changed by this documentation.

## Reading map

| Review | Main calculation | Potential role | Priority |
| --- | --- | --- | --- |
| [01 NHS Markov model](01-nhs-markov.md) | Disease-state occupancy and survival differences | Future comorbidity prognosis | Later |
| [02 Charrier childhood survivors](02-charrier-childhood-survivors.md) | Landmark restricted loss and pseudo-observation regression | Survivor follow-up and cohort comparisons | Relevant clinical reference |
| [03 Mortality weighting](03-mortality-life-table-weighting.md) | Observed deaths × remaining life | Life-table source and explanatory example | Limited |
| [04 Causal cause-specific LYL](04-causal-cause-specific-lyl.md) | Treatment effect on integrated cumulative incidence | Future causal treatment-comparison research | Later |
| [05 estimandYLL](05-estimand-yll.md) | Standardized difference of restricted survival areas | Scenario contracts and numerical checks | Useful methodological reference |
| [06 Relative-survival pseudo-observations](06-relative-survival-pseudo.md) | Cancer/background loss without death-cause labels | Registry-based post-event survival estimation | Evidence preparation |
| [07 OpenTOPAS-CancerRisk](07-opentopas-cancer-risk.md) | DVH → OED → ERR/EAR → LAR | Alternative dose-response family and source/data mapping | First to investigate |
| [08 SUMRAY](08-sumray.md) | Survival-weighted excess radiation risk | ERR/EAR transfer and coefficient uncertainty | Second to investigate |
| [09 lillies](09-lillies.md) | Empirical remaining life and excess restricted loss | Prognosis evidence and survival-area checks | Useful evidence reference |

The first six were supplied by the user; the last three were found during the additional GitHub search. The search found no additional directly matching implementation of the complete Brodin radiotherapy LYL workflow. This is a search result, not proof that no such repository exists.

## Our comparison baseline

Local source reviewed: commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae` on `feature/brodin-2012-domain`. At review time, `main` does not contain this domain. A subsequent merge or refactor should not silently change what these comparisons claim to have inspected.

Scientific baseline: Brodin et al. (2012), *Life Years Lost—Comparing Potentially Fatal Late Complications After Radiotherapy for Pediatric Medulloblastoma on a Common Scale*. DOI: [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536).

Read the [domain vocabulary](../domain.md), [scientific workflow](../workflow.md), [Brodin source map](../brodin-2012/sources.md) and the separate [Rechner 2020 comparison](../rechner-2020-comparison.md). The Rechner document is an existing article review; it is not one of the nine repository comparisons.

### Implemented calculation

For incidence endpoints, writing exposure age as e and attained age as a:

$$\Delta h_j(a)=ERR_j(D,e,a)h_{0,j}(a),$$

$$LYL_j=\int_{a_0}^{a_1}S_{surv}(a)\Delta h_j(a)
\left[e_{pop}(a)-e_{post,j}(a)\right]da.$$

For the fatal heart-failure pathway:

$$LYL_{HF}=\int_{a_0}^{a_1}S_{surv}(a)\Delta\mu_{HF}(a)e_{pop}(a)da.$$

`LifeLoss.atAge` constructs the density and auditable intermediate values; `LifeLoss.integrate` uses trapezoids on an explicit complete age grid. `DoseResponse.linear` and `DoseResponse.thyroid` provide generic responses; `Brodin2012.unmodifiedExcessHazardRatio` supplies the publication adapter. See [LifeLoss.fs](../../src/LifeYearsLost/LifeLoss.fs), [DoseResponse.fs](../../src/LifeYearsLost/DoseResponse.fs), [Brodin2012.fs](../../src/LifeYearsLost/Brodin2012.fs) and [Quantities.fs](../../src/LifeYearsLost/Quantities.fs).

`ERR` in these equations means the excess relative hazard expected by this domain, HR−1. An external paper's risk ratio, cumulative risk or vaguely labelled ERR must be interpreted before conversion. Annual hazards have units 1/year and are not annual death probabilities. Survivor survival refers to the declared starting cohort; population and post-event remaining life are conditional on the current event age.

The supplied evidence and coefficients are not fitted by the domain. It does not reconstruct a joint exposed survival model, estimate causal effects from patient data, or establish that overlapping endpoint losses sum to an exact total decrement in life expectancy.

### Distinguish four common meanings of YLL

| Quantity | Typical expression | Relationship to our current result |
| --- | --- | --- |
| Death-record burden | Sum of remaining life at observed death ages | Different population denominator; no prospective radiation attribution |
| Restricted cause-specific loss | Integral of a cause's cumulative incidence before a horizon | Different consequence model and competing-risk interpretation |
| Difference of scenario survival areas | Integral of reference minus exposed survival | Can define total restricted effect when scenarios are fully specified |
| Excess-event consequence attribution | Integral of survival × excess event rate × life shortening | Current Brodin pathway |

They share units, but do not become interchangeable through a type cast or a variable name.

## Cross-cutting decisions

1. Keep dose-response families separate: a Schneider/BEIR adapter would not overwrite the Brodin transcription.
2. Retain restricted versus full remaining life. `LifeLoss.restrictedMeanFromSurvival` returns `RestrictedMeanLife`; `remainingLifeFromSurvival` requires terminal survival zero. There is no inferred tail model.
3. State comparator, starting cohort, horizon, event definition and integration rule before comparing outputs.
4. Keep evidence-estimation workflows outside the deterministic kernel. Cohort fitting, bootstraps and causal inference may produce inputs, but require their own provenance and validation.
5. Treat static code findings as revision-specific verification questions. A public supplement defect does not by itself invalidate a published paper.
6. A useful comparator needs matching inputs and a known expected result. Matching another program is not independent scientific validation if both make the same approximation.

## Concrete follow-up order

- OpenTOPAS: verify carcinoma/sarcoma formulas against the original paper, parameter provenance, age bounds and output scaling; compare OED and age-specific rates first.
- SUMRAY: verify units, age indexing and baseline alignment on synthetic cases; assess covariance sampling as an external uncertainty workflow.
- lillies: compare explicit conditional survival areas with hand calculations, including the final horizon knot; do not treat its rate approximation as an exact hazard conversion.
- Cohort estimators: pursue only when the target study and available follow-up data justify them.

No external package was installed or executed for these reviews. The documentation introduces no dependency, runtime change or scientific-model change.
