# Risk-Based Radiotherapy Planning

**Work in progress.** This repository is a fresh F# / SAFE foundation for a life-years-lost application. A separate research domain now models the Brodin 2012 life-years-lost equations with units of measure and an explicit scientific audit.

The previous calculator and published-model catalogue are preserved in [NTCP Explorer](https://github.com/emintavlayan/ntcp-explorer).

## Current state

- Fable / Elmish client with a minimal application screen.
- Saturn server, typed infrastructure API and shared contracts.
- Existing development, packaging and test harnesses.
- Independent LifeYearsLost domain with typed hazards, dose responses, survival weighting and numerical integration.
- [Scientific reading guide and equation audit](docs/brodin-2012/README.md), including unresolved interpretations.
- No clinical calculation workflow or loaded population datasets.

## Scientific domain

Start with the [Brodin 2012 reading guide](docs/brodin-2012/README.md). It maps the clinical vocabulary to the F# files and distinguishes transcription, implementation choices and possible article mistakes.

```sh
dotnet run --project tests/LifeYearsLost/LifeYearsLost.Tests.fsproj
dotnet fsi examples/Brodin2012.fsx
```

The Figure 2 example is an age-specific calculation. Published lifetime totals and modality comparisons are not reproduced: the original population tables, survival fits and patient DVHs are not supplied.

## Development

Requires .NET 8, Node.js 18/20/22 and a compatible npm version.

```sh
dotnet run
```

Open http://localhost:8080.

```sh
dotnet run -- RunTestsHeadless
dotnet run -- Bundle
```

The retained infrastructure suites are empty. The separate scientific suite exercises the domain equations, numerical invariants and invalid inputs; it does not establish clinical validity. GitHub Actions runs it and compile-time misuse checks before the infrastructure build and bundle.

## Layout

| Path | Purpose |
| --- | --- |
| src/Client | Minimal client and styling |
| src/Server | Infrastructure host and status API |
| src/Shared | Infrastructure contracts |
| src/LifeYearsLost | Independent scientific domain |
| docs/brodin-2012 | Clinical vocabulary, source map and equation audit |
| tests | Retained test harnesses |
| Build.fs | Development and packaging commands |

The existing running deployment is separate from this source cleanup.
