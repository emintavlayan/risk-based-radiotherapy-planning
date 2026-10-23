# Demonstrating the prototype

## Run locally

From the repository root:

```powershell
git pull --ff-only
dotnet run
```

Open <http://localhost:8080>. The application loads a synthetic example automatically.

## A short demonstration

1. Show the patient and plan inputs, clinical-factor switches, three dose-response curves, and current-plan markers.
2. Change mean lung dose from 14 Gy to 20 Gy. The lung marker moves along the same published curve and its estimate changes from approximately 10.85% to 20.59%.
3. Select **Detail** to inspect the lower-dose portion of the curves; **Full range** restores the complete mathematical display range.
4. Open **NTCP models**. Select heart, lungs, or liver to inspect the description, equation, coefficient values and units, model context, and publication links.
5. Return to **Calculator**. Inputs are preserved. **Reset example** restores all initial values.

The three example equations are dose-only. Age, recorded sex, cardiac risk factors, and chemotherapy are represented in the form, but do not introduce unsupported effects into these equations. This is stated beside the controls.

The outputs concern different endpoints and follow-up periods. They are not summed, and their percentages are not presented as equivalent measures of clinical severity.

## Included models

| Organ | Endpoint | Equation | Parameter source |
| --- | --- | --- | --- |
| Heart | Cardiac mortality | Linear: `a × D` | Brodin 2014, Table 1, derived from Hancock 1993; `a = 0.00105 Gy⁻¹` |
| Lungs | Radiation pneumonitis | Logistic: `1 / (1 + exp(−(a + b × D)))` | Marks / QUANTEC 2010; `a = −3.87`, `b = 0.126 Gy⁻¹` |
| Liver | Radiation-induced liver disease | LKB with `n = 1`: `Φ((D − TD50) / (m × TD50))` | Brodin 2014, Table 1, citing Pan / QUANTEC 2010; `TD50 = 39.8 Gy`, `m = 0.12` |

Here `D` is the required mean organ dose in physical Gy. The plotted 0–50 Gy range is a display range, not a statement of model validation across that range. The equations are for conventionally fractionated source settings; no automatic fractionation conversion is performed.

## Run a server bundle

```powershell
dotnet run -- Bundle
cd deploy
$env:ASPNETCORE_URLS = 'http://0.0.0.0:8080'
dotnet Server.dll
```

The bundle contains the server and compiled client assets. The host requires the .NET 8 ASP.NET Core runtime. Use the host's HTTPS reverse proxy when exposing the application outside a local demonstration.

Alternatively, build the supplied Docker image:

```sh
docker build -t risk-based-radiotherapy-planning .
docker run --rm -p 8080:8080 risk-based-radiotherapy-planning
```

## Verification

```powershell
dotnet run -- RunTestsHeadless
```

The server checks published-equation reference values, the normal CDF, curve/marker consistency, monotonicity, unsupported clinical-factor effects, and input validation. Client tests check stale responses, catalogue navigation, empty numeric fields, and resetting the example.

This is a research demonstration and has not been validated for clinical decisions.
