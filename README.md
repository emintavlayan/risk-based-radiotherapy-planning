# Risk-Based Radiotherapy Planning

**Work in progress.** This repository is a fresh F# / SAFE foundation for a life-years-lost application. Scientific domain modelling and calculation workflows have not yet been implemented.

The previous calculator and published-model catalogue are preserved in [NTCP Explorer](https://github.com/emintavlayan/ntcp-explorer).

## Current state

- Fable / Elmish client with a minimal application screen.
- Saturn server, typed infrastructure API and shared contracts.
- Existing development, packaging and test harnesses.
- No scientific inputs, equations, coefficients or numerical results.

## Next work

Review the source papers together, agree terminology and notation, then develop small explicit domain types and pure calculation functions. Keep paper symbols, units, assumptions and source references traceable. Verify numerical examples before introducing application workflows.

Use small modules, single-line `///` documentation for every function, and `Result<_, string>` with `FsToolkit.ErrorHandling` for fallible workflows. No domain design is implied by the infrastructure API.

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

The test harness currently contains empty suites. Passing them establishes no scientific validation. GitHub Actions compiles the solution, runs the harness and packages the server and client.

## Layout

| Path | Purpose |
| --- | --- |
| src/Client | Minimal client and styling |
| src/Server | Infrastructure host and status API |
| src/Shared | Infrastructure contracts |
| tests | Retained test harnesses |
| Build.fs | Development and packaging commands |

The existing running deployment is separate from this source cleanup.
