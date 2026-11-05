# Clinical vocabulary

A hazard is an instantaneous event rate among people still in the relevant risk set. A hazard of 0.01/year is not exactly a 1% annual probability. With a constant isolated hazard, the one-year probability is 1-exp(-0.01). Competing events change the probability further.

| Domain type | Meaning | Unit | Important boundary |
| --- | --- | --- | --- |
| ExposureAge | Age at irradiation, e | year | Separate from attained age |
| AttainedAge | Age when an event could occur, a | year | Must not precede exposure |
| AbsorbedDose | Absorbed dose, D | Gy | No implicit RBE or neutron conversion |
| HazardRatio | h_exposed / h_baseline | 1 | HR=1 means no change; HR<1 is possible |
| ExcessHazardRatio | HR-1 | 1 | Zero means no excess; may exceed one |
| IncidenceHazard | New-event baseline hazard | 1/year | Not a death rate |
| MortalityHazard | Cause-specific baseline death hazard | 1/year | Not hospitalization incidence |
| ExcessIncidenceHazard | Radiation-attributable incidence hazard | 1/year | Excess HR × baseline incidence |
| ExcessMortalityHazard | Radiation-attributable death hazard | 1/year | Excess HR × baseline mortality |
| AnnualRate | Validated numerical annual rate | 1/year | Supporting quantity, not a probability |
| SurvivalProbability | Probability that an original cohort member remains alive | 1 | Starting cohort must be declared externally |
| RemainingLife | Remaining expected lifetime | year | Different from cohort expected loss |
| RemainingLifeAtAge | Remaining life explicitly conditional on a declared attained age | year | Both prognosis inputs must match evidence.Age |
| RestrictedMeanLife | Survival integral to a finite elapsed horizon | year | Retains horizon and terminal survival; distinct from full remaining life |
| LifeLossDensity | Expected years lost per year of attained age | year/year = 1 | Integrand, not a finite-bin loss |
| ExpectedLifeYearsLost | Integrated cohort expectation | year | Result of integrating the density |
| DifferentialDvh | Complete fractional differential dose distribution | 1 | Fractions sum to one |

Private constructors and Result-returning creation functions reject non-finite values and invalid ranges. Total HR allows protective effects; conversion to the non-negative excess-loss model rejects HR<1 rather than silently clipping it. Units prevent dose and time from being substituted for annual rates. Separate types prevent total HR from being supplied where HR-1 is required.

## Calculation

1. Select an endpoint and its required organ-dose descriptor.
2. Convert dose to excess HR using Table 1. Mean dose is used for the five linear responses. Thyroid requires a differential DVH: evaluate each bin's nonlinear response before averaging.
3. Supply any required excess-hazard modifier explicitly, with its source or declared assumption. A reported sex comparison is evidence, not an automatically inferred multiplier.
4. Multiply excess HR by the matching age-specific, sex-specific baseline incidence hazard. For heart failure, use baseline cause-specific mortality instead.
5. Multiply by survivor survival from the chosen starting cohort.
6. For incidence endpoints, multiply by population remaining life minus post-event remaining life. For heart failure deaths, multiply by population remaining life.
7. Integrate the resulting density over attained age.

This is the paper's attribution framework. It is not an exact replacement for a joint multistate survival model.

## Dimensional checks

| Operation | Dimensions |
| --- | --- |
| Linear response kD | Gy^-1 × Gy = 1 |
| Thyroid age correction | Gy^-1 - (Gy^-1 year^-1 × year) |
| Thyroid exponent -β4 D² | Gy^-2 × Gy² = 1 |
| Fractional DVH average | Σ fraction × dimensionless response = 1 |
| Absolute DVH average | Σ cm³ × response / cm³ = 1 |
| Excess hazard | Excess HR × year^-1 = year^-1 |
| Survival-weighted event rate | 1 × year^-1 = year^-1 |
| Life-loss density | 1 × year^-1 × year = 1 |
| Integrated loss | Density × year = year |

