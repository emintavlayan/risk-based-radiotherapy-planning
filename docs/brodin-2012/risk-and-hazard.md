# Risk ratio and hazard ratio: conditional relationship

The quoted formula is mathematically valid under a single-event proportional-hazards model:

HR = ln(1 - RR × R0) / ln(1 - R0).

It is not a generic epidemiological conversion. It requires cumulative risks for the same event, follow-up horizon, starting risk set and comparison groups, and proportional hazards over the interval. It is not generally valid for a cause-specific hazard ratio inferred from cumulative incidence in the presence of competing risks.

## Derivation

Under proportional hazards, h1(t)=HR×h0(t). Thus H1(t)=HR×H0(t), and S1(t)=S0(t)^HR. In a single-event model, Rj(t)=1-Sj(t). Consequently:

1 - RR×R0 = (1-R0)^HR.

Taking logarithms gives the quoted expression. Once risks are supplied at the same known horizon, the numerical horizon does not separately enter this equation. It remains essential to their interpretation.

Require 0<R0<1 and 0<=RR×R0<1 for a finite result. If R0=0, the finite-horizon risks do not identify HR this way. If RR×R0=1, the logarithm diverges. Invalid or missing assumptions must not be silently repaired.

Source relationship: [MathWorks Cox model documentation](https://www.mathworks.com/help/stats/cox-proportional-hazard-regression.html), survival equation S_X(t)=S_0(t)^HR.

## Recalculated values for RR=1.7

| Reference cumulative risk | Comparison cumulative risk | HR under the stated model |
| --- | --- | --- |
| 5% | 8.5% | 1.731829 |
| 10% | 17% | 1.768495 |
| 20% | 34% | 1.862099 |
| 30% | 51% | 2.000000 |
| 40% | 68% | 2.230574 |

The formula in the quoted AI response is correct under these assumptions, but several of its tabulated values are incorrect.

For RR>1 and 0<R0<1/RR, the resulting HR is strictly greater than RR, tending to RR as R0 tends to zero. One way to see this is that f(x)=-ln(1-x)/x is increasing on (0,1), so HR/RR=f(RR×R0)/f(R0)>1.

Therefore this formula cannot transform RR=1.7 into HR=1.21 for the same comparison. It does not explain Brodin's multiplier. Brodin applies 1.21 to an already calculated excess HR; that is a separate operation with a separate reference.

## Implementation decision

No automatic risk-ratio-to-hazard-ratio conversion is added to the loss kernel: the required cumulative risks and model assumptions are not its inputs. A separately justified future workflow could implement this relationship with explicit event/horizon context and assumption checks.

The current domain instead distinguishes reported comparisons, total HR, excess HR, explicit excess-HR multipliers, baseline incidence/mortality hazards, and excess incidence/mortality hazards. Equal physical units do not make these types interchangeable.
