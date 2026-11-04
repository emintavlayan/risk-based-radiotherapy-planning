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
| RemainingLife | Remaining expected lifetime conditional on being alive at the event age | year | Different from cohort expected loss |
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

Survivor survival is not automatically conditioned on surviving to the current event age: doing so would remove the intended competing-mortality weighting. Post-event survival, in contrast, is conditional on reaching the event age. A truncated survival curve with a non-zero terminal value yields restricted mean survival; the full RemainingLife function rejects it.

No plan-specific competing-risk adjustment or first-event probability is inferred. No endpoint summation function labels overlapping events as an exact total expectancy decrement.


## Evidence and supplied mathematical inputs

ReportedSexComparison preserves the female-to-male direction, the reported association measure (or its unresolved meaning) and the source. It is not accepted by ExcessHazardMultiplier.apply.

ExcessHazardMultiplier means exactly a multiplier of HR-1. It is dimensionless and includes provenance. Its constructor validates the number and reference; it does not certify scientific validity. Multipliers are supplied explicitly rather than inferred from reported comparisons.

DoseResponse.linear accepts a supplied slope in Gy^-1. DoseResponse.thyroid accepts validated supplied slope and falloff in Gy^-1 and Gy^-2. Brodin2012 keeps publication-specific transcriptions separate from these generic equations.

A risk ratio or cumulative-event probability is not required by the implemented loss kernel. Add such types when a workflow actually computes or consumes them, with endpoint, comparison and time horizon. Never use an unqualified risk ratio in a hazard argument.
