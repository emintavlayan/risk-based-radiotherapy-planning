# Lean, plot-first NTCP calculator

## Single-view design

The application now shows one calculator presentation: **dosimetric inputs on the left; the dose-response graph first and a compact endpoint table below it on the right**. No separate Clinical focus / Research detail toggle is rendered. The original more verbose calculator components and the entire 29-entry research catalogue remain in the F# source, and the NTCP model catalogue is still accessible from the top navigation.

- Top navigation is right-aligned; the header research-prototype label, marketing heading, example banner, reset-example button, and instructional prose from the calculator screen are not rendered. The scientific research disclaimer remains in the footer.
- The three present OAR inputs are explicitly labelled **Heart · Dmean**, **Lungs · Dmean**, and **Liver · Dmean**, with units Gy. **They are all mean-dose models; no D20% or other DVH-point metric is represented by those inputs.** Additional catalogue entries have distinct typed dose/volume metrics and remain in the catalogue runner.
- OAR inclusion toggle functionality remains as compact buttons below the dose inputs. The model catalogue and its references, equations, clinical factors, and DVH-specific inputs remain available from the right-hand navigation.
- Endpoint rows contain OAR, endpoint, mean dose, absolute NTCP (%), and finite **ΔNTCP (−1 Gy)** in percentage points, plus a direct model link.
- The graph renders original server-returned curves and plan markers. A short thickened interval connects the plan marker to the risk at 1 Gy less dose, with an open circle marking the lower-dose point; these values come from the **same server result** `ReductionForOneGy = p(D)−p(max(0,D−1 Gy))`.
- Each legend control highlights or clears focus on one organ's curve without navigating away. Model navigation is available from the estimate table.

## Scientific interpretation

`ΔNTCP (−1 Gy)` is a **finite one-Gy decrease in the probability of its stated endpoint**. It is not mathematically the instantaneous derivative, nor an estimate of survival, quality-adjusted life-years, or life-years gained. Severity, competing mortality, follow-up horizons, and baseline risks differ among endpoint models; direct comparison of endpoint-specific percentage-point changes cannot establish which organ reduction maximizes life expectancy.

The present calculations are *research prototypes* using synthetic dosimetric examples, not a validated clinical decision-support system.

## Plot bounds

Adaptive bounds are pure functions of the current F# server estimates. The visual range expands if a current dose or risk would otherwise be clipped, while the Full range remains fixed at 0–50 Gy and 0–100% probability. Switching to Full range affects only presentation.

## Code locations

- `src/Client/Index.fs`: single lean calculator, exact metric labels, plot-first results, OAR highlight, 1-Gy reduction table.
- `src/Client/Plot.fs`: graph axes, one-Gy segment and marker.
- `src/Client/State.fs`: preserved plan model, catalogue, numerical state and pure adaptive-plot calculation.
- `src/Server/RiskCalculation.fs`: source-of-truth NTCP predictions and one-Gy reduction.
- `src/Client/index.css`: restrained layout, legible data hierarchy and mobile adaptation.
- `tests/e2e/clinical_focus.py`: actual compiled SAFE user-interface integration checks.

The research calculations, API contracts, model catalogue, and original detailed calculator functions have not been deleted.
