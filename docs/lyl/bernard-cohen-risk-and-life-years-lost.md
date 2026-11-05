# Bernard L. Cohen: risk comparison and implications for life years lost

Date: 2026-11-05  
Status: literature evaluation and proposed review criteria; not an audit of the current LYL implementation.

## Purpose and scope

This document records the evaluation of Bernard L. Cohen's approach to expressing risks through expected lifetime lost, and its possible contribution to risk-based radiotherapy planning.

The relevant connection is the conversion of mortality evidence into expected time lost. Historical numerical estimates should not be adopted as clinical model parameters without contemporary evidence and validation.

The original 1979 and 1991 article records and abstracts were verified. Their complete methods were not retrieved during this evaluation. Cohen's freely available 2003 explanation and the full 2013 Andersen–Canudas-Romo–Keiding paper were consulted. Equations presented below are a modern mathematical explanation, not a transcription or verified reconstruction of Cohen's original algorithms.

## 1. The author and the remembered book

The book matching the description is *Çok Geç Olmadan — Bir Bilimadamı Gözüyle Nükleer Enerji*, by Bernard L. Cohen, translated by Miyase Göktepeli and published by TÜBİTAK in the 1990s, ISBN 975-403-013-8.

Its original is *Before It's Too Late: A Scientist's Case for Nuclear Energy* (1983). Cohen also wrote *The Nuclear Energy Option: An Alternative for the 90s* (1990).

Publisher records:

