# Rechner 2020 compared with the LifeYearsLost domain

Review date: 2026-11-05. Status: scientific comparison and implementation notes; no new calculation is implemented by this document.

## References and review scope

1. Rechner LA, Modiri A, Stick LB, et al. **Biological optimization for mediastinal lymphoma radiotherapy - a preliminary study.** Acta Oncologica. 2020;59(8):879-887. DOI: [10.1080/0284186X.2020.1733654](https://doi.org/10.1080/0284186X.2020.1733654). [Public full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC7446040/).
2. Supplementary material accompanying that article: especially Tables S1-S4 (parameters and population rates), S5-S6 (patient results), S7 (gEUD sensitivity), and Figures S2, S6-S9.
3. Brodin NP, et al. **Life years lost - comparing potentially fatal late complications after radiotherapy for pediatric medulloblastoma on a common scale.** Cancer. 2012;118(21):5432-5440. DOI: [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536).

The initial comparison used the combined domain source identifying commit [c192c0e05e76476c3b4efc8959768b36bf0ddd57](https://github.com/emintavlayan/risk-based-radiotherapy-planning/tree/c192c0e05e76476c3b4efc8959768b36bf0ddd57). Before adding this document, the current LifeLoss.fs and Brodin2012.fs on feature/brodin-2012-domain were checked against the relevant findings. This is not a numerical reproduction of the paper, an audit of every repository revision, or independent validation of the underlying epidemiological studies.

Paper statements, algebraic consequences, unresolved interpretations, and implementation recommendations are distinguished below. Page references are printed journal pages; supplementary pages use their own numbering.

## Main finding

Rechner 2020 retains the dose-to-excess-hazard concept but calculates a **dimensionless outcome penalty**. Our domain calculates **expected life years lost**, measured in years.

The difference is structural. Replacing the Brodin coefficients with Rechner coefficients would not implement Rechner Equation 4. Conversely, returning its penalty as ExpectedLifeYearsLost would give the result the wrong meaning and units.

## 1. Equations and dimensions

### Existing domain: incident-event pathway

LifeLoss.atAge followed by LifeLoss.integrate represents:

$$
LYL_x =
\int_{a_{\min}}^{a_{\max}}
S_{\mathrm{survivor}}(a)
\,ERR_x
\,\lambda_{\mathrm{inc},x}(a,s)
\,[L_{\mathrm{population}}(a)-L_{\mathrm{after\ event},x}(a)]
\,da.
$$

Here ERR denotes the non-negative excess hazard ratio, HR minus one. Rates are in inverse years; remaining-life differences and integration intervals are in years. The integrated output is in years.

The fatal-heart-failure pathway instead uses:

$$
LYL_{\mathrm{HF}} =
\int
S_{\mathrm{survivor}}(a)
\,ERR_{\mathrm{HF}}
\,\lambda_{\mathrm{mort},\mathrm{HF}}(a,s)
\,L_{\mathrm{population}}(a)
\,da.
$$

The distinction is encoded by IncidentEvent and FatalHeartFailure in ConsequenceEvidence, and by distinct incidence and mortality hazard types.

### Rechner 2020: Equation 4, page 882

$$
p_x =
w_x\,PFS(D_{\mathrm{target}})
\,ERR_x(D_x)
\int_{e+5}^{80}
\dot h_{\mathrm{gen.pop.},x}(a,s)
\,S_{\mathrm{gen}}(a,s)\,da.
$$

The paper writes the excess ratio as hr_excess,x. Exposure age is e, attained age is a, and s denotes sex.

There is **no remaining-life factor** in this equation. All factors outside the integral are dimensionless, and the rate multiplied by da is dimensionless. Consequently p_x is dimensionless.

| Component | Existing domain | Rechner 2020 |
| --- | --- | --- |
| Dose response | Supplied excess HR; publication adapter available | Excess HR from mean OAR dose |
| Baseline rates | Distinct incidence and mortality hazards | Cancer incidence; cardiac mortality in supplement |
| Survival | Externally supplied attained-age survivor survival | Target-dependent five-year PFS multiplied by population survival |
| Event consequence | Years lost after event, or remaining life after fatal event | Mortality-related weight |
| Bounds | Explicit; Brodin printed-bounds helper uses e+1 to 110 | e+5 to 80 |
| Output | ExpectedLifeYearsLost, years | Outcome penalty, dimensionless |
| Target control | Not calculated in this domain | PFS and recurrence penalty included |

Interpretation: mortality weighting does not distinguish the years lost from otherwise identical fatal events occurring at different attained ages. It therefore cannot substitute for remaining-life evidence while preserving LYL semantics.

## 2. Similarities that can be retained

Both formulations combine organ dose, excess relative hazard, age- and sex-specific population evidence, survival weighting, and attained-age integration.

DoseResponse.linear can evaluate the linear responses with an explicitly supplied slope. ExcessHazardRatio correctly represents HR-1, without imposing a probability upper bound. The separation of HazardRatio from ExcessHazardRatio prevents adding or removing one at the wrong stage.

The incidence/mortality distinctions in Quantities.fs remain necessary. DifferentialDvh.meanDose can supply mean OAR dose from a complete differential DVH. Population evidence and publication-specific coefficients should remain separate.

The paper's factorization outside the age integral is valid for its specified dose response and constant weights. An attained-age-dependent response or weight would need to remain inside the integral.

## 3. Endpoints and dose-response coefficients

Source: supplementary Table S1, page 9. Doses are in Gy; slopes have units Gy^-1.

| Endpoint | Brodin2012 adapter | Rechner 2020 |
| --- | --- | --- |
| Lung cancer | 0.194 × mean lung dose | 0.141 × mean lung dose |
| Female breast cancer | 0.149 × mean breast dose | 0.149 × mean glandular breast dose |
| Heart failure | 0.117 × mean heart dose | Not included |
| Myocardial infarction | 0.096 × mean heart dose | Not included as that endpoint |
| Coronary heart disease, CRF=0 | No explicit endpoint | 0.070 × mean heart dose |
| Coronary heart disease, CRF>0 | No explicit endpoint | 0.097 × mean heart dose |
| Stomach cancer | Included | Not included |
| Thyroid cancer | Nonlinear differential-DVH response | Not included |

The lung slope is approximately 27.3% lower than the existing adapter's slope. This compares parameter transcriptions, not the validity or applicability of the underlying studies.

CHD is not interchangeable with myocardial infarction or heart failure. Matching the dose descriptor does not establish matching endpoint definitions. A future adapter needs an explicit CHD endpoint and matching baseline evidence.

Rechner uses the same CHD and lung excess-response slopes for males and females. Sex dependence still enters through population rates, population survival, and the CHD conversion weight. Breast cancer is N/A for males in Table S1.

Brodin2012.reportedSexComparison preserves reported female-to-male comparisons as evidence of unresolved statistical meaning. These must not automatically become multipliers of Rechner's excess ratios, or be applied merely because sex-specific background rates are also supplied.

The breast descriptor is glandular tissue. A supplied breast contour/DVH must match that definition; the coefficient alone cannot establish that it does.

## 4. Baseline-rate semantics and mortality weights

Sources: supplementary Tables S2-S4, pages 10-11.

| Endpoint | Baseline table | Weight | Meaning |
| --- | --- | --- | --- |
| CHD | All-heart-disease mortality | Male 0.63; female 0.54 | Approximate fraction of heart-disease deaths attributed to CHD |
| Lung cancer | Lung-cancer incidence | 0.823 | 1 - five-year survival, 1 - 0.177 |
| Breast cancer | Female breast-cancer incidence | 0.103 | 1 - five-year survival, 1 - 0.897 |

The main text describes the baseline quantity generically as population incidence. The supplement explicitly supplies **death rates** for the cardiac calculation. A literal implementation should preserve the supplement's distinction.

For CHD the reported fractions derive from:

$$
w_{\mathrm{CHD,male}} = 133.5/210.9 \approx 0.63,
\qquad
w_{\mathrm{CHD,female}} = 71.6/131.8 \approx 0.54.
$$

The paper assumes these fractions are age independent. Whether the code used rounded weights or the underlying quotients remains a reproduction detail.

If starting with all-heart-disease mortality, apply this fraction once. If starting with CHD-specific mortality, do not apply it again.

The cancer weights are fixed approximations derived from five-year survival. They do not specify age-dependent lifetime fatality, time from diagnosis to death, or remaining years lost. In particular, one minus relative survival is not automatically an observed individual cause-specific death probability.

### Rate units

All supplementary background-rate tables are expressed as **percent per year**, including the heart-disease death rates. Convert to inverse years before calculation:

$$
0.190\%\text{/year} = 0.00190\text{/year}.
$$

AnnualRate.create expects the converted rate. AnnualRate.fromPer100000 is for a different denominator. Neither an unconverted percentage nor an annual event probability is interchangeable with a hazard.

## 5. Survival weighting and conditioning

The effective weighting in Rechner Equation 4 is:

$$
S_{\mathrm{effective}}(a)
= PFS(D_{\mathrm{target}})\,S_{\mathrm{gen}}(a,s).
$$

This uses a single five-year PFS value across the entire attained-age integration. It is not an age-dependent HL overall-survival trajectory.

Interpretation: progression-free survival and overall survival are distinct endpoints. Multiplication by PFS discounts the modeled late-effect contribution from patients who progress; it does not model those patients' subsequent survival or late effects.

The printed notation S_gen(a,s) does not explicitly display conditioning on being alive at exposure age. If the supplied life-table curve is survival from birth, conditional survival would be:

$$
S_{\mathrm{gen}}(a\mid e,s)
= \frac{S_{\mathrm{gen}}(a,s)}{S_{\mathrm{gen}}(e,s)}.
$$

If the input is already conditional, dividing again is wrong. The article's notation alone does not settle exactly how the implementation constructed this curve.

Our SurvivalProbability validates a number in [0,1]; it does not prove cohort definition or conditioning. Provenance must identify the starting cohort, source table, conditioning age, and whether another survival factor has already been included. Avoid applying PFS twice.

## 6. Latency, horizon, and empty windows

Rechner explicitly assumes a five-year latency and integrates to attained age 80. IntegrationWindow.printedPaperBounds instead transcribes Brodin's printed e+1 to 110 bounds without asserting biological latency.

IntegrationWindow.create can express Rechner's nonempty bounds with latency 5 and upper age 80. It rejects an upper age that does not exceed the start.

For exposure age e >= 75, no late-effect interval remains under Rechner's horizon. Reproduction needs an explicit zero-contribution policy for that case, rather than a negatively oriented integral or an invalid-input value silently dropped from results.

The supplement notes that the two patients aged 76 had zero reported total risk. At reference target coverage p_DR is also zero. This is a consequence of the horizon and reference penalty; it does not imply absence of clinical risk. A compromised target could still produce positive recurrence penalty.

## 7. Target control and recurrence penalty

Sources: Equations 2-3 and surrounding text, page 881.

$$
PFS(D) = 0.872^{HR(D)},
\qquad
p_{\mathrm{DR}} = 0.872 - PFS(D).
$$

The reference PFS is 0.872 for full-dose treatment. HR here is a **total hazard ratio for the progression endpoint**. It must not be confused with the excess ratios used for OAR late effects.

The text describes linear interpolation between HR values at 0, 20, and 30.6 Gy. It gives HR(0)=2.44 and HR(30.6)=1. The 20 Gy value is internally inconsistent:

| Passage on page 881 | HR at 20 Gy |
| --- | --- |
| Initial description of estimated HRs | 1.44 |
| Subsequent explanation of trial estimate used for interpolation | 1.49 |

Consequences calculated directly from the printed PFS equation:

| HR | PFS | p_DR |
| --- | --- | --- |
| 1.00 | 0.872000 | 0 |
| 1.44 | 0.821001 | 0.050999 |
| 1.49 | 0.815398 | 0.056602 |
| 2.44 | 0.715913 | 0.156087 |

The alternative 20 Gy inputs differ by approximately 0.56 percentage points in p_DR. Do not silently resolve this discrepancy without author clarification or verified original code.

p_DR is **excess progression probability relative to the full-dose reference**, because:

$$
[1-PFS(D)]-[1-0.872] = 0.872-PFS(D).
$$

It is not the absolute progression probability. At full dose the reference progression probability is 12.8%, while the penalty is zero.

## 8. Target gEUD and dose capping

Sources: page 881-882 and supplementary sensitivity analyses.

The article caps each target voxel before calculating gEUD:

$$
d_i'=\min(d_i,30.6\ \mathrm{Gy}),
\qquad
gEUD =
\left(\frac{1}{n}\sum_{i=1}^{n}(d_i')^{a_{\mathrm{gEUD}}}\right)^{1/a_{\mathrm{gEUD}}}.
$$

The symbol a_gEUD here is an exponent, distinct from attained age a elsewhere.

The base analysis uses exponent 1. Sensitivity analyses use -22, -13, -9, -5, -1, and 1. Negative exponents increase sensitivity to low-dose regions.

Capping **before** aggregation is not equivalent to capping the final mean or gEUD. It prevents high-dose voxels from compensating for cold target regions through this calculation. An implementation with negative exponents also needs an explicit mathematically appropriate zero-dose boundary treatment.

The reviewed domain does not calculate target gEUD, target-dependent PFS, or recurrence penalty. DifferentialDvh.meanDose does not cap target doses.

The authors identify the lack of clinical PFS evidence for partial target irradiation as a major limitation. Applying uniform-dose trial evidence to heterogeneous target doses through gEUD is an additional modeling assumption. They also report sensitivity to the exponent and to target-coverage/hotspot constraints. These findings should accompany any use of the objective to compare compromised target coverage.

## 9. Total objective and its coupling

Equation 1 defines:

$$
p_{\mathrm{tot}}
=p_{\mathrm{DR}}+p_{\mathrm{CHD}}+p_{\mathrm{LC}}+p_{\mathrm{BC}}.
$$

The base objective assigns equal coefficients to recurrence and the mortality-related terms. Those objective preferences are conceptually separate from the endpoint-specific mortality conversion weights.

Let K be the sum of late-effect terms before multiplication by PFS:

$$
K = \sum_x w_x\,ERR_x
\int_{e+5}^{80}\dot h_{\mathrm{gen.pop.},x}(a,s)
S_{\mathrm{gen}}(a,s)\,da.
$$

Then, by algebra:

$$
p_{\mathrm{tot}}=0.872-PFS+PFS\,K.
$$

Consequently, reducing PFS increases recurrence penalty but decreases every late-effect penalty even if OAR doses remain fixed. This is a structural property of the printed objective. It should appear in intermediate results and sensitivity analysis.

A sum of these penalties is not automatically an exact probability of any adverse outcome. The formula combines a reference-subtracted recurrence term and simplified late-effect terms; it does not specify a complete joint competing-risk process.

Similarly, neither our supplied-survival LYL integrator nor this review alone establishes a full competing-risk recalculation of survival under every treatment-dependent excess hazard.

## 10. Mapping to domain code

| Current element | Reuse or limitation |
| --- | --- |
| DoseResponse.linear | Reusable with explicitly supplied publication-specific slope |
| DifferentialDvh.meanDose | Reusable for matching OAR definition and complete DVH |
| HazardRatio / ExcessHazardRatio | Retain distinction between total and excess association |
| IncidenceHazard / MortalityHazard | Preserve the supplement's different baseline semantics |
| ExcessIncidenceHazard / ExcessMortalityHazard | Reusable transformations where endpoint and evidence match |
| Brodin2012 | Preserve as a separate publication-specific parameter adapter |
| ReportedSexComparison | Evidence, not an automatic response multiplier |
| ConsequenceEvidence.FatalHeartFailure | Endpoint-specific; cannot stand in for CHD |
| LifeLoss.atAge | Requires years-shortening evidence; does not calculate Rechner penalty |
| LifeLoss.integrate | Produces years; must not be reused to mislabel dimensionless results |
| IntegrationWindow.create | Can represent nonempty e+5 to 80 bounds |
| IntegrationWindow.printedPaperBounds | Brodin transcription, not Rechner bounds |
| ExpectedLifeYearsLost | Must retain units of years |
| RemainingLifeAtAge | No equivalent remaining-life term in Rechner Equation 4 |
| RestrictedMeanLife | Finite survival area, distinct from either penalty or full remaining life |

Recommended design, if implementation is later requested:

- Keep a separate Rechner2020 adapter and outcome-penalty workflow alongside LYL.
- Represent CHD explicitly, with cardiac-risk-factor selection and source evidence.
- Keep dimensionless penalty separate from expected years lost. A validated probability type should not be imposed on an arbitrary summed objective without establishing its bounds and interpretation.
- Separate target dose processing, PFS, reference-relative recurrence penalty, baseline-rate integration, mortality conversion, and objective aggregation.
- Preserve per-endpoint intermediate values, selected parameter versions, rate units, survival conditioning, age bounds, target descriptor, and assumptions.
- Keep publication reproduction choices distinct from proposed improvements.

These are recommendations, not implemented types or functions.

## 11. Reproduction questions and validation plan

Before claiming reproduction:

1. Resolve HR(20 Gy)=1.44 versus 1.49 against original implementation or author clarification.
2. Establish exactly how S_gen was sourced, conditioned, interpolated, and extrapolated.
3. Confirm rate-table age-band boundaries, within-band interpolation, and numerical quadrature. Cancer tables end with ages 70-79 while the integral reaches 80; the endpoint convention needs to be explicit.
4. Confirm rounded versus unrounded CHD fractions and correct percentage conversion.
5. Confirm contour definitions, especially glandular breast tissue.
6. Confirm voxelwise capping, gEUD exponent, HR interpolation, and zero-dose behavior for negative exponents.
7. Explicitly reproduce zero late-effect contribution for an empty age window.
8. Validate endpoint contributions and totals against Tables S5-S6, then sensitivity results in S7 and related figures. Published rounded mean doses alone may be insufficient for exact heterogeneous target reconstruction.
9. Keep scientific reproduction distinct from clinical validity and applicability.

Useful equation checks include zero OAR dose giving zero excess late-effect contribution, full reference target coverage giving zero recurrence penalty, percent-to-rate conversion, and different empty/nonempty horizon cases.

No calculation code or publication coefficients are changed by this review.
