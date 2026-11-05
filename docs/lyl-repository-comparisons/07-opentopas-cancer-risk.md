# OpenTOPAS-CancerRisk: DVH-based secondary cancer risk

[Comparison index](README.md)

## 1. Sources and review scope

- Repository: [OpenTOPAS/OpenTOPAS-CancerRisk](https://github.com/OpenTOPAS/OpenTOPAS-CancerRisk).
- Reviewed revision: [`be8ec77d4f90`](https://github.com/OpenTOPAS/OpenTOPAS-CancerRisk/tree/be8ec77d4f901f4051e406d6e1ce07f6b9a7ea58).
- Review date: 2026-11-05.
- Method: static inspection of the source files named below and associated documentation. External software was not executed; numerical failures are not reproduced unless explicitly stated. Publication association does not validate the public code.
- Repository authors: Isaac Meyer, Alejandro Bertolet and Harald Paganetti. The inspected README does not identify a single extension-validation article; do not infer one from authorship.
- Schneider, Sumila and Robotka (2011), *Site-specific dose-response relationships for cancer induction from the combined Japanese A-bomb and Hodgkin cohorts for doses relevant to radiotherapy*. DOI: [10.1186/1742-4682-8-27](https://doi.org/10.1186/1742-4682-8-27); [full text](https://pmc.ncbi.nlm.nih.gov/articles/PMC3161945/).
- BEIR VII Phase 2 report: [10.17226/11340](https://doi.org/10.17226/11340).
- Additional parameter sources explicitly cited in the tables: [10.1007/s00411-007-0151-y](https://doi.org/10.1007/s00411-007-0151-y), and radiosensitivity review [10.1186/s13014-018-1040-z](https://doi.org/10.1186/s13014-018-1040-z). These identify source provenance, not an independently verified derivation of every table entry.
- Inspected: complete `src/TsRiskModel.cc`, README, Python wrapper, Schneider/BEIR parameter files, and data inventory/sample. No build or data-provenance validation was performed.

The local comparator is the LifeYearsLost domain at commit `040f5eafcb35cab48dc5a5bc61f4cd9dcdfb74ae`. See the [shared equations and interpretation boundaries](README.md#our-comparison-baseline). Links to external source files below use the reviewed revision.

## 2. What it is and why it is related

The closest computational match in this set to our dose-response work. A TOPAS outcome extension takes an organ DVH and predicts excess relative risk (ERR), excess absolute risk (EAR) and lifetime attributable risk (LAR). It includes carcinoma/sarcoma models, age and sex effects, fractionation and repopulation, population survival, and organ-specific SEER files.

It connects two areas of our work: independently inspectable risk calculations and TOPAS-generated dose distributions. It does not calculate life years lost or supply post-event prognosis.

## 3. Formulation and code workflow

The central workflow in [TsRiskModel.cc](https://github.com/OpenTOPAS/OpenTOPAS-CancerRisk/blob/be8ec77d4f901f4051e406d6e1ce07f6b9a7ea58/src/TsRiskModel.cc) is:

1. Resolve organ, sex, exposure age, attained age, fraction count and parameter family.
2. Read population survival, organ incidence and response parameters.
3. Reduce differential DVH bins to organ equivalent dose (OED).
4. Evaluate age-dependent ERR/EAR.
5. Sum survival-weighted excess rates over age and combine transfer estimates.

### Differential DVH averaging

$$OED=\frac{\sum_i V_i RED(D_i,n,\alpha,\alpha/\beta,R)}{\sum_i V_i},\qquad
\alpha'_i=\alpha+\beta D_i/n.$$

This OED is a model-derived risk-equivalent dose, not ICRP organ equivalent dose in sieverts, mean organ dose, or EQD2. Those quantities must remain distinct.

For the zero-repopulation branch the implementation uses

$$RED(D)=D\exp(-\alpha'D).$$

For nonzero repopulation, the current carcinoma and sarcoma functions are algebraically identical. Writing $A=\alpha'D$, both evaluate

$$RED(D)=\frac{e^{-A}}{\alpha'R}
\left[1-2R+R^2e^A-(1-R)^2e^{-AR/(1-R)}-\alpha'RD\right].$$

That identity is an observation about the source, **not** confirmation that carcinoma and sarcoma should have the same published response. Check the separate original equations before adopting either branch. Zero dose, $R\to0$, $R\to1$, large exponentials and small $\alpha'$ deserve explicit limiting-value tests.

### Age effects and integration

The implementation uses the pattern

$$ERR(a,e)=OED\,\beta_{ERR}\exp\{\gamma_e e^*+\gamma_a\log(a/a_{ref})\},$$

and an analogous EAR expression. For most organs $e^*=\min(e-30,0)$; breast and thyroid use $e$ directly. Units and normalization of every coefficient must be checked against its source rather than inferred from variable names.

Conceptually the two LAR constructions are

$$LAR_{ERR}\approx\sum_a ERR(a,e)\lambda_{pop,j}(a)\frac{S_{pop}(a)}{S_{pop}(e)}\Delta a,$$

$$LAR_{EAR}\approx\sum_a EAR(a,e)\frac{S_{pop}(a)}{S_{pop}(e)}\Delta a.$$

The source applies additional factors of 100 and 1,000 in different paths. These formulas describe intended dimensions, not a certification of its final scaling. BEIR-family transfer uses a weighted **geometric** combination of cumulative ERR/EAR estimates divided by DDREF; Schneider defaults to EAR weighting. This differs from SUMRAY's arithmetic mixture of age-specific contributions.

## 4. Similarities and differences from our domain

| Dimension | OpenTOPAS extension | Our domain |
| --- | --- | --- |
| DVH | Nonlinear risk response averaged over bins | Differential DVH averaging for thyroid |
| Dose descriptor | OED with fractionation/repopulation | Mean dose for five endpoints; thyroid nonlinear response |
| Response age | Explicit exposure/attained-age modifiers | Brodin coefficients; caller can supply age-dependent excess ratio |
| Risk measure | ERR, EAR and cumulative attributable risk | Excess hazard and expected years lost |
| Survival | General-population survival conditional on exposure age | Externally declared survivor cohort survival |
| Consequences | No prognosis weighting | Population minus post-event life, or fatal-event population life |

## 5. What could be useful to us

### A separate dose-response family

Keep a Schneider adapter separate from `Brodin2012`. A generic OED function could share validated differential DVH inputs, but should return a distinct quantity. Only a validated conversion with endpoint, coefficient units and age definitions could then produce an `ExcessIncidenceHazard` or `ExcessHazardRatio`.

### A source/data map

The tables expose where coefficients, reference ages, survival and baseline incidence enter. These are useful leads for an auditable evidence catalogue. Downloaded SEER CSVs still need source release, denominator, age indexing, interpolation and sex/organ mappings established. Being included in a repository does not establish suitability for a contemporary Danish cohort.

### Independent test cases

Uniform dose should reduce DVH averaging to the point response; rescaling all bin volumes should not alter OED; zero dose should give zero excess; increasing fraction count should follow the chosen formula. Intermediate OED, ERR and EAR values are more suitable starting comparisons than the final reported probability.

The standalone Python API can process DVHs, but it is a ctypes wrapper around a compiled TOPAS extension, not an independent Python reference implementation. Its library path uses a macOS `.dylib`, so Linux/Windows reuse requires adaptation.

## 6. What is substantially different or cannot be transferred

LAR is not LYL. Multiplying cancer incidence risk by an arbitrary fixed life expectancy would omit event-age and prognosis dependence. General-population survival cannot silently replace cancer-survivor survival. The BEIR parameter option is not pure BEIR dosimetry: its table explicitly imports high-dose response parameters from other sources.

The [Schneider table](https://github.com/OpenTOPAS/OpenTOPAS-CancerRisk/blob/be8ec77d4f901f4051e406d6e1ce07f6b9a7ea58/data/Schneider.txt) explicitly states assumptions: thyroid repopulation is copied from salivary gland; some age modifiers use BEIR; sex factors are constructed from another publication. The [BEIR table](https://github.com/OpenTOPAS/OpenTOPAS-CancerRisk/blob/be8ec77d4f901f4051e406d6e1ce07f6b9a7ea58/data/BEIRVII_repopulation.txt) includes averaged or borrowed values for some organs. These should be preserved as separate assumptions, not labelled uniformly as measured organ-specific evidence.

## 7. Implementation observations and verification limits

- The age integration runs through `fAttainedAge + fLatency`, with latency fixed at five years, rather than stopping at the apparent requested attained age. `AverageLAReAndLARa` selects a returned entry using a different index convention. Resolve intended horizon semantics before interpreting outputs.
- Summary quantities for ages never reached can remain uninitialized. An empty or very short window is not validated robustly.
- Carcinoma and sarcoma implementations match algebraically in the inspected revision; confirm against the publication.
- README suggests `BEIR`, whereas the source recognizes `BEIRVII`. Invalid values print a fallback message without necessarily assigning a valid replacement string in every path.
- Output descriptions mix probability, percent and per-person-year terminology; cumulative risk should be dimensionless after rate integration. Trace input units and scale factors end to end.
- The parameter parser's row-count stopping condition uses the number of columns, not available organ rows. Organ lookup also contains aliases and duplicated SEER names that need an explicit mapping policy.
- No extension-wide automated verification suite was identified in the reviewed tree. These findings are static; no TOPAS build or numerical run was performed.

## 8. Recommendation and next verification

**Highest-priority additional formulation reference; do not use the present final output as a trusted oracle.** First transcribe the published carcinoma/sarcoma equations independently, verify parameter dimensions and provenance, and resolve horizon/units. Then compare small hand-checkable OED and age-specific rate cases before attempting cumulative risk. A future LYL adapter would additionally need compatible prognosis and survivor evidence. No automatic change to our current dose-response family is justified.
