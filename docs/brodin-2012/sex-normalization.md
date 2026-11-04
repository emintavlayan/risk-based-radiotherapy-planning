# Is 1:1.7 versus 1.21 an error?

**Current conclusion: not demonstrated. The normalization is unresolved. No numerical correction is justified yet.**

## Verified observations

The supplied final PDF has:

- Table 1, lung cancer: male:female risk ratio 1:1.7.
- Figure 2: multiply the unmodified excess hazard ratio by 1.21 for a female.
- Figure 2: then multiply by a female-specific population lung-cancer hazard.

The paper does not provide an explicit equation deriving 1.21 from 1.7. A between-sex ratio is not the same quantity as a multiplier relative to a pooled fitted reference.

## A consistency construction, not an explanation of the authors' calculation

Suppose, for illustration only, that the ratio refers to excess-relative-hazard dose-response coefficients:

```text
k_f = r × k_m, with r = 1.7
k_ref = (1-p) × k_m + p × k_f
female multiplier = k_f / k_ref = r / (1-p+p×r)
```

Here p is an effective reference weight, not necessarily the fraction of female patients. Fitted regression coefficients need not be simple averages.

| Hypothetical reference | Female multiplier |
| --- | --- |
| Male reference, p=0 | 1.7 |
| Equal weights, p=0.5 | 1.259259… |
| Effective female weight p≈0.5785124 | 1.21 |

Thus 1.21 and a ratio of 1.7 can coexist mathematically. This does not show that the authors used those weights, that such averaging matches the source estimator, or even that the reported ratio is a ratio of excess-relative-hazard coefficients.

Under this illustrative construction, the male multiplier would be 1.21/1.7≈0.711765. Finding such a male factor in the original implementation would help identify the normalization, but would not by itself validate its epidemiological meaning.

## What would establish an error?

- If the coefficient 0.194/Gy is explicitly male-referenced and the 1.7 ratio refers to the same coefficient/estimand, then the corresponding female multiplier should be 1.7, and 1.21 would require correction.
- If the coefficient is pooled, recover the reference normalization and derive the sex-specific multipliers from that model. Equal weights must not be assumed merely because there are two sexes.
- If 1.7 is a ratio of absolute incidence, an SIR ratio, a total HR ratio or an EAR ratio rather than an excess-relative-hazard coefficient ratio, it cannot be substituted directly without deriving the appropriate transformation.
- If the cited study reports no relevant radiation-by-sex interaction, a separate relative-hazard modifier may be unsupported. Sex-specific baseline incidence must still be retained.

## Evidence still needed

Trace the 0.194/Gy coefficient to Travis 2005 and its underlying analysis, and the sex ratio to Swerdlow 2000 / Bassal 2006. Establish:

1. What is the estimand: HR, HR-1, SIR, EAR, absolute incidence, or a dose coefficient?
2. What is its reference: male, female, pooled population or an internally normalized cohort?
3. How are sex-specific population rates used in that derivation?
4. What equation and reference weights produce 1.21?

The indexed abstracts and the Brodin article do not resolve these questions. Direct retrieval of the two cited ASCO full texts was blocked (403/inaccessible) on 4 November 2026. The review does not claim to have inspected those full texts.

## Implementation consequence

There is no selectable sex-normalization policy, arbitrary sex-multiplier API or automatic patient-specific dose-to-loss pipeline.

The code retains:

- typed, unit-checked hazard algebra;
- unmodified Table 1 response functions, explicitly named as such;
- the life-loss kernel consuming an already specified excess hazard ratio;
- a diagnostic transcription of Figure 2, clearly separate from a general scientific rule.

The Figure 2 test verifies arithmetic only. It does not validate 1.21. Patient-specific use of Table 1 remains blocked at the scientific-review stage until this normalization is established.
