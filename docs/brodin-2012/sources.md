# Source map

Primary source: supplied final published PDF, Brodin et al., Cancer 2012;118:5432–5440, DOI [10.1002/cncr.27536](https://doi.org/10.1002/cncr.27536). Public [author manuscript](https://pmc.ncbi.nlm.nih.gov/articles/PMC3402656/) and [PubMed record](https://pubmed.ncbi.nlm.nih.gov/22517408/) identify the same work.

| Implemented quantity | Printed source | Units and note |
| --- | --- | --- |
| Breast coefficient 0.149 | Table 1, p.5435; ref.8 Travis 2003 | Gy^-1; female source cohort, no male default |
| Lung coefficient 0.194 | Table 1; ref.10 Travis 2005 | Gy^-1 |
| Stomach coefficient 0.84 | Table 1; ref.14 van den Belt-Dusebout 2009 | Gy^-1; transcribed without unverified correction |
| HF coefficient 0.117 | Table 1; ref.15 Mulrooney 2009 | Gy^-1 |
| MI coefficient 0.096 | Table 1; ref.15 Mulrooney 2009 | Gy^-1 |
| Thyroid β1=2.81-0.15e | Table 1; ref.13 Bhatti 2010 | Intercept Gy^-1; age slope Gy^-1 year^-1 |
| Thyroid β4=0.00164 | Table 1 | Gy^-2 |
| Male:female ratios | Table 1 | Evidence only, no automatic normalization |
| Female lung modifier 1.21 | Figure 2, p.5434 | Used only in worked example |
| Excess rate | Eq.1, p.5433 | Excess HR × baseline annual hazard |
| Incidence loss | Eqs.2/3, p.5433 | Survival × excess rate × difference in remaining life |
| HF mortality loss | Eq.4, p.5434 | Explicit excess-mortality interpretation, see audit |
| Thyroid dose averaging | Eq.5, p.5436 | Volume normalization issue, see audit |
| Exposure+1 through 110 | Eq.3 and text, p.5433 | Explicit integration option |

The coefficients above are transcriptions from Brodin, not newly fitted estimates. Source association is not independent validation of the coefficient derivation.

## Focused primary-source cross-check

Bhatti et al., *Risk of Second Primary Thyroid Cancer after Radiotherapy for a Childhood Cancer in a Large Cohort Study: An Update from the Childhood Cancer Survivor Study*, Radiation Research 2010;174:741–752, DOI [10.1667/RR2240.1](https://doi.org/10.1667/RR2240.1), [full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC3080023/).

Its methods describe Poisson regression of cohort data and separate ERR and EAR models. Its results and Table 7 distinguish age modification of the ERR from sex/time-since-exposure modification of the EAR. Brodin's particular linear age formula must be traced further; it is not independently reconstructed here from Bhatti's published parameterization.

## Required data before reproduction

Versioned US incidence and cause-specific mortality rates, complete conditional general-population and post-event survival curves, a specified CCSS survivor curve and tail model, actual organ DVHs, and uncertainty distributions/covariances. Provenance must include calendar period, population, age group definitions, sex, endpoint and rate denominator. A discharge count must not be silently treated as first-event incidence.

The application currently consumes none of these datasets; all evidence enters the research domain explicitly.
