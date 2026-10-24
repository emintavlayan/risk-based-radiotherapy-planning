module ModelCatalogue

open Feliz
open State
open Shared

/// Formats a catalogue implementation status without implying clinical validation.
let private availabilityText = function
    | Implemented -> "Executable prototype"
    | RequiresImplementation -> "Catalogued · implementation pending"
    | MissingParameters -> "Incomplete publication parameters"
    | NoDoseResponse -> "Evidence only · no dose–response"

/// Formats the meaning of an output without mixing probabilities and relative risks.
let private measureText = function
    | AbsoluteProbability -> "Absolute endpoint probability"
    | RelativeRisk -> "Relative risk (RR)"
    | OddsRatio -> "Odds ratio (OR)"
    | BaselineAdjustedIncidence -> "Baseline-adjusted incidence"
    | NoRiskEstimate -> "No calculable risk estimate"

/// Displays one labelled field of clinical evidence.
let private evidence (label: string) (value: string) =
    Html.div [ prop.className "evidence-item"; prop.children [ Html.dt label; Html.dd value ] ]

/// Displays an external publication link.
let private paperLink (label: string) (url: string) =
    Html.a [ prop.className "paper-link"; prop.href url; prop.target "_blank"; prop.rel "noopener noreferrer"; prop.text label ]


/// Draws a model-specific probability curve and its evaluated dose marker.
let private evaluatedCurve (result: CatalogueEvaluation) =
    let path =
        result.Curve
        |> List.mapi (fun i point ->
            let x = 57.0 + point.DoseGy / 80.0 * 692.0
            let y = 260.0 - point.Probability * 230.0
            sprintf "%s%.2f,%.2f" (if i = 0 then "M" else "L") x y)
        |> String.concat " "
    let marker = result.DosePredictorGy |> Option.defaultValue 0.0
    let mx = 57.0 + marker / 80.0 * 692.0
    let my = 260.0 - result.Value * 230.0
    Html.div [ prop.className "model-runner-plot"; prop.children [
        Svg.svg [ svg.viewBox (0, 0, 790, 310); svg.children [
            for tick in 0 .. 4 do
                let y = 260.0 - float tick * 57.5
                Svg.line [ svg.x1 57; svg.x2 749; svg.y1 y; svg.y2 y; svg.stroke "#e0e8e5"; svg.strokeDasharray [| 3; 6 |] ]
                Svg.text [ svg.x 46; svg.y (y + 4.0); svg.textAnchor.endOfText; svg.className "axis-tick"; svg.text (sprintf "%g" (float tick * 25.0)) ]
            for tick in 0 .. 4 do
                let x = 57.0 + float tick * 173.0
                Svg.text [ svg.x x; svg.y 280; svg.textAnchor.middle; svg.className "axis-tick"; svg.text (sprintf "%g" (float tick * 20.0)) ]
            Svg.path [ svg.d path; svg.fill "none"; svg.stroke "#377e81"; svg.strokeWidth 2.8 ]
            Svg.circle [ svg.cx mx; svg.cy my; svg.r 5.2; svg.fill "#bb5c46"; svg.stroke "white"; svg.strokeWidth 2 ]
            Svg.text [ svg.x 400; svg.y 306; svg.textAnchor.middle; svg.className "axis-label"; svg.text (result.DosePredictorLabel |> Option.defaultValue "Dose predictor (Gy)") ]
            Svg.text [ svg.x 23; svg.y 20; svg.textAnchor.middle; svg.className "axis-label"; svg.text "%" ]
        ] ]
    ] ]

