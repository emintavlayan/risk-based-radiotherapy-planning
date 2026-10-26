# Clinical focus UI concept

The research application provides two **views of the same Elmish state and F# server model output**.

## Design intent

The `Clinical focus` calculator removes repetitive explanatory text and fields unused by the three source equations in the main calculator. The view presents:

1. The three input mean doses (heart, lungs, liver), with explicit Gy units.
2. One compact risk estimate per endpoint, retaining the endpoint name and current organ dose.
3. One scientific dose–response figure with live plan markers.
4. Direct access to the source-linked model catalogue.
5. An explicit link to `Research detail`, which remains the original unmodified detailed calculator layout.

The detailed view retains plan label, age, recorded sex, clinical factors, input toggles, full estimates, original plot and all explanatory content. Changing presentation modes does not re-request server evaluation or change the patient/plan form. The catalogue remains unchanged.

## Adaptive plot

`Detail` is not a hard-coded 0–30 Gy / 0–30% viewport anymore. It automatically fits all evaluated plan markers:

- Dose axis upper limit: round `max(30 Gy, maximum input dose + 2 Gy)` up to the next 5 Gy, capped at 60 Gy.
- Probability axis upper limit: round `max(5%, maximum endpoint probability + 4 percentage points)` up to the next 5 percentage points, capped at 100%.
- Both axes begin at zero. Full range stays 0–50 Gy and 0–100%.
- Because endpoints differ in clinical severity, type and follow-up, none of the numeric estimates is interpreted as a prioritization or ranked risk indicator. Coloured lines identify specific endpoints only.

The plot bounds are calculated by a pure F# function `State.plotBounds` from the **actual plan estimates returned by the F# server**, not dummy values or cached chart assumptions. The original dose-response server samples and marker equations remain unchanged.

## Why this layout

The split follows *progressive disclosure*: clinical work starts with the information used to perform the task, while detailed patient, study and numerical context remains one click away. Visible colour is reserved for OAR identity rather than status. The composition uses deliberate negative space, restrained decoration and readable, high-contrast type. A 60/30/10 colour heuristic may help balance surfaces, text and highlights but is **not** an accessibility or safety rule.

Key usability sources:

- Nielsen Norman Group, [Progressive Disclosure](https://www.nngroup.com/articles/progressive-disclosure/)
- Nielsen Norman Group, [Design Guidelines for Complex Applications](https://www.nngroup.com/articles/complex-application-design/)

## Verification

Client tests cover adaptive bounds for a 30 Gy lung input, smaller baseline axis limits, stable full range and lossless mode switching. Playwright runs against a compiled SAFE bundle and checks all dose/risk markers remain within the scientific plot, both views share edited dose values, and three screenshot captures are generated.

This is an **interface prototype**. The implemented NTCP equations are not a validated clinical decision-support system.
