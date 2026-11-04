# Equation audit and questions for scientific review

Classification matters: a dimensional contradiction can be established from the printed expression; an unresolved scientific assumption is not proof that the authors calculated incorrectly.

| Location | Finding | Classification | Implementation |
| --- | --- | --- | --- |
| Eq. 5, p.5436 | The prose calls m_i fractional volume, but the equation also divides by V, called total volume. Taken literally, the result has inverse-volume units instead of being dimensionless. | Printed definition/formula inconsistency | Fractional bins sum directly; absolute-volume bins are normalized exactly once. Both representations give the same result, invariant to organ-volume scale. |
| Table 1 heading | The heading says excess HR/Gy, but its cells contain dose-multiplied responses. k has Gy^-1; kD is dimensionless. | Dimensional labelling ambiguity | Coefficients carry inverse-gray units; computed excess HR is dimensionless. |
| Eqs. 2/4 and Fig. 2 | The age-specific quantity is called LYL, and Figure 2 prints years. S × hazard × life shortening is dimensionless until multiplied by an age interval. | Integrand versus finite-bin notation | AgeSpecificLoss contains LifeLossDensity; integration yields ExpectedLifeYearsLost in years. |
| Eq. 4 | The rate is written h_mort(D,e,s,a), without an explicit “excess” subscript or full dose-to-mortality formula. | Ambiguous notation | The implemented interpretation is excess HR × baseline cause-specific mortality. It gives zero attributable loss at zero dose. Treating h_mort as total population mortality would not. |
| Table 1 / Fig. 2 | Lung male:female is 1:1.7, while the female example uses ×1.21. The ratio alone does not determine this factor. | Missing normalization information | No sex-adjustment API. Figure 2 uses 1.21 only in a diagnostic arithmetic reproduction; see [normalization investigation](sex-normalization.md). |
| Fig. 2 | Direct multiplication of unrounded inputs gives 0.0288114430032, rounding to 0.029 at three decimals. The displayed rounded weighted rate 0.0015 multiplied by 18.6 gives 0.0279, rounding to 0.028. | Intermediate rounding explains discrepancy | Test both paths; do not present the difference as evidence of a substantive numerical error. |
| Eq. 3 | Lower bound is e+1. The paper does not derive this as an endpoint-specific biological latency. | Under-explained time convention | Explicit printedPaperBounds helper; no hidden latency default. |
| Thyroid β1(e) | 2.81-0.15e crosses zero at e≈18.733 years. Extrapolation predicts negative excess response. | Demonstrable extrapolation failure | Reject ages outside the applied study sample, 4–15, pending review. No clipping or invented replacement. |
| Table 1 thyroid / ref.13 | Brodin labels the thyroid dose-response source a nested case-control study; Bhatti 2010 describes a cohort analysis using Poisson regression. | Source-design description discrepancy | Document it; retain Brodin coefficients as transcribed rather than silently substituting source parameters. |
| Thyroid sex modifier / ref.13 | Bhatti distinguishes ERR and EAR effect modification: age modifies ERR; sex and time since exposure modify EAR in its adjusted analyses. Transferring modifiers between these scales needs justification. | Source-interpretation concern | No automatic transfer of a sex ratio into excess HR. |
| HF response transfer | Applying an incidence-derived radiation association to population cause-specific mortality assumes a corresponding mortality effect. | Additional scientific assumption | Separate incidence and mortality hazard types; mark the mortality interpretation for review. |

## Additional model limitations

- **External baseline transport.** An internal comparison with unirradiated survivors or siblings is treated as transportable to general-population rates. The paper discusses this assumption. Chemotherapy and inherited predisposition can affect it.
- **Sex adjustment.** Population hazards already depend on sex. A radiation-effect modifier could be legitimate, but a sex ratio of absolute incidence is not automatically an extra relative-hazard multiplier. The cited source must establish the intended estimand.
- **Competing risks.** Shared CCSS survivor survival includes causes that overlap the modeled late events and is not recomputed for each plan. Survival weighting is useful, but does not by itself create an event-free risk set or an exact joint competing-risk model.
- **Lifetime incidence.** An integrated survival-weighted event rate need not be the probability of at least one first event. Adding endpoint incidences also requires care about multiple events in the same person.
- **Endpoint addition.** Cancer, myocardial infarction and heart-failure pathways can overlap. A sum of attributed losses is an approximation unless their joint structure is addressed.
- **Prognosis.** The same prognosis is assumed for treatment-induced and spontaneous events. That is not demonstrated by the equations.
- **Survival extrapolation.** The paper extrapolates a CCSS/general-population survival ratio beyond 30 years and normalizes primary-disease five-year survival to 80%. The original ratio values, fit, age/sex mapping and normalization must be recovered before faithful numerical reproduction. Do not reconstruct them visually from Figure 1.
- **Late-age rates.** Rates above 90 are held at the reported ≥85 group. This is an explicit approximation, not evidence that late-life hazard is constant.
- **Uncertainty.** Parameter sampling omits structural model error. A mean and asymmetric 95% CI do not always identify one lognormal distribution exactly. Paired comparisons require shared parameter draws across plans; cross-endpoint and cross-patient dependence also need specification.
- **Neutrons.** A recommended neutron effectiveness is multiplied by five as a conservative assumption. Absorbed Gy, radiation-weighted quantities and endpoint-specific biological effectiveness must not be silently conflated.
- **Dose-response shape.** A falling high-dose thyroid response is a model of observed cancer occurrence, not a therapeutic instruction to increase thyroid dose.

## Proposed discussion decisions

1. Confirm the Eq.5 volume interpretation.
2. Establish the sex-modifier normalization behind 1.21 and the intended estimand for every endpoint.
3. Confirm the intended excess-mortality construction for heart failure.
4. Recover the actual hazard and survival data, with rate definitions and starting cohorts.
5. Decide whether to reproduce the published attribution approximation or develop a joint competing-risk model as a separately named model.
6. Verify each dose-response coefficient against its cited primary source before considering clinical use.

No matching correction was found in the targeted DOI/erratum search performed on 4 November 2026. This search is not proof that none exists. Review of external sources was focused on the thyroid source and publication record; the other endpoint source studies have not been independently audited in full.
