# Research questions and interpretation notes

This working note collects questions encountered while implementing published models. It is separate from the software guide and does not assert that the publications contain calculation errors. Some details may be implicit, use different conventions, or require information beyond the sources available to us.

For each item, the table records what we could establish, what remains to clarify, and how the implementation handles it. Further source material or discussion with the authors may resolve these questions.

## Brodin 2012

Source: [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536), Cancer 118 (2012), 5432–5440.

| Location | Observation or question | Review status | Implementation |
| --- | --- | --- | --- |
| Eq. 5, p.5436 | The prose calls m_i fractional volume, but the equation also divides by V, called total volume. Taken literally, the result has inverse-volume units instead of being dimensionless. | Volume convention to clarify | Fractional bins sum directly; absolute-volume bins are normalized exactly once. Both representations give the same result, invariant to organ-volume scale. |
| Table 1 heading | The heading says excess HR/Gy, but its cells contain dose-multiplied responses. k has Gy^-1; kD is dimensionless. | Unit convention to clarify | Coefficients carry inverse-gray units; computed excess HR is dimensionless. |
| Eqs. 2/4 and Fig. 2 | The age-specific quantity is called LYL, and Figure 2 prints years. S × hazard × life shortening is dimensionless until multiplied by an age interval. | Integrand versus finite-bin notation | AgeSpecificLoss contains LifeLossDensity; integration yields ExpectedLifeYearsLost in years. |
| Eq. 4 | The rate is written h_mort(D,e,s,a), without an explicit “excess” subscript or full dose-to-mortality formula. | Mortality interpretation to clarify | The implemented interpretation is excess HR × baseline cause-specific mortality. It gives zero attributable loss at zero dose. Treating h_mort as total population mortality would not. |
| Table 1 / Fig. 2 | Lung male:female is 1:1.7, while the female example uses ×1.21. We did not establish the derivation connecting these quantities in the sources reviewed. | Missing normalization information | No automatic derivation. A supplied excess-hazard multiplier has explicit provenance; the reported comparison is a different type. See the research record below. |
| Fig. 2 | Direct multiplication of unrounded inputs gives 0.0288114430032, rounding to 0.029 at three decimals. The displayed rounded weighted rate 0.0015 multiplied by 18.6 gives 0.0279, rounding to 0.028. | Intermediate rounding explains discrepancy | Document the rounding explanation; do not require example reproduction or infer a substantive numerical error. |
| Eq. 3 | Lower bound is e+1. The paper does not derive this as an endpoint-specific biological latency. | Time convention to clarify | Explicit printedPaperBounds helper; no hidden latency default. |
| Thyroid β1(e) | 2.81-0.15e crosses zero at e≈18.733 years. Extrapolation predicts negative excess response. | Applicability beyond the source setting | Reject negative resulting coefficients in the transcription. Generic equations accept externally established coefficients. The treated sample ages are not imposed as a mathematical boundary. |
| Table 1 thyroid / ref.13 | Brodin labels the thyroid dose-response source a nested case-control study; Bhatti 2010 describes a cohort analysis using Poisson regression. | Source description to clarify | Document it; retain Brodin coefficients as transcribed rather than silently substituting source parameters. |
| Thyroid sex modifier / ref.13 | Bhatti distinguishes ERR and EAR effect modification: age modifies ERR; sex and time since exposure modify EAR in its adjusted analyses. Transferring modifiers between these scales needs justification. | Effect-measure interpretation to clarify | No automatic transfer of a sex ratio into excess HR. |
| HF response transfer | Applying an incidence-derived radiation association to population cause-specific mortality assumes a corresponding mortality effect. | Additional scientific assumption | Separate incidence and mortality hazard types; mark the mortality interpretation for review. |

## Model assumptions and scope questions

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

## Further clarification

1. Confirm the Eq.5 volume interpretation.
2. Keep unexplained modifiers as explicit sourced inputs while investigating their derivation and intended estimand.
3. Confirm the intended excess-mortality construction for heart failure.
4. Recover the actual hazard and survival data, with rate definitions and starting cohorts.
5. Decide whether to reproduce the published attribution approximation or develop a joint competing-risk model as a separately named model.
6. Verify each dose-response coefficient against its cited primary source before considering clinical use.

No matching correction was found in the targeted DOI/erratum search performed on 4 November 2026. This search is not proof that none exists. Review of external sources was focused on the thyroid source and publication record; the other endpoint source studies have not been independently audited in full.


## Sex-adjustment research record

Table 1 reports a lung male:female comparison of 1:1.7; Figure 2 applies a female multiplier of 1.21 to an excess hazard ratio. These quantities have different roles. We reviewed the paper, accessible source records and relevant mathematical relationships, but could not establish the derivation connecting them. This is a documentation gap in our present understanding, not a finding that either number is incorrect.

A between-group comparison does not by itself determine a multiplier relative to a fitted reference. For example, an arithmetic reference mixture would give r/(1-p+p*r), whereas a centered proportional-hazards model can use r^(1-p). Reference weights and the underlying effect measure would need to be established before either relationship could explain this example. These are possible mathematical constructions only; neither was identified as the authors' method.

The [single-event proportional-hazards relationship](risk-and-hazard.md) is a separate calculation: for RR>1 and a positive valid baseline risk, its resulting HR exceeds RR. It therefore does not explain a change from 1.7 to 1.21 for the same comparison.

Further clarification would require the statistical meaning of the reported sex comparison, the reference population for the dose coefficient, and the normalization used for the multiplier. Brodin cites Travis 2005 for the lung dose response and Swerdlow 2000 / Bassal 2006 for age and sex information.

### Sources consulted and access limits

- The supplied Brodin final published PDF, its publication record and accessible author manuscript.
- Bhatti 2010 [full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC3080023/), including the separate ERR and EAR analyses. Its methods describe a cohort analysis using Poisson regression; further clarification would help map this to Brodin's source description and age formula.
- Travis and Gilbert 2005, Radiat Res 163(6):695–696, [PMID 16044495](https://pubmed.ncbi.nlm.nih.gov/16044495/). The accessible record has no abstract and did not establish the multiplier's derivation.
- Retrieval of the cited Swerdlow and Bassal ASCO full texts was unsuccessful (403/inaccessible). Their full analyses were not independently reviewed.
- [Cox model documentation](https://www.mathworks.com/help/stats/cox-proportional-hazard-regression.html), for the distinction between a between-group HR, a centered-reference multiplier and a survival-probability relationship.

Research recorded on 4 November 2026. No retrieved source established the connection between the reported ratio and the multiplier. The implementation therefore accepts supplied quantities with explicit provenance and does not infer this relationship.
