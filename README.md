# Risk-Based Radiotherapy Planning

**Work in progress.** An F# / SAFE application for modelling radiation-related late effects as expected life years lost. The scientific domain uses explicit quantities, units of measure and pure calculation functions, with publication-specific parameters kept in separate adapters.

The previous calculator and published-model catalogue are preserved in [NTCP Explorer](https://github.com/emintavlayan/ntcp-explorer).

## Current state

- Fable / Elmish client with a minimal application screen.
- Saturn server, typed infrastructure API and shared contracts.
- Existing development, packaging and test harnesses.
- Independent LifeYearsLost domain with typed hazards, dose responses, survival weighting and numerical integration.
- [Domain and source-code guide](docs/README.md).
- No clinical calculation workflow or loaded population datasets.

## Scientific domain

Start with the [documentation guide](docs/README.md). It maps the clinical vocabulary, calculation flow and units to the F# files. The current [Brodin 2012 adapter](docs/brodin-2012/README.md) supplies one set of publication-specific parameters.

```sh
dotnet run --project tests/LifeYearsLost/LifeYearsLost.Tests.fsproj
```

The domain tests use synthetic inputs to verify equations and invariants. Coefficients, population data and modifiers are explicit inputs with source information.

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
| docs | Domain guide, model references and separate research notes |
| tests | Retained test harnesses |
| Build.fs | Development and packaging commands |

The existing running deployment is separate from this source cleanup.