- [Before It's Too Late](https://link.springer.com/book/10.1007/978-1-4684-4577-0)
- [The Nuclear Energy Option](https://link.springer.com/book/10.1007/978-1-4899-6002-3)
- [Turkish edition listing](https://www.simurgkitabevi.com/cok-gec-olmadan-bir-bilimadami-gozuyle-nukleer-enerji)

The recollection of “eight days per cigarette” has not been verified in the Turkish text and should not be treated as an established quotation or estimate.

## 2. Core publications

| Publication | Contribution | Relevance and limitation |
|---|---|---|
| Cohen BL, Lee IS. **A Catalog of Risks.** *Health Physics*. 1979;36(6):707–722. [DOI](https://doi.org/10.1097/00004032-197906000-00007), [PubMed](https://pubmed.ncbi.nlm.nih.gov/468542/) | Collects risk evidence and converts it into life expectancy losses, including age-range comparisons. | Foundational reference for the approach. Full methods still require retrieval. |
| Cohen BL. **Catalog of Risks Extended and Updated.** *Health Physics*. 1991;61(3):317–335. [DOI](https://doi.org/10.1097/00004032-199109000-00002), [PubMed](https://pubmed.ncbi.nlm.nih.gov/1880022/) | Extends the catalogue across mortality risks and social, occupational and environmental factors. | Broad comparative scope; historical populations and evidence are not interchangeable with present clinical inputs. |
| Cohen BL. **Risks in Perspective.** *Journal of American Physicians and Surgeons*. 2003;8(2):50–53. [Full text](https://www.jpands.org/vol8no2/cohen.pdf) | Accessible summary using life-table-derived LLE and comparisons across hazards. | Useful introduction to communication; does not replace detailed appraisal of each underlying estimate. |

These references serve different purposes. The books explain the nuclear-energy argument, the catalogues supply the methodological research trail, and the short article introduces the presentation of results.

## 3. General method and mathematical connection

Cohen combines evidence from different sources and expresses consequences in a common unit: average loss of life expectancy (LLE). The common unit supports comparison, but does not make the underlying studies equally causal, precise or applicable.

### 3.1 Expected lifetime as area under survival

For a person alive at age \(a\), define:

- \(t\): time since that age, in years;
- \(S_r(t\mid a)\): probability of surviving another \(t\) years in the reference scenario;
- \(S_e(t\mid a)\): corresponding survival in the exposure or alternative treatment scenario;
- \(\tau\): the analysis horizon measured from age \(a\).

The difference in expected years lived is

\[
L(a;\tau)=\int_0^\tau [S_r(t\mid a)-S_e(t\mid a)]\,dt.
\]

A positive result means the exposure scenario has lower expected survival over that horizon. A negative result means it has higher expected survival.

A finite horizon gives a difference in restricted mean survival time. A lifetime calculation additionally requires defensible extrapolation beyond observed follow-up.

### 3.2 From mortality hazards to survival

For scenario \(j\), with cause-specific mortality hazards \(\lambda_{j,k}\),

\[
S_j(t\mid a)=
\exp\left[-\int_0^t\sum_k\lambda_{j,k}(u\mid a)\,du\right].
\]

This shows why a mortality probability alone does not determine lifetime loss: the timing of deaths and all competing hazards affect survival.

These are standard survival relationships, included to explain the connection to LYL. They are not attributed as a unique invention of Cohen.

### 3.3 A simplified expectation example

Suppose, purely for illustration, a specified scenario gives a 1% probability of a fatal outcome, with a mean loss of 20 years among those experiencing it:

\[
0.01\times20=0.2\text{ years}\approx73\text{ days}.
\]

The group-average loss is 73 days per person under these assumptions. Each affected person does not lose 73 days. Real calculations must establish the counterfactual lifetime, event timing and competing mortality rather than assume a fixed 20-year loss.

## 4. Quantities that must remain distinct

| Quantity | Question answered | Principal qualification |
|---|---|---|
| Difference in expected survival between scenarios | How much does expected lifetime differ between these scenarios? | A causal interpretation requires justified scenario models and causal assumptions. |
| Cause-deleted life expectancy gain | How much longer would people live if a cause were removed? | Requires assumptions about what happens to other hazards after removal. |
| Cause-specific years lost before a fixed horizon | How is time lost before the horizon allocated among causes of death? | Descriptive attribution is not automatically an intervention effect. |
| Loss conditional on a fatal event | How much lifetime is lost among those experiencing the event? | Its denominator differs from the entire treated population. |
| Population total years lost | How much expected lifetime is lost across a specified population? | Depends on population size and composition. |

The name “LYL” alone is insufficient to identify the estimand. Every reported value needs its reference, population, conditioning and horizon.

## 5. Related modern competing-risk method

**Andersen PK, Canudas-Romo V, Keiding N. Cause-specific measures of life years lost.** *Demographic Research*. 2013;29(41):1127–1152.

- [Article and replication material](https://www.demographic-research.org/articles/volume/29/41)
- [Full text](https://www.demographic-research.org/volumes/vol29/41/29-41.pdf)
- [DOI: 10.4054/DemRes.2013.29.41](https://doi.org/10.4054/DemRes.2013.29.41)

This is a related methodological reference; direct intellectual descent from Cohen has not been established here.

Using time since a common starting point, let \(F_k(t)\) be the cumulative incidence of death from cause \(k\), among mutually exclusive and exhaustive causes. Then

\[
S(t)+\sum_k F_k(t)=1.
\]

Define

\[
L_k(\tau)=\int_0^\tau F_k(t)\,dt.
\]

It follows that

\[
\sum_k L_k(\tau)=\tau-\int_0^\tau S(t)\,dt.
\]

This provides additive descriptive accounting without assuming independent latent competing death times. The interpretation differs from comparing survival after a hypothetical cause removal.

**Potential contribution to our project:** distinguish cause attribution from treatment effects, and use the balance identity as a verification criterion wherever this particular estimand is implemented. It is not evidence that our current Brodin-based calculation should be replaced.

## 6. Related exposure-rate formulation

**Johnson WH, Kearfott KJ. Lost life expectancy rate: an application to environmental levels of radiation.** *Health Physics*. 1997;73(2):312–319.

- [PubMed](https://pubmed.ncbi.nlm.nih.gov/9228166/)
- [DOI: 10.1097/00004032-199708000-00003](https://doi.org/10.1097/00004032-199708000-00003)

The paper describes lost life expectancy rate (LLER): expected lifetime lost per unit of exposure time. It investigates dependence on exposure and demographic factors using radiation cancer risk models.

This may help explain comparisons between exposure patterns. Application to a finite radiotherapy course requires a specified schedule, latency model and survival calculation. The abstract was verified; the complete derivation was not reviewed.

## 7. Proposed implications for the radiotherapy domain

The following are review criteria and design suggestions, not findings of implementation defects.

| Topic | Proposed requirement | Reason |
|---|---|---|
| Reference scenario | Identify exactly what the alternative is compared with. | A loss has meaning only relative to a specified reference. |
| Population | Record starting age, relevant clinical factors and source population. | Average losses depend on baseline prognosis and case mix. |
| Time | Keep attained age, time since treatment and horizon explicit. | Latency and competing mortality affect expected lifetime loss. |
| Endpoint | Distinguish complication incidence from mortality. | A nonfatal complication probability is not directly a death hazard. |
| Conditioning | State “per treated patient” or the relevant conditional group. | Prevents denominator errors. |
| Multiple effects | Calculate joint survival where appropriate. | Separately estimated survival losses generally cannot be added. |
| Cause attribution | Name the attribution method. | Additivity depends on the estimand and decomposition. |
| Presentation | Permit days as a display unit while retaining internal precision. | Small year fractions are difficult to interpret. |
| Evidence | Preserve source, version, applicability and uncertainty. | A common unit does not remove differences in evidence quality. |

The generic conceptual workflow is: define reference and exposure scenarios; derive compatible mortality hazards; calculate survival; integrate the survival difference; report the estimate with its assumptions. This is not a diagram of the current code.

## 8. Limits and critical appraisal

### Historical estimates

Cohen's estimates reflect their source populations, periods and modeling assumptions. Transferring a value into a contemporary Danish patient calculation requires new justification. Per-cigarette equivalents are communication devices, not established biological increments that can automatically be accumulated.

### Association and causation

Differences associated with income, occupation or marital status cannot automatically be interpreted as the lifetime benefit of changing one factor. Confounding, selection and differences in baseline health must be addressed.

### Ecological inference in Cohen's radon work

Cohen's separate county-level radon analyses received specific methodological criticism. Lubin explains how ecological regression can fail to recover an individual exposure–response relationship, including reversal of its direction:

- [Lubin, 1998: On the discrepancy between epidemiologic studies in individuals of lung cancer and residential radon and Cohen's ecologic regression](https://pubmed.ncbi.nlm.nih.gov/9645660/)

This criticism concerns those analyses. It does not invalidate the mathematical concept of expected lifetime loss. It demonstrates why a statistical presentation and the causal validity of its inputs require separate assessment.

### Expected value and distribution

Equal mean lifetime losses can arise from different distributions of harm. A common LYL value alone does not describe severity among affected patients, uncertainty, nonfatal morbidity or the concentration of risk in particular groups.

## 9. Assessment and next review

Cohen is relevant as a historical and communication reference for expressing risk in expected time lost. His work motivates explicit comparisons and interpretable units.

Modern survival and competing-risk methods provide the precise language needed to distinguish survival contrasts, cause attribution and conditional losses.

A subsequent review of the current Brodin-based implementation should:

1. State its exact estimand and reference scenario.
2. Map published equations to implemented functions.
3. Check how background mortality, latency and cause-specific mortality enter.
4. Establish whether and why components can be aggregated.
5. Confirm that labels and units communicate the computed quantity.
6. Document numerical checks appropriate to that quantity.

No source-code audit, parameter change or model replacement is claimed by this document.
