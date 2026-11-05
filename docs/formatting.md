# F# formatting

The repository pins Fantomas 6.3.9 in the local tool manifest. Formatting rules live in .editorconfig, and the VS Code workspace enables format on save for F#.

- Keep one blank line between type declarations and between function declarations, before their documentation comment.
- Start a function body on the line after its equals sign.
- Put each pipeline operator on a new line.
- Start multiline computation expressions on the line after the binding.

```fsharp
/// Creates a baseline incidence hazard in inverse years.
let create value =
    AnnualRate.create value
    |> Result.map IncidenceHazard

/// Returns the incidence hazard in inverse years.
let value (IncidenceHazard rate) =
    AnnualRate.value rate
```

From the repository root:

```sh
dotnet tool restore
dotnet fantomas src/LifeYearsLost tests/LifeYearsLost
```

To check without editing:

```sh
dotnet fantomas --check src/LifeYearsLost tests/LifeYearsLost
```

## Formatter boundaries

fsharp_max_function_binding_width = 0 puts ordinary function bodies below the binding. fsharp_newline_before_multiline_computation_expression = true does the same for multiline computation expressions.

fsharp_max_infix_operator_expression = 0 makes pipelines multiline. In this Fantomas version, that setting also applies to arithmetic and other infix operators; there is no separate pipeline-only width setting.

Fantomas preserves existing blank lines between short type declarations but does not insert all of them automatically. The current source includes those blank lines; retain one when adding declarations. fsharp_keep_max_number_of_blank_lines = 1 prevents extra consecutive blank lines.

These settings do not guarantee that every short anonymous lambda is expanded. Prefer a documented named helper when that makes the operation clearer.
