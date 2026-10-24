module RiskBasedRadiotherapyPlanning.NtcpModels

open Shared
open RiskBasedRadiotherapyPlanning.Domain

/// Represents a published equation with its canonical input and catalogue definition.
type NtcpModel = { Organ: Organ; Response: DoseResponse; Definition: ModelDefinition }

/// Creates metadata for one published coefficient.
let private parameter symbol value unit description =
    { Symbol = symbol; Value = value; Unit = unit; Description = description }

/// Represents the source-linked three-model catalogue.
let all = [
    { Organ = Heart; Response = Linear 0.00105
      Definition = {
        Id = "heart-hancock-brodin"; Organ = "Heart"; OarId = "heart"; Region = "Thorax"; OutputKind = AbsoluteProbability; Availability = Implemented
        Inputs = [ { OarId = "heart"; Kind = MeanDoseGy; Description = "Mean dose (Gy)" } ]; Endpoint = "Cardiac mortality"
        Name = "Hancock / Brodin"; Family = "Linear"; Color = "#bc5b47"; Version = "1.0"
        Description = "The linear cardiac-mortality model used by Brodin et al. for Hodgkin lymphoma plan comparison, derived from the Hancock cohort. Mean heart dose is the predictor."
        Formula = "NTCP(D) = a · D"
        Parameters = [ parameter "a" 0.00105 "Gy⁻¹" "Probability increase per gray of mean heart dose" ]
        RequiredMetric = "Mean heart dose (Gy)"
        Population = "Historical Hodgkin lymphoma cohorts."
        DoseBasis = "Physical dose in Gy; historical conventionally fractionated radiotherapy."
        TimeHorizon = "Source-study follow-up; not a specified individual lifetime probability."
        Limitations = "The derived Brodin implementation, not a contemporary individual cardiovascular-risk model. Age, sex, cardiac risk factors and chemotherapy do not modify this equation."
        Citation = "Brodin et al. IJROBP 2014;88:433–445, Table 1; based on Hancock et al. 1993."
        PaperUrl = "https://pubmed.ncbi.nlm.nih.gov/8411552/"
        ParameterSourceUrl = "https://doi.org/10.1016/j.ijrobp.2013.10.028" } }
    { Organ = Lungs; Response = Logistic (-3.87, 0.126)
      Definition = {
        Id = "lung-quantec-marks"; Organ = "Lungs"; OarId = "lungs"; Region = "Thorax"; OutputKind = AbsoluteProbability; Availability = Implemented
        Inputs = [ { OarId = "lungs"; Kind = MeanDoseGy; Description = "Mean dose (Gy)" } ]; Endpoint = "Radiation pneumonitis"
        Name = "QUANTEC · Marks"; Family = "Logistic"; Color = "#287e99"; Version = "1.0"
        Description = "The published QUANTEC logistic fit relating mean lung dose to symptomatic radiation pneumonitis. Its non-zero intercept is part of the fitted response."
        Formula = "NTCP(D) = 1 / (1 + exp(−(a + b · D)))"
        Parameters = [ parameter "a" -3.87 "—" "Logistic intercept"; parameter "b" 0.126 "Gy⁻¹" "Mean lung dose coefficient" ]
        RequiredMetric = "Mean bilateral lung dose (Gy)"
        Population = "Pooled thoracic radiotherapy cohorts with heterogeneous pneumonitis grading."
        DoseBasis = "Physical dose in Gy; conventional fractionation. This is not an SBRT model."
        TimeHorizon = "Study-defined pneumonitis assessment; not lifetime risk."
        Limitations = "Contour definitions and endpoint grading affect applicability. This equation contains no clinical-factor coefficients. The plot range is a mathematical display range, not a validated treatment range."
        Citation = "Marks et al. Radiation Dose-Volume Effects in the Lung. IJROBP 2010;76:S70–S76."
        PaperUrl = "https://pmc.ncbi.nlm.nih.gov/articles/PMC3576042/"
        ParameterSourceUrl = "https://doi.org/10.1016/j.ijrobp.2009.06.091" } }
    { Organ = Liver; Response = MeanDoseLkb (39.8, 0.12)
      Definition = {
        Id = "liver-quantec-pan"; Organ = "Liver"; OarId = "liver"; Region = "Abdomen"; OutputKind = AbsoluteProbability; Availability = Implemented
        Inputs = [ { OarId = "liver"; Kind = MeanDoseGy; Description = "Mean dose (Gy)" } ]; Endpoint = "Radiation-induced liver disease"
        Name = "QUANTEC · Pan"; Family = "Lyman–Kutcher–Burman"; Color = "#7c62a6"; Version = "1.0"
        Description = "The liver LKB parameter set reproduced in Brodin et al., with n = 1. Effective dose therefore reduces to mean dose. Φ denotes the standard normal cumulative distribution function."
        Formula = "NTCP(D) = Φ((D − TD₅₀) / (m · TD₅₀))"
        Parameters = [ parameter "TD₅₀" 39.8 "Gy" "Dose at 50% complication probability"; parameter "m" 0.12 "—" "Dose-response width"; parameter "n" 1.0 "—" "Volume exponent; n = 1 gives mean dose" ]
        RequiredMetric = "Mean normal liver dose (Gy)"
        Population = "Source liver irradiation cohorts; liver function and contour definitions affect applicability."
        DoseBasis = "Physical dose in Gy; conventionally fractionated RT. This is not an SBRT model."
        TimeHorizon = "Study-defined RILD follow-up; not lifetime risk."
        Limitations = "One published parameter set. No cirrhosis, age, sex or chemotherapy adjustment is implemented. Confirm the normal-liver contour definition before interpreting a case."
        Citation = "Pan et al. QUANTEC, IJROBP 2010;76:S94–S100; parameter set reproduced in Brodin et al. 2014, Table 1."
        PaperUrl = "https://doi.org/10.1016/j.ijrobp.2009.06.092"
        ParameterSourceUrl = "https://doi.org/10.1016/j.ijrobp.2013.10.028" } }
]

/// Returns metadata for the model catalogue.
let definitions () = all |> List.map _.Definition
