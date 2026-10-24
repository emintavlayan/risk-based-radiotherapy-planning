module State

open System
open Elmish
open SAFE
open FsToolkit.ErrorHandling
open Shared

/// Represents an application screen without discarding calculator inputs.
type Page = Calculator | Models

/// Represents editable numeric text without coercing empty fields to zero.
type Form = { Label: string; Age: string; Sex: Sex; Heart: string; Lung: string; Liver: string; CardiacRiskFactors: bool; Chemotherapy: bool; AvailableOars: string list }

/// Represents the client state and the generation of its latest calculation request.
type Model = {
    Page: Page; Form: Form; Definitions: ModelDefinition list; Evaluation: PlanEvaluation option
    Error: string option; Busy: bool; RequestId: int; SelectedModel: string; FocusedModel: string option; Zoom: bool
    CatalogueOar: string option
    CatalogueValues: Map<string, string>; CatalogueEvaluation: CatalogueEvaluation option
    CatalogueError: string option; CatalogueBusy: bool
}

/// Represents explicit user actions and asynchronous server results.
type Msg =
    | SetForm of Form
    | ResetExample
    | Navigate of Page
    | SelectModel of string
    | FocusModel of string option
    | SetZoom of bool
    | FilterCatalogueOar of string option
    | SetCatalogueValue of string * string
    | EvaluateCatalogueModel
    | CatalogueModelEvaluated of string * Result<CatalogueEvaluation, string>
    | ModelsLoaded of Result<ModelDefinition list, string>
    | Calculated of int * Result<PlanEvaluation, string>

/// Represents the remoting client for the server-owned calculation API.
let api = Api.makeProxy<IRiskApi> ()

/// Converts synthetic example values into editable form fields.
let exampleForm () = {
    Label = Example.plan.PlanLabel; Age = "42"; Sex = Example.plan.Sex
    Heart = "18.0"; Lung = "14.0"; Liver = "26.0"; AvailableOars = Example.plan.AvailableOarIds
    CardiacRiskFactors = Example.plan.CardiacRiskFactors; Chemotherapy = Example.plan.Chemotherapy
}

/// Parses a numeric field without accepting missing or non-finite values.
let private number label (text: string) =
    match Double.TryParse text with
    | true, value when not (Double.IsNaN value || Double.IsInfinity value) -> Ok value
    | _ -> Error ("Enter a valid number for " + label + ".")

/// Creates the boundary request while preserving field-specific parse errors.
let input form =
    result {
        let! age = number "age" form.Age
        let! heart = number "mean heart dose" form.Heart
        let! lung = number "mean lung dose" form.Lung
        let! liver = number "mean liver dose" form.Liver
        return {
            PlanLabel = form.Label; AgeAtPlanCreation = age; Sex = form.Sex
            MeanHeartDoseGy = heart; MeanLungDoseGy = lung; MeanLiverDoseGy = liver
            AvailableOarIds = form.AvailableOars
            CardiacRiskFactors = form.CardiacRiskFactors; Chemotherapy = form.Chemotherapy
        }
    }


/// Names a numerical input unambiguously across different organ structures and DVH metrics.
let catalogueInputKey (input: ModelInput) =
    input.OarId + "|" + sprintf "%A" input.Kind

