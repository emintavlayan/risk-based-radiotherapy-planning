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
