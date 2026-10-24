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
    | SelectModel id -> { model with SelectedModel = id; Page = Models }, Cmd.none
    | FocusModel id -> { model with FocusedModel = id }, Cmd.none
    | SetZoom zoom -> { model with Zoom = zoom }, Cmd.none
    | FilterCatalogueOar oar ->
        let selection =
            match oar with
            | Some id -> model.Definitions |> List.tryFind (fun d -> d.OarId = id)
            | None -> model.Definitions |> List.tryHead
        let selectedId = selection |> Option.map _.Id |> Option.defaultValue model.SelectedModel
        { model with CatalogueOar = oar; SelectedModel = selectedId }, Cmd.none
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