/// Parses a normalised differential DVH from a comma-separated series of doseGy:volumeFraction bins.
let private parseDifferentialDvh label (value: string) =
    result {
        do! (not (String.IsNullOrWhiteSpace value)) |> Result.requireTrue ("Enter a full differential DVH for " + label + ".")
        let entries = value.Split([| ','; '\n'; ';' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList
        let! bins =
            entries |> List.traverseResultM (fun entry ->
                let parts = entry.Trim().Split ':'
                if parts.Length <> 2 then Error ("DVH bin must use doseGy:volumeFraction for " + label + ".")
                else
                    result {
                        let! dose = number "DVH dose in Gy" parts.[0]
                        let! fraction = number "DVH fractional volume" parts.[1]
                        return { DoseGy = dose; VolumeFraction = fraction }
                    })
        return { OarId = label; Bins = bins }
    }

/// Parses a model's exact required scalar inputs and full DVH inputs for the server.
let catalogueInput (definition: ModelDefinition) (values: Map<string, string>) =
    result {
        let read input = Map.tryFind (catalogueInputKey input) values |> Option.defaultValue ""
        let! measurements =
            definition.Inputs
            |> List.filter (fun input -> input.Kind <> DifferentialDvh)
            |> List.traverseResultM (fun input ->
                result {
                    let! value = number input.Description (read input)
                    return { OarId = input.OarId; Kind = input.Kind; Value = value }
                })
        let! histograms =
            definition.Inputs
            |> List.filter (fun input -> input.Kind = DifferentialDvh)
            |> List.traverseResultM (fun input -> parseDifferentialDvh input.OarId (read input))
        return { ModelId = definition.Id; Measurements = measurements; Histograms = histograms }
    }

/// Requests a calculation and attaches a generation to reject stale responses.
let private calculate model =
    let requestId = model.RequestId + 1
    match input model.Form with
    | Error error -> { model with RequestId = requestId; Error = Some error; Evaluation = None; Busy = false }, Cmd.none
    | Ok plan ->
        let command = Cmd.OfAsync.either api.evaluatePlan plan (fun answer -> Calculated (requestId, answer)) (fun _ -> Calculated (requestId, Error "The calculation server could not be reached. Check the connection and try again."))
        { model with RequestId = requestId; Error = None; Busy = true }, command

/// Starts the application with a synthetic example and the server model catalogue.
let init () =
    let model = {
        Page = Calculator; Form = exampleForm (); Definitions = []; Evaluation = None
        Error = None; Busy = true; RequestId = 0; SelectedModel = "heart-hancock-brodin"; FocusedModel = None; Zoom = false; CatalogueOar = None
        CatalogueValues = Map.empty; CatalogueEvaluation = None; CatalogueError = None; CatalogueBusy = false
    }
    let calculated, command = calculate model
    let models = Cmd.OfAsync.either api.getModels () (Ok >> ModelsLoaded) (fun _ -> ModelsLoaded (Error "The model catalogue could not be loaded. Refresh to try again."))
    calculated, Cmd.batch [ command; models ]

/// Updates client state while applying only the latest calculation result.
let update message model =
    match message with
    | SetForm form -> calculate { model with Form = form }
    | ResetExample -> calculate { model with Form = exampleForm () }
    | Navigate page -> { model with Page = page; FocusedModel = None }, Cmd.none
    | SelectModel id -> { model with SelectedModel = id; Page = Models; CatalogueValues = Map.empty; CatalogueEvaluation = None; CatalogueError = None; CatalogueBusy = false }, Cmd.none
    | FocusModel id -> { model with FocusedModel = id }, Cmd.none
    | SetZoom zoom -> { model with Zoom = zoom }, Cmd.none
    | FilterCatalogueOar oar ->
        let selection =
            match oar with
            | Some id -> model.Definitions |> List.tryFind (fun d -> d.OarId = id)
            | None -> model.Definitions |> List.tryHead
        let selectedId = selection |> Option.map _.Id |> Option.defaultValue model.SelectedModel
        { model with CatalogueOar = oar; SelectedModel = selectedId; CatalogueValues = Map.empty; CatalogueEvaluation = None; CatalogueError = None; CatalogueBusy = false }, Cmd.none
    | SetCatalogueValue (key, value) ->
        { model with CatalogueValues = Map.add key value model.CatalogueValues; CatalogueEvaluation = None; CatalogueError = None }, Cmd.none
    | EvaluateCatalogueModel ->
        match model.Definitions |> List.tryFind (fun d -> d.Id = model.SelectedModel) with
        | None -> { model with CatalogueError = Some "Select a catalogue model first." }, Cmd.none
        | Some definition when definition.Availability <> Implemented ->
            { model with CatalogueError = Some "This entry is not executable. See the applicability notes." }, Cmd.none
        | Some definition ->
            match catalogueInput definition model.CatalogueValues with
            | Error error -> { model with CatalogueError = Some error; CatalogueEvaluation = None; CatalogueBusy = false }, Cmd.none
            | Ok request ->
                let command = Cmd.OfAsync.either api.evaluateCatalogueModel request
                                  (fun answer -> CatalogueModelEvaluated (definition.Id, answer))
                                  (fun _ -> CatalogueModelEvaluated (definition.Id, Error "Could not reach the calculation server."))
                { model with CatalogueBusy = true; CatalogueError = None; CatalogueEvaluation = None }, command
    | CatalogueModelEvaluated (id, _) when id <> model.SelectedModel -> model, Cmd.none
    | CatalogueModelEvaluated (_, Ok value) ->
        { model with CatalogueEvaluation = Some value; CatalogueError = None; CatalogueBusy = false }, Cmd.none
    | CatalogueModelEvaluated (_, Error error) ->
        { model with CatalogueEvaluation = None; CatalogueError = Some error; CatalogueBusy = false }, Cmd.none
    | ModelsLoaded (Ok definitions) -> { model with Definitions = definitions }, Cmd.none
    | ModelsLoaded (Error error) -> { model with Error = Some error; Busy = false }, Cmd.none
    | Calculated (id, _) when id <> model.RequestId -> model, Cmd.none
    | Calculated (_, Ok evaluation) -> { model with Evaluation = Some evaluation; Busy = false; Error = None }, Cmd.none
    | Calculated (_, Error error) -> { model with Evaluation = None; Busy = false; Error = Some error }, Cmd.none

/// Formats a probability in percent without concealing small non-zero estimates.
let riskText probability =
    if probability = 0.0 then "0.00"
    elif probability * 100.0 < 0.01 then "<0.01"
    else sprintf "%.2f" (probability * 100.0)

/// Returns definitions whose implementation produced an estimate for this plan.
let plottedDefinitions model =
    let ids =
        model.Evaluation
        |> Option.map (fun value -> value.Estimates |> List.map _.ModelId |> Set.ofList)
        |> Option.defaultValue Set.empty
    model.Definitions |> List.filter (fun definition -> Set.contains definition.Id ids)

/// Returns the canonical organ filter without discarding the all-organ catalogue view.
let catalogueDefinitions model =
    match model.CatalogueOar with
    | None -> model.Definitions
    | Some organ -> model.Definitions |> List.filter (fun definition -> definition.OarId = organ)

/// Selects the published models whose OARs are explicitly available from dosimetry.
let toggleAvailableOar oar form =
    let selected = Set.ofList form.AvailableOars
    let updated =
        if Set.contains oar selected then Set.remove oar selected
        else Set.add oar selected
    { form with AvailableOars = Set.toList updated }