## Population data obligations

AgeSpecificEvidence identifies endpoint, sex and attained age. The caller must supply properly matched population data. The function verifies endpoint, sex and event pathway. Integrating samples requires one exposure age, sex and endpoint (excess HR may vary with attained age), strictly increasing ages, non-increasing survivor survival and exact coverage of the chosen bounds.

The caller must also establish calendar period, population, hazard definition, survivor starting cohort, and the conditioning of post-event survival. The code cannot determine these from a rate alone.

Survivor survival is not automatically conditioned on surviving to the current event age: doing so would remove the intended competing-mortality weighting. Population and post-event life expectancies must both be remaining lifetimes conditional on being alive at evidence.Age. ConsequenceEvidence requires RemainingLifeAtAge values, and atAge checks their declared ages against evidence.Age, including the mortality pathway. The wrapper records the caller's declaration; validating the source conditioning still requires checking the data.

LifeLoss.restrictedMeanFromSurvival integrates a valid conditional survival curve over the supplied finite horizon. Its RestrictedMeanLife result exposes horizon, terminalSurvival and hasUnintegratedTail. It is not interchangeable with RemainingLife. LifeLoss.remainingLifeFromSurvival continues to require a zero terminal survival probability for full remaining life. Neither function extrapolates a tail or silently treats a restricted mean as full expectancy. A finite-horizon LYL workflow would also need matched prognosis horizons; that is not inferred here.

No plan-specific competing-risk adjustment or first-event probability is inferred. No endpoint summation function labels overlapping events as an exact total expectancy decrement.


## Evidence and supplied mathematical inputs

ReportedSexComparison preserves the female-to-male direction, the reported association measure (or its unresolved meaning) and the source. It is not accepted by ExcessHazardMultiplier.apply.

ExcessHazardMultiplier means exactly a multiplier of HR-1. It is dimensionless and includes provenance. Its constructor validates the number and reference; it does not certify scientific validity. Multipliers are supplied explicitly rather than inferred from reported comparisons.

DoseResponse.linear accepts a supplied slope in Gy^-1. DoseResponse.thyroid accepts validated supplied slope and falloff in Gy^-1 and Gy^-2. Brodin2012 keeps publication-specific transcriptions separate from these generic equations.

A risk ratio or cumulative-event probability is not required by the implemented loss kernel. Add such types when a workflow actually computes or consumes them, with endpoint, comparison and time horizon. Never use an unqualified risk ratio in a hazard argument.


## Review decisions

- The Brodin thyroid age transcription returns an explicit error above 2.81/0.15 years (approximately 18.733333), where its slope becomes negative. It does not truncate the slope or claim a clinical validity limit. Independently established coefficients can be supplied to the generic nonlinear response.
- FractionalDoseBin and AbsoluteDoseBin are public records, but their Dose field requires AbsorbedDose, whose representation is private. Raw negative gray values cannot be assigned to those fields. AbsorbedDose.create rejects negative and non-finite inputs.
- HazardRatio may represent a protective association. HazardRatio.toExcess explicitly declines HR<1 because this kernel models non-negative excess loss. A signed benefit/loss model would require separate semantics; values are not clipped.
- ExcessIncidenceHazard.fromHazardRatio and ExcessMortalityHazard.fromHazardRatio provide total-HR convenience functions with the same protective-association decision.
- HazardRatio.ofExcess retains Result for consistency with the other validated constructors. For current valid finite non-negative excess HR values the conversion succeeds.
- ExcessHazardMultiplier.apply is a mathematical scaling operation, not scientific approval. Before using it, establish that the multiplier applies to the target endpoint, patient sex and reference normalization. A reported female-to-male comparison is not automatically an excess-HR multiplier for either sex. Passing a number with a source string does not establish applicability; the typed reported comparison cannot be passed directly to apply.

The printed-paper integration window remains e+1 through 110 years; its helper does not claim that one year is an endpoint-specific biological latency. Integration ordering, coverage and survival checks remain unchanged.