/// Provides an explicit manual-input runner with no unsupported synthetic covariate defaults.
let private runner (definition: ModelDefinition) model dispatch =
    Html.section [ prop.className "model-runner"; prop.children [
        Html.div [ prop.className "runner-title"; prop.children [
            Html.span [ prop.className "eyebrow"; prop.text "MODEL CALCULATOR" ]
            Html.h3 "Evaluate with explicit metrics"
            Html.p "Only the measurements entered below are sent to the F# calculation server. No clinical covariates or missing DVHs are inferred."
        ] ]
        Html.div [ prop.className "runner-fields"; prop.children [
            for metric in definition.Inputs do
                let key = catalogueInputKey metric
                let value = model.CatalogueValues |> Map.tryFind key |> Option.defaultValue ""
                Html.div [ prop.className "runner-field"; prop.children [
                    Html.label [ prop.text ((if metric.OarId = "" then "Clinical" else metric.OarId) + " · " + metric.Description) ]
                    match metric.Kind with
                    | DifferentialDvh basis ->
                        Html.div [
                            Html.textarea [
                                prop.value value
                                prop.placeholder "0:0.2, 30:0.3, 45:0.5"
                                prop.rows 3
                                prop.onChange (fun (value: string) -> dispatch (SetCatalogueValue (key, value)))
                            ]
                            Html.small ("Differential DVH as doseGy:fraction bins in " + (match basis with | PhysicalGy -> "physical dose (Gy)" | EqD2Gy ab -> sprintf "EQD2 Gy using α/β = %g Gy" ab) + ". Fractions must sum to 1; cumulative DVHs are not accepted.")
                        ]
                    | _ ->
                        Html.input [
                            prop.type' "number"
                            prop.step 0.1
                            prop.placeholder (match metric.Kind with | ClinicalVariable name when name.StartsWith "baseline" -> "0 or 1" | VolumeCm3 -> "cm³" | _ -> "Enter value")
                            prop.value value
                            prop.onChange (fun (value: string) -> dispatch (SetCatalogueValue (key, value)))
                        ]
                ] ]
        ] ]
        Html.button [
            prop.className "runner-button"
            prop.disabled model.CatalogueBusy
            prop.onClick (fun _ -> dispatch EvaluateCatalogueModel)
            prop.text (if model.CatalogueBusy then "Calculating…" else "Calculate selected model")
        ]
        match model.CatalogueError with
        | Some error -> Html.p [ prop.className "runner-error"; prop.role "alert"; prop.text error ]
        | None -> Html.none
        match model.CatalogueEvaluation with
        | Some result when result.ModelId = definition.Id ->
            Html.div [ prop.className "runner-result"; prop.children [
                Html.strong "Server calculation result"
                Html.div [ prop.className "runner-value"; prop.text (
                    match result.OutputKind with
                    | AbsoluteProbability -> sprintf "%.3f %%" (result.Value * 100.0)
                    | RelativeRisk -> sprintf "RR %.4f" result.Value
                    | OddsRatio -> sprintf "OR %.4f" result.Value
                    | BaselineAdjustedIncidence -> sprintf "Incidence %.5f" result.Value
                    | NoRiskEstimate -> "Not available") ]
                Html.p (measureText result.OutputKind + " · " + definition.Endpoint)
                if not (List.isEmpty result.Curve) then evaluatedCurve result
                else Html.p "A relative effect is not plotted against an absolute-probability axis."
            ] ]
        | _ -> Html.none
    ] ]

