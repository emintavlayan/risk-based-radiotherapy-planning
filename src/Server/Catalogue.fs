module RiskBasedRadiotherapyPlanning.Catalogue

open Shared
open RiskBasedRadiotherapyPlanning.NtcpModels

/// Represents the canonical paper used to transcribe this catalogue.
let private brodin = "Brodin et al., IJROBP 2014;88:433–445, Table 1 (online 2013)."

/// Represents the canonical paper URL without inventing links to original studies.
let private sourceUrl = "https://doi.org/10.1016/j.ijrobp.2013.10.028"

/// Links each independently verified publication; unknown source links retain the Brodin table provenance.
let private primaryPublication = function
    | "parotid-beetz-2011" | "submandibular-beetz-2011" ->
        "https://www.thegreenjournal.com/article/S0167-8140%2811%2900202-7/fulltext"
    | "thyroid-vogelius-2011" -> "https://doi.org/10.1002/cncr.26186"
    | "thyroid-boomsma-2012" -> "https://pubmed.ncbi.nlm.nih.gov/22717243/"
    | "neck-weakness-vanleeuwen-2012" -> "https://pubmed.ncbi.nlm.nih.gov/21288655/"
    | "larynx-edema-rancati-2009" -> "https://pubmed.ncbi.nlm.nih.gov/19747783/"
    | "larynx-dysphagia-rancati-2010" -> "https://pubmed.ncbi.nlm.nih.gov/20171520/"
    | "esophagus-stricture-alevronta-2010" -> "https://pubmed.ncbi.nlm.nih.gov/20557965/"
    | "esophagus-esophagitis-belderbos-2005" -> "https://pubmed.ncbi.nlm.nih.gov/15890421/"
    | "lung-pneumonitis-huang-2011" -> "https://pmc.ncbi.nlm.nih.gov/articles/PMC4041523/"
    | "kidney-injury-cassady-1995" -> "https://pubmed.ncbi.nlm.nih.gov/7713786/"
    | _ -> sourceUrl

/// Creates a published model coefficient with its reported unit.
let private coefficient symbol value unit description =
    { Symbol = symbol; Value = value; Unit = unit; Description = description }

/// Creates a required DVH mean dose identified independently of Eclipse structure naming.
let private meanDose oar = { OarId = oar; Kind = MeanDoseGy; Description = "Mean organ dose (Gy)" }

/// Creates a required full differential dose-volume histogram for volume-effect models.
let private dvh oar doseBasis =
    let basisText = match doseBasis with | PhysicalGy -> "physical Gy" | EqD2Gy alphaBeta -> sprintf "EQD2 Gy (α/β = %g Gy)" alphaBeta
    { OarId = oar; Kind = DifferentialDvh doseBasis; Description = "Full differential DVH in " + basisText + ", fractional volume" }

/// Creates a required clinical covariate that cannot be extracted from the DVH.
let private clinical name = { OarId = ""; Kind = ClinicalVariable name; Description = name }

/// Creates a required organ volume in cubic centimeters.
let private volume oar = { OarId = oar; Kind = VolumeCm3; Description = "Contoured organ volume (cm³)" }

/// Creates a required heart dose-at-volume statistic from a cumulative DVH.
let private heartD10 = { OarId = "heart"; Kind = DoseAtVolumePercent 10.0; Description = "Heart D10% (Gy)" }

/// Creates an age- or population-dependent baseline incidence requirement.
let private baseline = { OarId = ""; Kind = BaselineIncidence; Description = "Population baseline incidence at the relevant age and follow-up" }

/// Maps an OAR identifier to its display name, anatomic region and consistent colour.
let private oarInfo id =
    match id with
    | "parotids" -> "Parotid glands", "Head and neck", "#8a6b9a"
    | "submandibular" -> "Submandibular glands", "Head and neck", "#6d8a70"
    | "carotids" -> "Carotid arteries", "Head and neck", "#ae675d"
    | "thyroid" -> "Thyroid", "Head and neck", "#867e5c"
    | "neck-muscles" -> "Neck muscles", "Head and neck", "#8193a6"
    | "larynx" -> "Larynx", "Head and neck", "#65829c"
    | "esophagus" -> "Esophagus", "Thorax", "#9a7855"
    | "lungs" -> "Lungs", "Thorax", "#287e99"
    | "breast" -> "Breast", "Thorax", "#ae7396"
    | "heart" -> "Heart", "Thorax", "#bc5b47"
    | "stomach" -> "Stomach / ventricle", "Abdomen", "#a17f5e"
    | "liver" -> "Liver", "Abdomen", "#7c62a6"
    | "spleen" -> "Spleen", "Abdomen", "#667f93"
    | "pancreas" -> "Pancreas", "Abdomen", "#8e8382"
    | "small-bowel" -> "Small bowel", "Abdomen", "#839565"
    | "colon" -> "Colon", "Abdomen", "#897969"
    | "kidneys" -> "Kidneys", "Abdomen", "#51878d"
    | _ -> "Unknown OAR", "Unspecified", "#71818a"

