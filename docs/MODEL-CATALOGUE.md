# Brodin Table 1 model catalogue

## Source and scope

**Brodin et al.** *Interactive Decision-Support Tool for Risk-Based Radiation Therapy Plan Comparison for Hodgkin Lymphoma*. International Journal of Radiation Oncology • Biology • Physics. 2014;88:433–445 (published online in 2013). [DOI](https://doi.org/10.1016/j.ijrobp.2013.10.028), **Table 1, pages 435–436**.

The inventory contains 29 study/model/evidence rows across 17 canonical OAR categories.

- **3 implemented**: source-linked server calculations and curves (Hancock/Brodin cardiac mortality, Marks/QUANTEC radiation pneumonitis, Pan/QUANTEC mean-dose liver LKB).
- **19 catalogued; implementation pending**: mathematical relationship or coefficients are listed, but model-specific input handling and independent numerical validation have not been completed.
- **4 incomplete-parameter entries**: Table 1 alone does not specify enough to implement safely (Moiseenko parotid LKB; two carotid/stroke slopes and population baselines; Bhatti thyroid cancer).
- **3 evidence-only entries**: spleen (no NTCP data), pancreatic and colon cancer (cohort RR figures but no patient-dose response).

**A catalogued model must never be silently calculated.** These flags represent implementation state, not evidence quality or clinical appropriateness.

## Inventory

| OAR | Endpoint | Source | Mathematical measure | Status |
|---|---|---|---|---|
| Parotid | Xerostomia | Moiseenko 2012 | LKB NTCP | Incomplete |
| Parotid | Xerostomia | Houweling 2010 | Logistic NTCP | Pending |
| Parotid | Xerostomia | Beetz 2011 | Multivariable logistic NTCP | Pending |
| Submandibular | Sticky saliva | Beetz 2011 | Multivariable logistic NTCP | Pending |
| Carotid arteries | Stroke | Bowers 2005 / Maraldo 2013 | Background-adjusted incidence | Incomplete |
| Carotid arteries | Stroke | de Bruin 2009 / Maraldo 2013 | Background-adjusted incidence | Incomplete |
| Thyroid | Thyroid cancer | Bhatti 2010 | Unresolved from table alone | Incomplete |
| Thyroid | Hypothyroidism | Vogelius 2011 | Logistic NTCP | Pending |
| Thyroid | Hypothyroidism | Boomsma 2012 | Multivariable logistic NTCP | Pending |
| Neck muscles | Weakness | van Leeuwen-Segarceanu 2012 | Logistic NTCP | Pending |
| Larynx | Edema | Rancati 2009 | DVH-LKB NTCP | Pending |
| Larynx | Dysphagia | Rancati / QUANTEC 2010 | Logit NTCP | Pending |
| Esophagus | Stricture | Alevronta 2010 | Relative seriality NTCP | Pending |
| Esophagus | Esophagitis | Belderbos 2005 | EUD-LKB NTCP | Pending |
| Esophagus | Esophageal cancer | Morton 2012 | Odds ratio | Pending |
| Lungs | Radiation pneumonitis | Marks / QUANTEC 2010 | Logistic NTCP | **Implemented** |
| Lungs | Radiation pneumonitis | Huang 2011 | D10 heart + mean lung logistic NTCP | Pending |
| Lungs | Lung cancer | Travis 2002 / Brodin 2011 | Relative risk | Pending |
| Breast | Breast cancer | Travis 2003 / Brodin 2011 | Relative risk | Pending |
| Heart | Valvular disease | Schellong / Maraldo | Logistic NTCP | Pending |
| Heart | Cardiac morbidity | Schellong / Maraldo | Logistic NTCP | Pending |
| Heart | Cardiac mortality | Hancock / Brodin | Linear NTCP | **Implemented** |
| Stomach | Stomach cancer | van den Belt-Dusebout 2009 | Relative risk | Pending |
| Liver | Radiation-induced liver disease | Pan / QUANTEC 2010 | Mean-dose LKB NTCP | **Implemented** |
| Spleen | Spleen toxicity | No model available | No numeric dose response | Evidence only |
| Pancreas | Pancreatic cancer | Hodgson 2007 | Cohort RR (non-dose-specific) | Evidence only |
| Small bowel | Gastrointestinal toxicity | Eich 2005 | Full-DVH LKB NTCP | Pending |
| Colon | Colon cancer | Hodgson 2007 | Cohort RR (non-dose-specific) | Evidence only |
| Kidney | Symptomatic injury | Cassady 1995 / Kornerup 2012 | Logistic NTCP | Pending |

## Typed OAR-based selection

The catalogue uses canonical OAR IDs such as `heart`, `lungs`, `thyroid` and `esophagus`, independently of Eclipse structure names. Every model specifies:

- `OarId`: canonical organ
- `Inputs`: typed `MeanDoseGy`, `DoseAtVolumePercent`, `DifferentialDvh`, `VolumeCm3`, `ClinicalVariable`, or `BaselineIncidence`
- `OutputKind`: `AbsoluteProbability`, `RelativeRisk`, `OddsRatio`, `BaselineAdjustedIncidence`, `NoRiskEstimate`
- `Availability`: `Implemented`, `RequiresImplementation`, `MissingParameters`, `NoDoseResponse`
- source, dose context, follow-up, clinical limitations, and coefficients

**Current prototype:** a manual OAR-availability toggle lets the user mark heart, lungs and liver as present; the server computes **only** implemented models belonging to those available OARs. Changing a toggle re-evaluates the plan and updates the displayed curve set. The catalogue is independently filterable by OAR.

**Future ESAPI adapter:** translate Eclipse structures into canonical OAR IDs; extract model-required DVH quantities with dose/volume units; include provenance (course, plan, structure set, contour naming, dose presentation, fractionation, extraction time), and combine available metric keys with required metric keys. Plot eligibility must depend on the model being implemented **and** all required dosimetric/clinical inputs being present and validated. Structure presence alone will not suffice for models needing, for example, heart D10%, thyroid volume, or age/baseline score.

## Numerical and interpretation safeguards

- Absolute probability is a different kind of quantity from RR, OR or cohort-level incidence.
- A dose-response reported for a historical cohort does not imply universal clinical applicability.
- The table is not a source for missing baseline incidence rates, coefficient correlations, or model uncertainty distributions.
- The complete model expression and metric definition must be checked against the original paper or supplemental material before promoting each pending model into executable code.
- The current 0–50 Gy plots are display ranges only and do not establish the validated applicability ranges of individual models.
- Three executable models retain their existing server-owned equations and mathematical tests; 26 additional inventory rows are never plotted by the calculation API.
