# Brodin Table 1 model catalogue

## Source and scope

**Brodin et al.** *Interactive Decision-Support Tool for Risk-Based Radiation Therapy Plan Comparison for Hodgkin Lymphoma*. International Journal of Radiation Oncology • Biology • Physics. 2014;88:433–445 (published online in 2013). [DOI](https://doi.org/10.1016/j.ijrobp.2013.10.028), **Table 1, pages 435–436**.

The inventory contains 29 study/model/evidence rows across 17 canonical OAR categories.

- **21 implemented as research equations**: the original three, plus 18 new model functions accepting explicitly typed scalar, DVH, and clinical inputs.
- **1 catalogued; implementation pending**: Alevronta esophageal stricture / relative-seriality model. The full model convention and γ parameterization have not been uniquely reconciled with its primary source and Brodin supplement.
- **4 incomplete-parameter entries**: Table 1 alone does not specify enough to implement safely (Moiseenko parotid LKB; two carotid/stroke slopes and population baselines; Bhatti thyroid cancer).
- **3 evidence-only entries**: spleen (no NTCP data), pancreatic and colon cancer (cohort RR figures but no patient-dose response).

**A catalogued model must never be silently calculated.** These flags represent implementation state, not evidence quality or clinical appropriateness.

## Inventory

| OAR | Endpoint | Source | Mathematical measure | Status |
|---|---|---|---|---|
| Parotid | Xerostomia | Moiseenko 2012 | LKB NTCP | Incomplete |
| Parotid | Xerostomia | Houweling 2010 | Logistic NTCP | Implemented |
| Parotid | Xerostomia | Beetz 2011 | Multivariable logistic NTCP | Implemented |
| Submandibular | Sticky saliva | Beetz 2011 | Multivariable logistic NTCP | Implemented |
| Carotid arteries | Stroke | Bowers 2005 / Maraldo 2013 | Background-adjusted incidence | Incomplete |
| Carotid arteries | Stroke | de Bruin 2009 / Maraldo 2013 | Background-adjusted incidence | Incomplete |
| Thyroid | Thyroid cancer | Bhatti 2010 | Unresolved from table alone | Incomplete |
| Thyroid | Hypothyroidism | Vogelius 2011 | Logistic NTCP | Implemented |
| Thyroid | Hypothyroidism | Boomsma 2012 | Multivariable logistic NTCP | Implemented |
| Neck muscles | Weakness | van Leeuwen-Segarceanu 2012 | Logistic NTCP | Implemented |
| Larynx | Edema | Rancati 2009 | DVH-LKB NTCP | Implemented |
| Larynx | Dysphagia | Rancati / QUANTEC 2010 | Logit NTCP | Implemented |
| Esophagus | Stricture | Alevronta 2010 | Relative seriality NTCP | Pending |
| Esophagus | Esophagitis | Belderbos 2005 | EUD-LKB NTCP | Implemented |
| Esophagus | Esophageal cancer | Morton 2012 | Odds ratio | Implemented |
| Lungs | Radiation pneumonitis | Marks / QUANTEC 2010 | Logistic NTCP | **Implemented** |
| Lungs | Radiation pneumonitis | Huang 2011 | D10 heart + mean lung logistic NTCP | Implemented |
| Lungs | Lung cancer | Travis 2002 / Brodin 2011 | Relative risk | Implemented |
| Breast | Breast cancer | Travis 2003 / Brodin 2011 | Relative risk | Implemented |
| Heart | Valvular disease | Schellong / Maraldo | Logistic NTCP | Implemented |
| Heart | Cardiac morbidity | Schellong / Maraldo | Logistic NTCP | Implemented |
| Heart | Cardiac mortality | Hancock / Brodin | Linear NTCP | **Implemented** |
| Stomach | Stomach cancer | van den Belt-Dusebout 2009 | Relative risk | Implemented |
| Liver | Radiation-induced liver disease | Pan / QUANTEC 2010 | Mean-dose LKB NTCP | **Implemented** |
| Spleen | Spleen toxicity | No model available | No numeric dose response | Evidence only |
| Pancreas | Pancreatic cancer | Hodgson 2007 | Cohort RR (non-dose-specific) | Evidence only |
| Small bowel | Gastrointestinal toxicity | Eich 2005 | Full-DVH LKB NTCP | Implemented |
| Colon | Colon cancer | Hodgson 2007 | Cohort RR (non-dose-specific) | Evidence only |
| Kidney | Symptomatic injury | Cassady 1995 / Kornerup 2012 | Logistic NTCP | Implemented |

## Typed OAR-based selection

The catalogue uses canonical OAR IDs such as `heart`, `lungs`, `thyroid` and `esophagus`, independently of Eclipse structure names. Every model specifies:

- `OarId`: canonical organ
- `Inputs`: typed `MeanDoseGy`, `DoseAtVolumePercent`, `DifferentialDvh`, `VolumeCm3`, `ClinicalVariable`, or `BaselineIncidence`
- `OutputKind`: `AbsoluteProbability`, `RelativeRisk`, `OddsRatio`, `BaselineAdjustedIncidence`, `NoRiskEstimate`
- `Availability`: `Implemented`, `RequiresImplementation`, `MissingParameters`, `NoDoseResponse`
- source, dose context, follow-up, clinical limitations, and coefficients

**Current main calculator:** a manual OAR-availability toggle lets the user mark heart, lungs and liver as present; the server computes the **original three** directly in the plan-level calculator. **Catalogue runner:** any of the 21 implemented models can be evaluated on demand by supplying its explicitly required inputs. The catalogue calculation endpoint does not infer missing values. Changing a toggle re-evaluates the plan and updates the displayed curve set. The catalogue is independently filterable by OAR.

**Future ESAPI adapter:** translate Eclipse structures into canonical OAR IDs; extract model-required DVH quantities with dose/volume units; include provenance (course, plan, structure set, contour naming, dose presentation, fractionation, extraction time), and combine available metric keys with required metric keys. Plot eligibility must depend on the model being implemented **and** all required dosimetric/clinical inputs being present and validated. Structure presence alone will not suffice for models needing, for example, heart D10%, thyroid volume, or age/baseline score.

## Numerical and interpretation safeguards

- Absolute probability is a different kind of quantity from RR, OR or cohort-level incidence.
- A dose-response reported for a historical cohort does not imply universal clinical applicability.
- The table is not a source for missing baseline incidence rates, coefficient correlations, or model uncertainty distributions.
- The complete model expression and metric definition must be checked against the original paper or supplemental material before promoting each pending model into executable code.
- The current 0–50 Gy plots are display ranges only and do not establish the validated applicability ranges of individual models.
- The three original models retain their server-owned equations and mathematical tests. The 18 additional models have a separate typed evaluation endpoint. Only absolute-probability models have 0–100% dose-response curves; RR/OR outputs have no absolute-probability curve. Eight non-executable entries remain listed with explicit status.


## Expanded implementation and numerical safeguards (October 2026)

The `src/Server/CatalogueCalculations.fs` module implements the new 18 relationships as small pure F# functions using `Result<_,string>` and computation expressions. Each function receives a typed input, not a treatment-site assumption.

**Families implemented**

- Six γ50 logistic models: Houweling spared parotid; Vogelius thyroid (HL-rederived Table 1 parameters 33.6/0.58, not the unrestricted 45/1.4 meta-analysis); neck muscle weakness; Schellong valvular disease and cardiac morbidity; Cassady kidney injury. Formula: `logistic(4γ50(D/D50−1))`.
- Four multivariable logistic regressions: Beetz patient-rated xerostomia, Beetz sticky saliva, Boomsma hypothyroidism and Huang radiation pneumonitis. Missing age, explicitly binary baseline symptom score, thyroid volume, sublingual dose or heart D10% is an error, never zero.
- One dose-ratio logit model for Rancati supraglottic-larynx aspiration/dysphagia endpoint: `1/(1+(D50/D)^k)`, with the zero-dose limit explicitly zero.
- Three full differential-DVH LKB functions for Rancati laryngeal edema, Belderbos esophagitis and Eich small bowel toxicity: `gEUD = (Σ vᵢ·Dᵢ^(1/n))^n`, `NTCP=Φ((gEUD−TD50)/(m·TD50))`.
- Four dose-dependent relative associations: Morton esophageal OR, Travis lung RR, Travis breast RR, van den Belt-Dusebout stomach RR. These are evaluated on **RR/OR scales**, never converted to an absolute patient probability without background risk data.

**Important data semantics**

- Houweling requires **mean dose of the least-irradiated parotid**, not a pooled bilateral parotid mean. This has its own typed `SparedGlandMeanDoseGy` input.
- Rancati dysphagia dose-ratio logit uses **mean dose of the supraglottic larynx** as a separate input structure, not a whole-larynx substitute.
- Differential DVH bins are `doseGy:volumeFraction` with *fractions summing to 1*. Cumulative DVHs cannot be passed as differential histograms. The API rejects an empty, duplicated, non-finite, negative or non-normalized histogram.
- Clinical factor codes for baseline xerostomia and sticky saliva must be 0 (none) or 1 (a bit); the correct underlying endpoint and coding must be confirmed on original study populations.
- Curves in the dedicated catalogue runner sweep the primary dose predictor (or gEUD) while other clinical predictors remain fixed. These display dose ranges are mathematical demonstrations, **not automatically validated ranges for all radiotherapy protocols**.
- Any request to evaluate Alevronta relative seriality, carotid/stroke, Bhatti thyroid cancer, Moiseenko parotid or cohort-only evidence receives a clear computation error.

**Numerical checks:** individual γ50 curves equal 0.5 at D50, uniform DVH curves equal 0.5 at TD50, Beetz coefficients agree with direct independent regression arithmetic, Boomsma responds to thyroid volume, Huang responds to heart D10, logit zero-dose limiting behavior, RR/OR retains its original scale, incomplete DVHs and missing covariates fail closed. These are **equation implementation checks**, not calibration, independent clinical outcome validation, or proof of applicability.

**Supporting research:** [Brodin Table 1](https://doi.org/10.1016/j.ijrobp.2013.10.028), [Beetz 2011 original model and baseline categories](https://www.thegreenjournal.com/article/S0167-8140%2811%2900202-7/fulltext), [Houweling spared-parotid analysis](https://pmc.ncbi.nlm.nih.gov/articles/PMC3192313/), [NTCP family formula review](https://pmc.ncbi.nlm.nih.gov/articles/PMC5915347/), [Boomsma thyroid volume model](https://pmc.ncbi.nlm.nih.gov/articles/PMC7563778/), [Alevronta relative-seriality source](https://pubmed.ncbi.nlm.nih.gov/20557965/).
