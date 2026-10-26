module Index

open Feliz
open State
open Shared

/// Displays the application mark without external image dependencies.
let private brandMark =
    Html.div [ prop.className "brand-mark"; prop.children [
        Svg.svg [ svg.viewBox (0, 0, 40, 40); svg.children [
            Svg.path [ svg.d "M5 29C14 29 11 11 20 11S26 29 35 29"; svg.fill "none"; svg.stroke "#d6e9e4"; svg.strokeWidth 2.3 ]
            Svg.line [ svg.x1 5; svg.x2 35; svg.y1 20; svg.y2 20; svg.stroke "#d6e9e4"; svg.strokeWidth 1; svg.custom ("opacity", 0.45) ]
            Svg.circle [ svg.cx 20; svg.cy 11; svg.r 2.7; svg.fill "#edc38a" ]
        ] ]
    ] ]

/// Displays a section heading with a small numbered marker.
let private sectionHeading (number: string) (title: string) (subtitle: string) =
    Html.div [ prop.className "section-heading"; prop.children [
        Html.span [ prop.className "section-number"; prop.text number ]
        Html.div [ Html.h2 title; Html.p subtitle ]
    ] ]

/// Displays a validated numeric form control with its unit.
let private numberField (id: string) (label: string) (unit: string) (value: string) (minimum: int) (maximum: int) change =
    Html.div [ prop.className "form-field"; prop.children [
        Html.label [ prop.htmlFor id; prop.text label ]
        Html.div [ prop.className "number-field"; prop.children [
            Html.input [ prop.id id; prop.type' "number"; prop.step 0.1; prop.min minimum; prop.max maximum; prop.value value; prop.onChange (fun (text: string) -> change text) ]
            Html.span unit
        ] ]
    ] ]

/// Displays a button group for the recorded sex.
let private sexField (form: Form) dispatch =
    Html.div [ prop.className "form-field"; prop.children [
        Html.label [ prop.id "sex-label"; prop.text "Recorded sex" ]
        Html.div [ prop.className "segmented sex-group"; prop.role "group"; prop.ariaLabelledBy "sex-label"; prop.children [
            for value, label in [ Female, "Female"; Male, "Male"; NotRecorded, "Not recorded" ] do
                Html.button [ prop.className (if form.Sex = value then "active" else ""); prop.ariaPressed (form.Sex = value); prop.onClick (fun _ -> dispatch (SetForm { form with Sex = value })); prop.text label ]
        ] ]
    ] ]

/// Displays an accessible on/off clinical-factor switch.
let private factor (label: string) (detail: string) (active: bool) change =
    Html.button [ prop.className "factor-row"; prop.role "switch"; prop.ariaChecked active; prop.ariaLabel label; prop.onClick (fun _ -> change (not active)); prop.children [
        Html.div [ Html.strong label; Html.small detail ]
        Html.span [ prop.className ("switch " + if active then "on" else ""); prop.children [ Html.span [] ] ]
    ] ]

/// Displays mean organ dose inputs linked to the live calculation workflow.
let private doseField (id: string) (organ: string) (endpoint: string) (color: string) (value: string) change =
    Html.div [ prop.className "dose-row"; prop.children [
        Html.div [ prop.className "dose-heading"; prop.children [
            Html.span [ prop.className "organ-dot"; prop.style [ style.backgroundColor color ] ]
            Html.label [ prop.htmlFor id; prop.text organ ]
            Html.span [ prop.className "mean-label"; prop.text "MEAN DOSE" ]
        ] ]
        Html.div [ prop.className "number-field dose-number"; prop.children [
            Html.input [ prop.id id; prop.type' "number"; prop.min 0; prop.max 50; prop.step 0.1; prop.value value; prop.onChange (fun (text: string) -> change text) ]
            Html.span "Gy"
        ] ]
        Html.small [ prop.className "dose-endpoint"; prop.text endpoint ]
    ] ]

/// Displays the patient, plan and clinical-factor input pane.
let private inputPane model dispatch =
    let form = model.Form
    Html.aside [ prop.className "input-pane"; prop.children [
        Html.section [ prop.className "panel input-panel"; prop.children [
            sectionHeading "01" "Patient & plan" "Synthetic example · manual entry"
            Html.div [ prop.className "form-field"; prop.children [
                Html.label [ prop.htmlFor "plan-label"; prop.text "Plan label" ]
                Html.input [ prop.className "text-input"; prop.id "plan-label"; prop.maxLength 80; prop.value form.Label; prop.onChange (fun value -> dispatch (SetForm { form with Label = value })) ]
            ] ]
            numberField "patient-age" "Age at plan creation" "years" form.Age 0 110 (fun value -> dispatch (SetForm { form with Age = value }))
            sexField form dispatch
            Html.div [ prop.className "subsection-title"; prop.text "PLAN DOSIMETRY" ]
            doseField "heart-dose" "Heart" "Cardiac mortality" "#bc5b47" form.Heart (fun value -> dispatch (SetForm { form with Heart = value }))
            doseField "lung-dose" "Lungs" "Radiation pneumonitis" "#287e99" form.Lung (fun value -> dispatch (SetForm { form with Lung = value }))
            doseField "liver-dose" "Liver" "Radiation-induced liver disease" "#7c62a6" form.Liver (fun value -> dispatch (SetForm { form with Liver = value }))
            Html.div [ prop.className "subsection-title"; prop.text "AVAILABLE OARS FOR RISK PLOT" ]
            Html.div [ prop.className "available-oars"; prop.children [
                for id, label in [ "heart", "Heart"; "lungs", "Lungs"; "liver", "Liver" ] do
                    let enabled = List.contains id form.AvailableOars
                    Html.button [
                        prop.className ("available-oar " + if enabled then "active" else "")
                        prop.ariaPressed enabled
                        prop.onClick (fun _ -> dispatch (SetForm (toggleAvailableOar id form)))
                        prop.text ((if enabled then "✓ " else "○ ") + label)
                    ]
            ] ]
            Html.p [ prop.className "factor-note"; prop.text "Only models with implemented equations and available OAR inputs are evaluated. Later the ESAPI import will supply the available structures and metrics." ]
        ] ]
        Html.section [ prop.className "panel input-panel clinical-panel"; prop.children [
            sectionHeading "02" "Clinical factors" "Patient & treatment context"
            factor "Cardiac risk factors" "Pre-existing cardiovascular risk" form.CardiacRiskFactors (fun value -> dispatch (SetForm { form with CardiacRiskFactors = value }))
            factor "Chemotherapy" "Systemic treatment received" form.Chemotherapy (fun value -> dispatch (SetForm { form with Chemotherapy = value }))
            Html.p [ prop.className "factor-note"; prop.text "Recorded for the example. These three published models use dose only; age, sex and these factors do not alter their curves." ]
        ] ]
    ] ]

/// Displays one endpoint estimate and the model-defined effect of a one-gray dose reduction.
let private estimateCard (definition: ModelDefinition) (estimate: EndpointEstimate) dispatch =
    Html.article [ prop.className "estimate-card"; prop.style [ style.custom ("--organ-color", definition.Color) ]; prop.children [
        Html.div [ prop.className "estimate-heading"; prop.children [ Html.span [ prop.className "organ-dot"; prop.style [ style.backgroundColor definition.Color ] ]; Html.strong definition.Organ; Html.span [ prop.className "dose-chip"; prop.text (sprintf "%.1f Gy" estimate.DoseGy) ] ] ]
        Html.div [ prop.className "estimate-value"; prop.children [ Html.span (riskText estimate.Probability); Html.small "%" ] ]
        Html.p [ prop.className "estimate-endpoint"; prop.text definition.Endpoint ]
        Html.div [ prop.className "estimate-bottom"; prop.children [
            Html.span [ prop.children [ Html.small "With 1 Gy less"; Html.strong (if estimate.DoseGy = 0.0 then "0.00 pp" else "−" + riskText estimate.ReductionForOneGy + " pp") ] ]
            Html.button [ prop.className "model-link"; prop.onClick (fun _ -> dispatch (SelectModel definition.Id)); prop.text "View model ↗" ]
        ] ]
    ] ]

/// Displays the response plot, legend and individual endpoint estimates.
let private resultsPane model dispatch =
    let estimates = model.Evaluation |> Option.map _.Estimates |> Option.defaultValue []
    Html.div [ prop.className "results-pane"; prop.children [
        Html.section [ prop.className "panel chart-panel"; prop.children [
            Html.div [ prop.className "chart-heading"; prop.children [
                Html.div [ Html.span [ prop.className "eyebrow"; prop.text "PLAN-SPECIFIC RISK PROFILE" ]; Html.h2 "Dose–response relationships"; Html.p "Published curves. Your plan, in context." ]
                Html.span [ prop.className ("calculation-status " + if model.Busy then "busy" else ""); prop.children [ Html.span [ prop.className "status-dot" ]; Html.text (if model.Busy then "Updating" else "Calculated") ] ]
            ] ]
            Html.div [ prop.className "plot-toolbar"; prop.children [
                Html.div [ prop.className "plot-legend"; prop.children [
                    for definition in plottedDefinitions model do
                        Html.button [ prop.className "legend-item"; prop.onMouseEnter (fun _ -> dispatch (FocusModel (Some definition.Id))); prop.onMouseLeave (fun _ -> dispatch (FocusModel None)); prop.onFocus (fun _ -> dispatch (FocusModel (Some definition.Id))); prop.onBlur (fun _ -> dispatch (FocusModel None)); prop.onClick (fun _ -> dispatch (SelectModel definition.Id)); prop.children [ Html.span [ prop.className "legend-line"; prop.style [ style.backgroundColor definition.Color ] ]; Html.text definition.Organ ] ]
                ] ]
                Html.div [ prop.className "segmented plot-range"; prop.role "group"; prop.ariaLabel "Plot range"; prop.children [
                    Html.button [ prop.className (if not model.Zoom then "active" else ""); prop.onClick (fun _ -> dispatch (SetZoom false)); prop.text "Full range" ]
                    Html.button [ prop.className (if model.Zoom then "active" else ""); prop.onClick (fun _ -> dispatch (SetZoom true)); prop.text "Detail" ]
                ] ]
            ] ]
            match model.Error with
            | Some error -> Html.div [ prop.className "error-message"; prop.role "alert"; prop.text error ]
            | None when model.Evaluation.IsNone -> Html.div [ prop.className "loading-chart"; prop.text "Loading the example risk profile…" ]
            | None when List.isEmpty estimates -> Html.div [ prop.className "loading-chart"; prop.text "No implemented NTCP models match the available OARs." ]
            | None -> Plot.view model dispatch
            Html.div [ prop.className "chart-caption"; prop.children [
                Html.div [ Html.span [ prop.className "marker-key" ]; Html.span "Filled points show the current plan" ]
                Html.span "Mean dose · physical Gy · conventional fractionation"
            ] ]
        ] ]
        Html.div [ prop.className "estimates-heading"; prop.children [ Html.h2 "Estimated endpoints"; Html.span "Model outputs for the current plan" ] ]
        Html.div [ prop.className "estimate-grid"; prop.ariaLive.polite; prop.children [
            for definition in model.Definitions do
                match estimates |> List.tryFind (fun item -> item.ModelId = definition.Id) with
                | Some estimate -> estimateCard definition estimate dispatch
                | None -> ()
        ] ]
        Html.div [ prop.className "interpretation"; prop.children [
            Html.div [ prop.className "information-icon"; prop.text "i" ]
            Html.div [ Html.strong "Read each endpoint in its own clinical context."; Html.p "Endpoints have different severity and follow-up periods. Estimates are not additive, and curve height alone does not establish a planning priority. “pp” means percentage points." ]
        ] ]
        Html.div [ prop.className "science-strip"; prop.children [
            Html.div [ Html.span (string (model.Definitions |> List.filter (fun m -> m.Availability = Implemented) |> List.length)); Html.small "Executable models" ]
            Html.div [ Html.span "Gy"; Html.small "Explicit dose units" ]
            Html.div [ Html.span "↗"; Html.small "Source-linked equations" ]
        ] ]
    ] ]


/// Presents the current model's exact DVH metric rather than a generic organ-dose description.
let private leanMetricField (id: string) (label: string) (endpoint: string) (color: string) (value: string) change =
    Html.div [ prop.className "dose-row lean-metric-row"; prop.children [
        Html.div [ prop.className "dose-heading"; prop.children [
            Html.span [ prop.className "organ-dot"; prop.style [ style.backgroundColor color ] ]
            Html.label [ prop.htmlFor id; prop.text label ]
        ] ]
        Html.div [ prop.className "number-field dose-number"; prop.children [
            Html.input [ prop.id id; prop.type' "number"; prop.min 0; prop.max 50; prop.step 0.1; prop.value value; prop.onChange (fun (text: string) -> change text) ]
            Html.span "Gy"
        ] ]
        Html.small [ prop.className "dose-endpoint"; prop.text endpoint ]
    ] ]

/// Displays the three currently implemented mean-dose metrics with OAR inclusion controls.
let private focusedInputPane model dispatch =
    let form = model.Form
    Html.aside [ prop.className "focus-input panel"; prop.children [
        Html.span [ prop.className "eyebrow"; prop.text "DVH INPUTS" ]
        Html.div [ prop.className "focus-dose-list"; prop.children [
            leanMetricField "heart-dose" "Heart · Dmean" "Cardiac mortality" "#bc5b47" form.Heart (fun value -> dispatch (SetForm { form with Heart = value }))
            leanMetricField "lung-dose" "Lungs · Dmean" "Radiation pneumonitis" "#287e99" form.Lung (fun value -> dispatch (SetForm { form with Lung = value }))
            leanMetricField "liver-dose" "Liver · Dmean" "Radiation-induced liver disease" "#7c62a6" form.Liver (fun value -> dispatch (SetForm { form with Liver = value }))
        ] ]
        Html.div [ prop.className "focus-oar-select"; prop.children [
            Html.div [ prop.className "available-oars"; prop.role "group"; prop.ariaLabel "Included OARs"; prop.children [
                for id, label in [ "heart", "Heart"; "lungs", "Lungs"; "liver", "Liver" ] do
                    let included = List.contains id form.AvailableOars
                    Html.button [
                        prop.className ("available-oar " + if included then "active" else "")
                        prop.ariaPressed included
                        prop.ariaLabel ((if included then "Exclude " else "Include ") + label)
                        prop.onClick (fun _ -> dispatch (SetForm (toggleAvailableOar id form)))
                        prop.text ((if included then "✓ " else "+ ") + label)
                    ]
            ] ]
        ] ]
    ] ]

/// Formats the model's exact predicted absolute NTCP reduction for one Gy less.
let private marginalRiskText (estimate: EndpointEstimate) =
    if estimate.DoseGy <= 0.0 then "0.00 pp"
    else riskText estimate.ReductionForOneGy + " pp"

/// Displays a model endpoint row with matched dose, NTCP and the finite one-Gy reduction.
let private focusedEstimate (definition: ModelDefinition) (estimate: EndpointEstimate) dispatch =
    Html.div [ prop.className "focus-estimate"; prop.children [
        Html.div [ prop.className "focus-estimate-name"; prop.children [
            Html.span [ prop.className "organ-dot"; prop.style [ style.backgroundColor definition.Color ] ]
            Html.div [
                Html.strong definition.Organ
                Html.span definition.Endpoint
            ]
        ] ]
        Html.span [ prop.className "focus-estimate-dose"; prop.text (sprintf "%.1f Gy" estimate.DoseGy) ]
        Html.strong [ prop.className "focus-estimate-risk"; prop.text (riskText estimate.Probability + "%") ]
        Html.strong [ prop.className "focus-estimate-delta"; prop.text (marginalRiskText estimate) ]
        Html.button [
            prop.className "focus-detail-link"
            prop.ariaLabel ("View published model for " + definition.Endpoint)
            prop.onClick (fun _ -> dispatch (SelectModel definition.Id))
            prop.text "Model ↗"
        ]
    ] ]

/// Places the dose-response chart first, followed by endpoint-specific sensitivity values.
let private focusedResults model dispatch =
    let estimates = model.Evaluation |> Option.map _.Estimates |> Option.defaultValue []
    Html.div [ prop.className "focus-results"; prop.children [
        Html.section [ prop.className "panel focus-chart"; prop.children [
            Html.div [ prop.className "focus-section-title"; prop.children [
                Html.h2 "Dose–response"
                Html.div [ prop.className "segmented plot-range"; prop.role "group"; prop.ariaLabel "Plot range"; prop.children [
                    Html.button [ prop.className (if model.Zoom then "active" else ""); prop.onClick (fun _ -> dispatch (SetZoom true)); prop.text "Adaptive" ]
                    Html.button [ prop.className (if not model.Zoom then "active" else ""); prop.onClick (fun _ -> dispatch (SetZoom false)); prop.text "Full range" ]
                ] ]
            ] ]
            match model.Error, model.Evaluation with
            | Some error, _ -> Html.div [ prop.className "error-message"; prop.role "alert"; prop.text error ]
            | None, Some evaluation when not (List.isEmpty evaluation.Estimates) -> Plot.view model dispatch
            | None, Some _ -> Html.p "No included executable OAR models."
            | None, None -> Html.p "Calculating…"
            Html.div [ prop.className "focus-legend"; prop.role "group"; prop.ariaLabel "Highlight dose-response curve"; prop.children [
                for definition in plottedDefinitions model do
                    let selected = model.FocusedModel = Some definition.Id
                    let estimate = estimates |> List.tryFind (fun e -> e.ModelId = definition.Id)
                    Html.button [
                        prop.className (if selected then "selected" else "")
                        prop.ariaPressed selected
                        prop.onClick (fun _ -> dispatch (FocusModel (if selected then None else Some definition.Id)))
                        prop.children [
                            Html.span [ prop.className "legend-line"; prop.style [ style.backgroundColor definition.Color ] ]
                            Html.text definition.Organ
                            match estimate with
                            | Some item -> Html.span [ prop.className "legend-delta"; prop.text ("−1 Gy: " + marginalRiskText item) ]
                            | None -> Html.none
                        ]
                    ]
            ] ]
        ] ]
        Html.section [ prop.className "panel focus-summary"; prop.children [
            Html.div [ prop.className "focus-section-title"; prop.children [
                Html.h2 "Endpoint estimates"
                Html.span [ prop.className ("calculation-status " + if model.Busy then "busy" else ""); prop.children [
                    Html.span [ prop.className "status-dot" ]
                    Html.text (if model.Busy then "Updating" else "Calculated")
                ] ]
            ] ]
            match model.Error with
            | Some error -> Html.div [ prop.role "alert"; prop.className "error-message"; prop.text error ]
            | None when model.Evaluation.IsNone -> Html.p "Calculating…"
            | None ->
                Html.div [ prop.className "focus-estimates"; prop.ariaLive.polite; prop.children [
                    Html.div [ prop.className "focus-estimate-head"; prop.children [
                        Html.span "Endpoint"
                        Html.span "Dmean"
                        Html.span "NTCP"
                        Html.span "ΔNTCP (−1 Gy)"
                        Html.span ""
                    ] ]
                    for definition in plottedDefinitions model do
                        match estimates |> List.tryFind (fun value -> value.ModelId = definition.Id) with
                        | Some estimate -> focusedEstimate definition estimate dispatch
                        | None -> ()
                ] ]
        ] ]
    ] ]

/// Renders the calculator and model catalogue in the same persistent application shell.
let view model dispatch =
    Html.div [ prop.className "app-shell"; prop.children [
        Html.header [ prop.className "app-header"; prop.children [
            Html.a [ prop.className "brand"; prop.href "#"; prop.onClick (fun event -> event.preventDefault (); dispatch (Navigate Calculator)); prop.children [
                brandMark
                Html.div [ Html.strong "Risk-Based"; Html.span "Radiotherapy Planning" ]
            ] ]
            Html.nav [ prop.className "main-nav"; prop.ariaLabel "Main navigation"; prop.children [
                Html.button [ prop.className (if model.Page = Calculator then "nav-item active" else "nav-item"); prop.onClick (fun _ -> dispatch (Navigate Calculator)); prop.text "Calculator" ]
                Html.button [ prop.className (if model.Page = Models then "nav-item active" else "nav-item"); prop.onClick (fun _ -> dispatch (Navigate Models)); prop.children [
                    Html.text "NTCP models"
                    Html.span [ prop.className "nav-count"; prop.text (string model.Definitions.Length) ]
                ] ]
            ] ]
        ] ]
        Html.main [ prop.className "main-content lean-main"; prop.children [
            if model.Page = Calculator then
                Html.div [ prop.className "focus-layout"; prop.children [
                    focusedInputPane model dispatch
                    focusedResults model dispatch
                ] ]
            else
                ModelCatalogue.view model dispatch
        ] ]
        Html.footer [ prop.className "app-footer"; prop.children [ Html.span "Risk-Based Radiotherapy Planning"; Html.span "F# · SAFE Stack"; Html.span "Research prototype · not validated for clinical decisions" ] ]
    ] ]

/// Starts the example calculator and loads the model catalogue.
let init = State.init

/// Applies explicit user actions and server responses.
let update = State.update
