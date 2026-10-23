module ModelCatalogue

open Feliz
open State
open Shared

/// Displays an evidence field with its descriptive label.
let private evidence (label: string) (value: string) =
    Html.div [ prop.className "evidence-item"; prop.children [ Html.dt label; Html.dd value ] ]

/// Displays an external publication link with a descriptive accessible label.
let private paperLink (label: string) (url: string) =
    Html.a [ prop.className "paper-link"; prop.href url; prop.target "_blank"; prop.rel "noopener noreferrer"; prop.text label ]

/// Displays the expandable three-model catalogue and selected model's complete definition.
let view model dispatch =
    Html.div [
        prop.className "catalogue-layout"
        prop.children [
            Html.aside [
                prop.className "catalogue-list panel"
                prop.children [
                    Html.div [ prop.className "panel-heading"; prop.children [ Html.span [ prop.className "eyebrow"; prop.text "MODEL CATALOGUE" ]; Html.span [ prop.className "count-badge"; prop.text "3" ] ] ]
                    for definition in model.Definitions do
                        Html.button [
                            prop.className ("catalogue-item " + if model.SelectedModel = definition.Id then "selected" else "")
                            prop.onClick (fun _ -> dispatch (SelectModel definition.Id))
                            prop.ariaPressed (model.SelectedModel = definition.Id)
                            prop.children [
                                Html.span [ prop.className "organ-dot"; prop.style [ style.backgroundColor definition.Color ] ]
                                Html.div [ prop.children [ Html.strong definition.Organ; Html.span definition.Endpoint; Html.small definition.Name ] ]
                                Html.span [ prop.className "catalogue-arrow"; prop.text "↗" ]
                            ]
                        ]
                    Html.p [ prop.className "catalogue-note"; prop.text "Published equations. Explicit coefficients. Traceable sources." ]
                ]
            ]
            match model.Definitions |> List.tryFind (fun item -> item.Id = model.SelectedModel) with
            | None -> Html.div [ prop.className "panel model-detail"; prop.text "Loading model definitions…" ]
            | Some definition ->
                Html.article [
                    prop.className "panel model-detail"
                    prop.children [
                        Html.div [ prop.className "detail-meta"; prop.children [ Html.span [ prop.className "model-family"; prop.text definition.Family ]; Html.span ("Definition v" + definition.Version) ] ]
                        Html.h2 definition.Endpoint
                        Html.p [ prop.className "model-byline"; prop.text (definition.Organ + " / " + definition.Name) ]
                        Html.p [ prop.className "model-description"; prop.text definition.Description ]
                        Html.div [ prop.className "equation-box"; prop.children [ Html.span [ prop.className "eyebrow"; prop.text "DOSE–RESPONSE EQUATION" ]; Html.div [ prop.className "equation"; prop.text definition.Formula ]; Html.small "D = mean organ dose in Gy; NTCP is a probability between 0 and 1." ] ]
                        Html.h3 "Published coefficients"
                        Html.table [
                            prop.className "coefficient-table"
                            prop.children [
                                Html.thead [ Html.tr [ Html.th "Symbol"; Html.th "Value"; Html.th "Unit"; Html.th "Meaning" ] ]
                                Html.tbody [ for parameter in definition.Parameters do Html.tr [ Html.td [ prop.className "coefficient-symbol"; prop.text parameter.Symbol ]; Html.td (sprintf "%g" parameter.Value); Html.td parameter.Unit; Html.td parameter.Description ] ]
                            ]
                        ]
                        Html.h3 "Model context"
                        Html.dl [ prop.className "evidence-grid"; prop.children [ evidence "Required input" definition.RequiredMetric; evidence "Population" definition.Population; evidence "Dose basis" definition.DoseBasis; evidence "Time horizon" definition.TimeHorizon ] ]
                        Html.div [ prop.className "model-limitations"; prop.children [ Html.strong "Applicability"; Html.p definition.Limitations ] ]
                        Html.div [ prop.className "publication"; prop.children [ Html.span [ prop.className "eyebrow"; prop.text "PUBLICATION & PROVENANCE" ]; Html.p definition.Citation; Html.div [ prop.className "paper-links"; prop.children [ paperLink "Read source paper ↗" definition.PaperUrl; paperLink "Equation / parameter source ↗" definition.ParameterSourceUrl ] ] ] ]
                    ]
                ]
        ]
    ]
