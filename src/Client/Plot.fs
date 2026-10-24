module Plot

open Feliz
open Shared
open State

/// Maps mean organ dose to the scientific plot's horizontal coordinate.
let private x maxDose dose = 76.0 + dose / maxDose * 770.0

/// Maps a probability to the scientific plot's vertical coordinate.
let private y maxRisk probability = 360.0 - probability / maxRisk * 320.0

/// Builds an SVG path from the points returned by the calculation server.
let private curvePath maxDose maxRisk (points: CurvePoint list) =
    points |> List.mapi (fun i p -> sprintf "%s%.3f,%.3f" (if i = 0 then "M" else "L") (x maxDose p.DoseGy) (y maxRisk p.Probability)) |> String.concat " "

/// Draws scientific axes, published response curves and exact plan markers.
let view model dispatch =
    let maxDose, maxRisk = if model.Zoom then 30.0, 0.3 else 50.0, 1.0
    let estimates = model.Evaluation |> Option.map _.Estimates |> Option.defaultValue []
    Html.div [
        prop.className "plot-wrap"
        prop.children [
            Svg.svg [
                svg.viewBox (0, 0, 900, 420)
                svg.custom ("role", "img")
                svg.custom ("aria-label", "Published NTCP dose-response curves. Filled circles mark the current plan's mean organ doses and endpoint probabilities.")
                svg.children [
                    Svg.title "Published NTCP dose-response curves and current plan markers"
                    Svg.defs [ svg.children [ Svg.clipPath [ svg.id "risk-plot-clip"; svg.children [ Svg.rect [ svg.x 76; svg.y 40; svg.width 770; svg.height 320 ] ] ] ] ]
                    for i in 0 .. 5 do
                        let probability = float i * maxRisk / 5.0
                        let ordinate = y maxRisk probability
                        Svg.line [ svg.x1 76; svg.x2 846; svg.y1 ordinate; svg.y2 ordinate; svg.stroke "#e3e9e8"; svg.strokeDasharray [| 3; 5 |] ]
                        Svg.text [ svg.x 61; svg.y (ordinate + 4.0); svg.textAnchor.endOfText; svg.className "axis-tick"; svg.text (sprintf "%.0f" (probability * 100.0)) ]
                    for i in 0 .. 5 do
                        let dose = float i * maxDose / 5.0
                        let abscissa = x maxDose dose
                        Svg.line [ svg.x1 abscissa; svg.x2 abscissa; svg.y1 40; svg.y2 360; svg.stroke "#eef1f0" ]
                        Svg.text [ svg.x abscissa; svg.y 383; svg.textAnchor.middle; svg.className "axis-tick"; svg.text (sprintf "%.0f" dose) ]
                    Svg.line [ svg.x1 76; svg.x2 846; svg.y1 360; svg.y2 360; svg.stroke "#aabbbd" ]
                    Svg.text [ svg.x 460; svg.y 410; svg.textAnchor.middle; svg.className "axis-label"; svg.text "Mean organ dose (Gy)" ]
                    Svg.text [ svg.custom ("transform", "translate(20 200) rotate(-90)"); svg.textAnchor.middle; svg.className "axis-label"; svg.text "Endpoint probability (%)" ]
                    Svg.g [
                        svg.clipPath "url(#risk-plot-clip)"
                        svg.children [
                            for definition in plottedDefinitions model do
                                match estimates |> List.tryFind (fun estimate -> estimate.ModelId = definition.Id) with
                                | None -> ()
                                | Some estimate ->
                                    let focused = model.FocusedModel.IsNone || model.FocusedModel = Some definition.Id
                                    Svg.g [
                                        svg.custom ("opacity", if focused then 1.0 else 0.18)
                                        svg.children [
                                            Svg.path [ svg.d (curvePath maxDose maxRisk estimate.Curve); svg.fill "none"; svg.stroke definition.Color; svg.strokeWidth 2.8; svg.strokeLineCap "round" ]
                                            Svg.line [ svg.x1 (x maxDose estimate.DoseGy); svg.x2 (x maxDose estimate.DoseGy); svg.y1 (y maxRisk estimate.Probability); svg.y2 360; svg.stroke definition.Color; svg.strokeWidth 1; svg.strokeDasharray [| 3; 5 |]; svg.custom ("opacity", 0.45) ]
                                            Svg.circle [ svg.cx (x maxDose estimate.DoseGy); svg.cy (y maxRisk estimate.Probability); svg.r 10; svg.fill definition.Color; svg.custom ("opacity", 0.12) ]
                                            Svg.circle [
                                                svg.cx (x maxDose estimate.DoseGy); svg.cy (y maxRisk estimate.Probability); svg.r 5
                                                svg.fill definition.Color; svg.stroke "white"; svg.strokeWidth 2
                                                svg.children [ Svg.title (sprintf "%s: %.1f Gy · %s%%" definition.Endpoint estimate.DoseGy (riskText estimate.Probability)) ]
                                            ]
                                        ]
                                    ]
                        ]
                    ]
                ]
            ]
            if model.Zoom then Html.p [ prop.className "zoom-note"; prop.text "Detail view: 0–30 Gy and 0–30%. Values outside this window are clipped; the estimates below remain complete." ]
        ]
    ]