/// Creates a traceable catalogue entry without implying that it is executable.
let private entry id organId endpoint study family formula parameters inputs kind availability limitations =
    let organ, region, color = oarInfo organId
    { Id = id; Organ = organ; OarId = organId; Region = region
      Endpoint = endpoint; Name = study; Family = family; Color = color; Version = "1.0"
      OutputKind = kind; Availability = availability; Inputs = inputs
      Description = "Endpoint and model reported by " + study + "; included in the Hodgkin lymphoma literature review."
      Formula = formula; Parameters = parameters
      RequiredMetric =
          if List.isEmpty inputs then "No dose-response input established."
          else inputs |> List.map _.Description |> String.concat "; "
      Population = "Original study population; consult the cited paper before clinical use."
      DoseBasis = "Published dose basis; fractionation, laterality and contour requirements must be checked."
      TimeHorizon = "As defined in the original endpoint study; see Brodin Table 2."
      Limitations = limitations
      Citation = study + "; transcribed from " + brodin
      PaperUrl = primaryPublication id; ParameterSourceUrl = sourceUrl }

/// Represents every Table 1 entry not already implemented in the original prototype.
let private additional = [
    entry "parotid-moiseenko-2012" "parotids" "Xerostomia" "Moiseenko et al. (2012)"
        "LKB" "NTCP = Φ((D_eff − TD₅₀)/(m · TD₅₀))"
        [ coefficient "TD₅₀" 39.9 "Gy" "37.3–42.8 Gy"; coefficient "m" 0.40 "—" "0.34–0.51" ]
        [ meanDose "parotids" ] AbsoluteProbability MissingParameters
        "Table 1 does not specify the volume exponent or complete effective-dose definition. Confirm the spared-gland metric in the source before implementing."
    entry "parotid-houweling-2010" "parotids" "Xerostomia" "Houweling et al. (2010)"
        "Logistic" "NTCP = 1/(1 + exp(−4γ₅₀(D/D₅₀ − 1)))"
        [ coefficient "D₅₀" 32.4 "Gy" "26.3–39.9 Gy"; coefficient "γ₅₀" 0.97 "—" "0.58–1.53" ]
        [ { OarId = "parotids"; Kind = SparedGlandMeanDoseGy; Description = "Mean dose of least-irradiated parotid gland (Gy)" } ] AbsoluteProbability RequiresImplementation
        "Study endpoint is 1-year grade 4 xerostomia assessed by salivary flow; least-irradiated parotid is required."
    entry "parotid-beetz-2011" "parotids" "Xerostomia" "Beetz et al. (2011)"
        "Multivariable logistic" "NTCP = logistic(−5.27 + 0.066 D_parotid + 0.050 age + 0.916 baseline)"
        [ coefficient "intercept" -5.27 "—" ""; coefficient "dose" 0.066 "Gy⁻¹" ""; coefficient "age" 0.050 "year⁻¹" ""; coefficient "baseline" 0.916 "—" "" ]
        [ meanDose "parotids"; clinical "age at treatment"; clinical "baseline xerostomia score" ]
        AbsoluteProbability RequiresImplementation "Requires the exact baseline score coding and parotid metric specified in the study."
    entry "submandibular-beetz-2011" "submandibular" "Sticky saliva" "Beetz et al. (2011)"
        "Multivariable logistic" "NTCP = logistic(−10.70 + 0.091 D_SM + 0.107 age + 1.218 baseline − 0.041 D_SL)"
        [ coefficient "intercept" -10.70 "—" ""; coefficient "D_SM" 0.091 "Gy⁻¹" ""; coefficient "age" 0.107 "year⁻¹" ""; coefficient "baseline" 1.218 "—" ""; coefficient "D_SL" -0.041 "Gy⁻¹" "" ]
        [ meanDose "submandibular"; meanDose "sublingual"; clinical "age at treatment"; clinical "baseline sticky-saliva score" ]
        AbsoluteProbability RequiresImplementation "Requires sublingual structure mapping and the exact baseline score coding."
    entry "carotid-bowers-2005" "carotids" "Stroke" "Bowers et al. (2005) / Maraldo et al. (2013)"
        "Linear excess incidence" "Incidence = baseline + a_children · mean common-carotid dose"
        [] [ meanDose "carotids"; baseline; clinical "age at exposure / pediatric cohort" ]
        BaselineAdjustedIncidence MissingParameters "The pediatric slope and baseline incidence are not given in Table 1."
    entry "carotid-debruin-2009" "carotids" "Stroke" "de Bruin et al. (2009) / Maraldo et al. (2013)"
        "Linear excess incidence" "Incidence = baseline + a_adults · mean common-carotid dose"
        [] [ meanDose "carotids"; baseline; clinical "age at exposure / adult cohort" ]
        BaselineAdjustedIncidence MissingParameters "The adult slope and baseline incidence are not given in Table 1."
    entry "thyroid-bhatti-2010" "thyroid" "Thyroid cancer" "Bhatti et al. (2010)"
        "Linear–exponential–quadratic" "Dose-risk relationship with β₁ and β₂; see original model"
        [ coefficient "β₁" 1.381 "source-defined" ""; coefficient "β₂" -0.00164 "source-defined" "" ]
        [ meanDose "thyroid"; clinical "original risk scale and covariates" ]
        NoRiskEstimate MissingParameters "Table 1 does not specify the full function, risk scale or baseline conversion."
    entry "thyroid-vogelius-2011" "thyroid" "Hypothyroidism" "Vogelius et al. (2011)"
        "Logistic" "NTCP = 1/(1 + exp(−4γ₅₀(D/D₅₀ − 1)))"
        [ coefficient "γ₅₀" 0.58 "—" "0.35–0.81"; coefficient "D₅₀" 33.6 "Gy" "28.9–38.3 Gy" ]
        [ meanDose "thyroid" ] AbsoluteProbability RequiresImplementation
        "Fitted from historical Hodgkin lymphoma cohorts; check population and endpoint timing."
    entry "thyroid-boomsma-2012" "thyroid" "Hypothyroidism" "Boomsma et al. (2012)"
        "Multivariable logistic" "NTCP = logistic(0.011 + 0.062 D_thyroid − 0.19 V_thyroid)"
        [ coefficient "intercept" 0.011 "—" ""; coefficient "dose" 0.062 "Gy⁻¹" ""; coefficient "volume" -0.19 "source-defined" "" ]
        [ meanDose "thyroid"; volume "thyroid" ] AbsoluteProbability RequiresImplementation
        "Verify thyroid volume units and model contour requirements from the original publication."
    entry "neck-weakness-vanleeuwen-2012" "neck-muscles" "Neck muscle weakness" "van Leeuwen-Segarceanu et al. (2012)"
        "Logistic" "NTCP = 1/(1 + exp(−4γ₅₀(D/D₅₀ − 1)))"
        [ coefficient "γ₅₀" 0.56 "—" "0.31–0.81"; coefficient "D₅₀" 27.2 "Gy" "14.8–39.6 Gy" ]
        [ meanDose "neck-muscles" ] AbsoluteProbability RequiresImplementation
        "Derived from latency 10–19 years; neck muscle ROI definition needs confirmation."
    entry "larynx-edema-rancati-2009" "larynx" "Laryngeal edema" "Rancati et al. (2009)"
        "LKB" "NTCP = Φ((EUD_n − TD₅₀)/(m · TD₅₀))"
        [ coefficient "n" 1.17 "—" "±0.6"; coefficient "m" 0.23 "—" "±0.07"; coefficient "TD₅₀" 47.3 "Gy" "±2.1 Gy" ]
        [ dvh "larynx" (EqD2Gy 3.0) ] AbsoluteProbability RequiresImplementation
        "Requires a complete larynx DVH and source definition of endpoint and effective dose."
    entry "larynx-dysphagia-rancati-2010" "larynx" "Dysphagia" "Rancati / QUANTEC (2010)"
        "Logit" "NTCP = 1/(1 + (D₅₀/D)^k)"
        [ coefficient "D₅₀" 57.5 "Gy" ""; coefficient "k" 6.57 "Gy" "" ]
        [ meanDose "supraglottic-larynx" ] AbsoluteProbability RequiresImplementation
        "Aspiration response against mean supraglottic larynx dose; logit model uses dose-ratio exponent k."
    entry "esophagus-stricture-alevronta-2010" "esophagus" "Esophageal stricture" "Alevronta et al. (2010)"
        "Relative seriality" "Relative-seriality model (γ, s, D₅₀); complete DVH required"
        [ coefficient "γ" 1.4 "—" "68% CI 0.8–2.6"; coefficient "s" 0.1 "—" "68% CI 0.01–0.3"; coefficient "D₅₀" 61.5 "Gy" "68% CI 52.9–84.9 Gy" ]
        [ dvh "esophagus" (EqD2Gy 10.0) ] AbsoluteProbability RequiresImplementation
        "Relative seriality requires full voxel-volume discretization and original study normalization."
    entry "esophagus-esophagitis-belderbos-2005" "esophagus" "Esophagitis" "Belderbos et al. (2005)"
        "EUD–LKB" "NTCP = Φ((EUD_n − TD₅₀)/(m · TD₅₀))"
        [ coefficient "n" 0.69 "—" "0.18–6.3"; coefficient "m" 0.36 "—" "0.25–0.55"; coefficient "TD₅₀" 47.0 "Gy" "41–60 Gy" ]
        [ dvh "esophagus" ] AbsoluteProbability RequiresImplementation
        "Requires full DVH and precise EUD convention used in the study."
    entry "esophagus-cancer-morton-2012" "esophagus" "Esophageal cancer" "Morton et al. (2012)"
        "Linear odds ratio" "OR = 1 + 0.09 · mean esophageal dose"
        [ coefficient "slope" 0.09 "Gy⁻¹" "95% CI 0.04–0.16" ]
        [ meanDose "esophagus" ] OddsRatio RequiresImplementation
        "Odds ratio is not an absolute cancer probability; requires baseline odds to convert."
    entry "lung-pneumonitis-huang-2011" "lungs" "Radiation pneumonitis" "Huang et al. (2011)"
        "Multivariable logistic" "NTCP = logistic(0.0234 · D10_heart + 0.0649 · mean lung dose − 3.5)"
        [ coefficient "D10_heart" 0.0234 "Gy⁻¹" ""; coefficient "lung mean" 0.0649 "Gy⁻¹" ""; coefficient "intercept" -3.5 "—" "" ]
        [ heartD10; meanDose "lungs" ] AbsoluteProbability RequiresImplementation
        "Requires the heart D10% DVH metric; check endpoint classification and original population."
    entry "lung-cancer-travis-2002" "lungs" "Lung cancer" "Travis et al. (2002) / Brodin et al. (2011)"
        "Linear relative risk" "RR = 1 + 0.141 · mean lung dose"
        [ coefficient "slope" 0.141 "Gy⁻¹" "95% CI 0.05–0.75" ]
        [ meanDose "lungs" ] RelativeRisk RequiresImplementation
        "RR is not an absolute cancer probability; risk conversion needs baseline age- and sex-specific incidence."
    entry "breast-cancer-travis-2003" "breast" "Breast cancer" "Travis et al. (2003) / Brodin et al. (2011)"
        "Linear relative risk" "RR = 1 + 0.149 · mean breast dose"
        [ coefficient "slope" 0.149 "Gy⁻¹" "95% CI 0.04–0.73" ]
        [ meanDose "breast" ] RelativeRisk RequiresImplementation
        "Breast dose laterality and baseline cancer incidence require explicit definition."
    entry "heart-valvular-schellong-2010" "heart" "Valvular disease" "Schellong et al. (2010) / Maraldo et al. (2012)"
        "Logistic" "NTCP = 1/(1 + exp(−4γ₅₀(D/D₅₀ − 1)))"
        [ coefficient "γ₅₀" 4.01 "—" "95% CI 0.275–7.75"; coefficient "D₅₀" 40.1 "Gy" "95% CI 34.4–45.9" ]
        [ meanDose "heart" ] AbsoluteProbability RequiresImplementation
        "Historical survivors and long latency; check time horizon and valvular endpoint definition."
    entry "heart-morbidity-schellong-2010" "heart" "Cardiac morbidity" "Schellong et al. (2010) / Maraldo et al. (2012)"
        "Logistic" "NTCP = 1/(1 + exp(−4γ₅₀(D/D₅₀ − 1)))"
        [ coefficient "γ₅₀" 1.36 "—" "95% CI 0.78–1.94"; coefficient "D₅₀" 48.2 "Gy" "95% CI 38.2–58.2" ]
        [ meanDose "heart" ] AbsoluteProbability RequiresImplementation
        "Historical survivors and long latency; check cardiac endpoint coding."
    entry "stomach-cancer-vandenbelt-2009" "stomach" "Stomach cancer" "van den Belt-Dusebout et al. (2009)"
        "Linear relative risk" "RR = 1 + 0.84 · mean stomach dose"
        [ coefficient "slope" 0.84 "Gy⁻¹" "95% CI 0.12–15.6" ]
        [ meanDose "stomach" ] RelativeRisk RequiresImplementation
        "Study table uses 'ventricle' for stomach; RR requires baseline incidence to obtain absolute probability."
    entry "spleen-toxicity-no-data" "spleen" "Spleen toxicity" "No dose-response data in Brodin Table 1"
        "Not available" "No published NTCP relationship included" [] [] NoRiskEstimate NoDoseResponse
        "The paper explicitly reports no NTCP data for this endpoint."
    entry "pancreas-cancer-hodgson-2007" "pancreas" "Pancreatic cancer" "Hodgson et al. (2007)"
        "Cohort relative risk only" "Reported cohort RR = 4.7; no dose-response equation"
        [ coefficient "cohort RR" 4.7 "—" "95% CI 2.9–7.1" ]
        [] RelativeRisk NoDoseResponse "Cohort relative risk is not a function of the individual treatment plan's dose."
    entry "small-bowel-toxicity-eich-2005" "small-bowel" "Gastrointestinal toxicity" "Eich et al. (2005)"
        "LKB" "NTCP = Φ((EUD_n − TD₅₀)/(m · TD₅₀))"
        [ coefficient "m" 0.17 "—" ""; coefficient "n" 0.09 "—" ""; coefficient "TD₅₀" 32.0 "Gy" "" ]
        [ dvh "small-bowel" PhysicalGy ] AbsoluteProbability RequiresImplementation
        "Requires complete small-bowel DVH with consistent anatomy and volume-effect calculation."
    entry "colon-cancer-hodgson-2007" "colon" "Colon cancer" "Hodgson et al. (2007)"
        "Cohort relative risk only" "Reported cohort RR = 4.3; no dose-response equation"
        [ coefficient "cohort RR" 4.3 "—" "95% CI 3.2–5.6" ]
        [] RelativeRisk NoDoseResponse "Cohort relative risk is not a dose-response equation for plan-specific estimation."
    entry "kidney-injury-cassady-1995" "kidneys" "Symptomatic kidney injury" "Cassady et al. (1995) / Kornerup (2012)"
        "Logistic" "NTCP = 1/(1 + exp(−4γ₅₀(D/D₅₀ − 1)))"
        [ coefficient "γ₅₀" 1.924 "—" "95% CI 1.65–2.20"; coefficient "D₅₀" 27.84 "Gy" "95% CI 23.9–31.8" ]
        [ meanDose "kidneys" ] AbsoluteProbability RequiresImplementation
        "Confirm bilateral/individual kidney dose metric and late-effect definition in source."
]

/// Returns the complete Table 1 inventory, including the original implemented models.
let definitions () =
    let included = NtcpModels.all |> List.map _.Definition
    included @ (additional |> List.map (fun model ->
        if CatalogueCalculations.executableIds.Contains model.Id then { model with Availability = Implemented }
        else model))

/// Returns the distinct canonical OAR identifiers used for catalogue filtering.
let organs () =
    definitions ()
    |> List.map (fun model -> model.OarId, model.Organ)
    |> List.distinct
    |> List.sortBy snd

/// Finds catalogue entries belonging to any of the supplied canonical OARs.
let forOrgans oarIds =
    let selected = Set.ofList oarIds
    definitions () |> List.filter (fun definition -> Set.contains definition.OarId selected)