/// Displays the full OAR-filtered source catalogue, including non-executable entries.
let view model dispatch =
    let visible = catalogueDefinitions model
    let organs = model.Definitions |> List.map (fun m -> m.OarId, m.Organ) |> List.distinct |> List.sortBy snd
    Html.div [
        prop.className "catalogue-layout"
        prop.children [
            Html.aside [
                prop.className "catalogue-list panel"
                prop.children [
                    Html.div [ prop.className "panel-heading"; prop.children [
                        Html.span [ prop.className "eyebrow"; prop.text "BRODIN TABLE 1 · MODEL CATALOGUE" ]
                        Html.span [ prop.className "count-badge"; prop.text (string visible.Length + " / " + string model.Definitions.Length) ]
                    ] ]
                    Html.div [ prop.className "catalogue-filter"; prop.children [
                        Html.label [ prop.htmlFor "catalogue-oar"; prop.text "FILTER BY ORGAN AT RISK" ]
                        Html.select [
                            prop.id "catalogue-oar"
                            prop.value (model.CatalogueOar |> Option.defaultValue "")
                            prop.onChange (fun (oar: string) -> dispatch (FilterCatalogueOar (if oar = "" then None else Some oar)))
                            prop.children [
                                Html.option [ prop.value ""; prop.text "All organs" ]
                                for id, organ in organs do Html.option [ prop.value id; prop.text organ ]
                            ]
                        ]
                    ] ]
                    Html.div [ prop.className "catalogue-scroll"; prop.children [
                        for definition in visible do
                            Html.button [
                                prop.className ("catalogue-item " + if model.SelectedModel = definition.Id then "selected" else "")
                                prop.onClick (fun _ -> dispatch (SelectModel definition.Id))
                                prop.ariaPressed (model.SelectedModel = definition.Id)
                                prop.children [
                                    Html.span [ prop.className "organ-dot"; prop.style [ style.backgroundColor definition.Color ] ]
                                    Html.div [ prop.children [
                                        Html.strong definition.Organ
                                        Html.span definition.Endpoint
                                        Html.small definition.Name
                                        Html.small (availabilityText definition.Availability)
                                    ] ]
                                    Html.span [ prop.className "catalogue-arrow"; prop.text "↗" ]
                                ]
                            ]
                    ] ]
                    Html.p [ prop.className "catalogue-note"; prop.text "A source-table inventory is not equivalent to clinical validation. Models with missing inputs or coefficients cannot be calculated." ]
                ]
            ]
            match model.Definitions |> List.tryFind (fun item -> item.Id = model.SelectedModel) with
            | None -> Html.div [ prop.className "panel model-detail"; prop.text "Loading model definitions…" ]
            | Some definition ->
                Html.article [
                    prop.className "panel model-detail"
                    prop.children [
                        Html.div [ prop.className "detail-meta"; prop.children [
                            Html.span [ prop.className "model-family"; prop.text definition.Family ]
                            Html.span ("Definition v" + definition.Version)
                        ] ]
                        Html.h2 definition.Endpoint
                        Html.p [ prop.className "model-byline"; prop.text (definition.Organ + " / " + definition.Name) ]
                        Html.p [ prop.className "model-description"; prop.text definition.Description ]
                        Html.div [ prop.className "model-status-line"; prop.children [
                            Html.strong (availabilityText definition.Availability)
                            Html.span (" · " + measureText definition.OutputKind)
                        ] ]
                        Html.div [ prop.className "equation-box"; prop.children [
                            Html.span [ prop.className "eyebrow"; prop.text "PUBLISHED DOSE–RESPONSE RELATIONSHIP" ]
                            Html.div [ prop.className "equation"; prop.text definition.Formula ]
                            Html.small ("Output meaning: " + measureText definition.OutputKind + ". Formula display does not imply implementation.")
                        ] ]
                        Html.h3 "Required inputs"
                        Html.div [ prop.className "model-requirements"; prop.children [
                            if List.isEmpty definition.Inputs then Html.p "No patient-specific dose-response inputs established."
                            for requirement in definition.Inputs do
                                Html.p ((if requirement.OarId = "" then "Clinical" else requirement.OarId) + " · " + requirement.Description)
                        ] ]
                        Html.h3 "Published coefficients"
                        Html.table [
                            prop.className "coefficient-table"
                            prop.children [
                                Html.thead [ Html.tr [ Html.th "Symbol"; Html.th "Value"; Html.th "Unit"; Html.th "Meaning" ] ]
                                Html.tbody [
                                    if List.isEmpty definition.Parameters then Html.tr [ Html.td [ prop.colSpan 4; prop.text "No coefficients reported." ] ]
                                    for parameter in definition.Parameters do
                                        Html.tr [
                                            Html.td [ prop.className "coefficient-symbol"; prop.text parameter.Symbol ]
                                            Html.td (sprintf "%g" parameter.Value)
                                            Html.td parameter.Unit
                                            Html.td parameter.Description
                                        ]
                                ]
                            ]
                        ]
                        if definition.Availability = Implemented then runner definition model dispatch
                        Html.h3 "Model context"
                        Html.dl [ prop.className "evidence-grid"; prop.children [
                            evidence "Anatomic region" definition.Region
                            evidence "Risk measure" (measureText definition.OutputKind)
                            evidence "Required metric" definition.RequiredMetric
                            evidence "Population" definition.Population
                            evidence "Dose basis" definition.DoseBasis
                            evidence "Time horizon" definition.TimeHorizon
                        ] ]
                        Html.div [ prop.className "model-limitations"; prop.children [
                            Html.strong "Applicability and limitations"
                            Html.p definition.Limitations
                        ] ]
                        Html.div [ prop.className "publication"; prop.children [
                            Html.span [ prop.className "eyebrow"; prop.text "PUBLICATION & PROVENANCE" ]
                            Html.p definition.Citation
                            Html.div [ prop.className "paper-links"; prop.children [
                                paperLink "Read source publication ↗" definition.PaperUrl
                                paperLink "Model provenance ↗" definition.ParameterSourceUrl
                            ] ]
                        ] ]
                    ]
                ]
        ]
    ]
