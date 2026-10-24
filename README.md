# Risk-Based Radiotherapy Planning

An F# / SAFE Stack application for evaluating radiotherapy plans using published normal tissue complication probability (NTCP) models, patient characteristics, and clinical risk factors.

The initial research focus is lymphoma. The intended workflow combines plan dosimetry with evidence-based models to present endpoint-specific risk estimates and support treatment-planning decisions.

## Current status

The research prototype includes a manual-entry calculator, **21 executable dose-response/relative-effect models** (three in the main calculator and 18 additional in the model runner), and a **29-entry model/evidence catalogue** transcribed from Brodin et al. Table 1, spanning **17 OAR categories**. The catalogue distinguishes implementation status, risk measures (NTCP/RR/OR/incidence), and required patient or DVH metrics. Synthetic inputs load automatically, and calculations run on the SAFE server.

## Implemented prototype

The calculator includes:

- A patient and plan section containing age at plan creation, recorded sex, and the dosimetric inputs required by each model.
- A clinical factors section with clearly labelled selections and toggles.
- A source-linked 29-entry Brodin Table 1 catalogue: 21 executable model equations (including RR/OR as distinct outputs) and 8 blocked/evidence-only entries.
- Interactive dose-response curves with a marker showing the current plan's input and estimated risk. A manual available-OAR selector controls which implemented models the server evaluates; the catalogue can be filtered independently by organ.

The initial plan calculator retains cardiac mortality (Hancock / Brodin), radiation pneumonitis (Marks / QUANTEC) and radiation-induced liver disease (Pan / QUANTEC). In the NTCP catalogue, 18 additional models can be evaluated individually using explicit dosimetric and clinical inputs, including differential DVHs. All executable equations run on the server.

The three original calculator equations use dose only, and its optional clinical factors do not modify them. For the additional multivariable models, age, baseline symptoms and other covariates must be supplied explicitly in the catalogue runner. Missing required values are rejected. RR/OR estimates are not presented as absolute complication probabilities.

See [the full model catalogue and ESAPI eligibility design](docs/MODEL-CATALOGUE.md) for the transcription, readiness classifications and future DVH metric contract. See [the demo guide](docs/DEMO.md) for a short presentation sequence, exact parameter sources, and server deployment commands. This prototype has not been validated for clinical decisions.

## Architecture

- **SAFE server:** input validation, model evaluation, and calculation workflows.
- **Fable / Elmish client:** input forms, model definitions, and interactive visualisation.
- **Shared contracts:** the input and output definitions used between client and server.
- **Future read-only ESAPI plugin:** extract plan data, resolve structure names, and obtain the required DVH metrics for submission to the server.

Manual entry and ESAPI import will feed the same calculation workflow. Patient age will have an explicit reference date so that age at plan creation and age at treatment are distinguishable where required.

## Development principles

- Light domain-driven design with explicit F# types.
- Small modules and pure, clearly named calculation functions.
- Side effects at application boundaries.
- Railway-oriented error handling using `Result<_, string>` and `FsToolkit.ErrorHandling` computation expressions.
- A single-line `///` documentation comment for every function.
- Published equations, units, applicability, model versions, and evidence kept traceable.
- Numerical checks against independently calculated reference examples.

## Planned direction

1. Confirm source-specific contour and clinical-input conventions and implement the eight currently blocked/evidence-only models only if adequate original data become available. Model uncertainty and external clinical validation remain future research.
2. ESAPI extraction and structure-name / DVH-metric mapping.
3. Comparison of multiple plans and their individual endpoint estimates.
4. Life-years-lost calculations with the required survival, prognosis, and competing-risk data.
5. Planning suggestions and automated generation of deliverable alternatives.
6. Clinical evaluation of plan trade-offs, including patient preferences.

Life years lost is one outcome within the broader risk profile. Endpoint probabilities, relative risks, and life-years-lost estimates retain their own meanings and units.

## Run locally

### Prerequisites

- .NET 8 SDK, as selected by `global.json`.
- Node.js 18 or higher.
- npm 9 or higher.

The application retains the SAFE template's existing build commands.

### Start the application

From the repository root:

```sh
dotnet run
```

Open <http://localhost:8080>.

### Run the tests

```sh
dotnet run -- WatchRunTests
```

Client tests are served at <http://localhost:8081>; server tests run in the terminal.

For a headless test run:

```sh
dotnet run -- RunTestsHeadless
```

### Build a deployment bundle

```sh
dotnet run -- Bundle
```

The template also contains an `Azure` target. Its deployment configuration in `Build.fs` must be customised before use.

A multi-stage [Dockerfile](Dockerfile) is included for server hosting. The [demo guide](docs/DEMO.md#run-a-server-bundle) also shows how to run the compiled bundle directly. GitHub Actions runs the numerical and client-state tests and builds an application bundle.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/Client` | Fable / Elmish client |
| `src/Server` | Saturn server and 29-entry source catalogue |
| `src/Shared` | Shared F# types and contracts |
| `tests/Client` | Client tests |
| `tests/Server` | Server tests |
| `tests/Shared` | Shared tests |
| `Build.fs` | Development, test, and packaging targets |

## Scientific starting points

- [Brodin et al. (2014): Interactive decision-support tool for risk-based radiation therapy plan comparison for Hodgkin lymphoma](https://doi.org/10.1016/j.ijrobp.2013.10.028).
- [Brodin et al. (2012): Life years lost — comparing potentially fatal late complications after radiotherapy for pediatric medulloblastoma on a common scale](https://pmc.ncbi.nlm.nih.gov/articles/PMC3402656/).

## SAFE Stack

The application starts from the [SAFE template](https://safe-stack.github.io/docs/template-overview/). Framework documentation:

- [SAFE Stack](https://safe-stack.github.io/docs/)
- [Saturn](https://saturnframework.org/)
- [Fable](https://fable.io/docs/)
- [Elmish](https://elmish.github.io/elmish/)
