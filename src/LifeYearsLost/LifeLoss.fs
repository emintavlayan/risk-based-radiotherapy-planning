namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents the two distinct event pathways in Equations 2 and 4.
type ConsequenceEvidence =
    | IncidentEvent of
        baselineIncidence: IncidenceHazard *
        populationLife: RemainingLifeAtAge *
        postEventLife: RemainingLifeAtAge
    | FatalHeartFailure of baselineCauseSpecificMortality: MortalityHazard * populationLife: RemainingLifeAtAge

/// Represents externally supplied age-specific evidence; survival must use the declared starting cohort.
type AgeSpecificEvidence = {
    Endpoint: Endpoint
    Sex: Sex
    Age: AttainedAge
    SurvivorSurvival: SurvivalProbability
    Consequence: ConsequenceEvidence
}

/// Represents the Equation 2 or 4 integrand, in years lost per year of attained age (dimensionless).
type LifeLossDensity = private LifeLossDensity of float<year / year>

/// Represents auditable intermediate values at one attained age.
type AgeSpecificLoss = {
    Exposure: ExposureAge
    Sex: Sex
    Endpoint: Endpoint
    SurvivorSurvival: SurvivalProbability
    Age: AttainedAge
    ExcessRatio: ExcessHazardRatio
    ExcessEventHazard: ExcessEventHazard
    SurvivalWeightedExcessRate: AnnualRate
    LifeShortening: float<year>
    Density: LifeLossDensity
}

/// Represents the explicitly selected integration bounds; no hidden latency assumption.
type IntegrationWindow = private {
    Exposure: ExposureAge
    Start: AttainedAge
    Finish: AttainedAge
}

/// Represents a survival integral over a finite horizon, distinct from full remaining life.
type RestrictedMeanLife = private {
    Horizon: float<year>
    TerminalSurvival: SurvivalProbability
    Area: float<year>
}

[<RequireQualifiedAccess>]
module RestrictedMeanLife =
    /// Returns the finite elapsed-time horizon of the survival integral.
    let horizon input =
        input.Horizon

    /// Returns terminal survival, identifying whether an unintegrated tail remains.
    let terminalSurvival input =
        input.TerminalSurvival

    /// Returns whether survival beyond the finite horizon remains unaccounted for.
    let hasUnintegratedTail input =
        SurvivalProbability.value input.TerminalSurvival > 0.0

    /// Returns the restricted mean in years without converting it to full remaining life.
    let value input =
        input.Area

[<RequireQualifiedAccess>]
module LifeLossDensity =
    /// Returns years lost per year of attained age, not a finite-bin loss in years.
    let value (LifeLossDensity value) =
        value

[<RequireQualifiedAccess>]
module IntegrationWindow =
    /// Creates finite bounds from exposure age, a chosen latency, and an upper attained age.
    let create exposure latency upperAge =
        result {
            let! latency = Validation.nonNegative "Latency" latency

            let! start =
                AttainedAge.create (
                    ExposureAge.value exposure
                    + latency
                )

            do!
                Result.requireTrue
                    "Upper attained age must exceed the integration start."
                    (AttainedAge.value upperAge > AttainedAge.value start)

            return {
                Exposure = exposure
                Start = start
                Finish = upperAge
            }
        }

    /// Reproduces the printed Equation 3 bounds e+1 through 110 years without claiming a biological latency.
    let printedPaperBounds exposure =
        result {
            let! upper = AttainedAge.create 110.0<year>
            return! create exposure 1.0<year> upper
        }

    /// Returns the explicit lower and upper attained ages.
    let bounds window =
        window.Start, window.Finish

[<RequireQualifiedAccess>]
module LifeLoss =
    /// Calculates an auditable age-specific integrand with matching endpoint, sex and event pathway.
    let atAge exposure sex endpoint excessRatio (evidence: AgeSpecificEvidence) =
        result {
            do! Result.requireTrue "Requested and population endpoint must match." (endpoint = evidence.Endpoint)
            do! Result.requireTrue "Patient and population sex must match." (sex = evidence.Sex)

            do!
                Result.requireTrue
                    "Attained age must not precede exposure."
                    (AttainedAge.value evidence.Age
                     >= ExposureAge.value exposure)

            let ratio = excessRatio

            let! excess, shortening =
                match evidence.Endpoint, evidence.Consequence with
                | Linear HeartFailure, FatalHeartFailure(mortality, populationLife) ->
                    result {
                        do!
                            Result.requireTrue
                                "Population life expectancy must be conditional on evidence.Age."
                                (RemainingLifeAtAge.age populationLife = evidence.Age)

                        let! excess = ExcessMortalityHazard.fromBaseline ratio mortality
                        return ExcessMortality excess, RemainingLifeAtAge.value populationLife
                    }
                | Linear HeartFailure, _ -> Error "Heart failure requires cause-specific mortality for Equation 4."
                | _, FatalHeartFailure _ -> Error "The Equation 4 mortality pathway is restricted to heart failure."
                | _, IncidentEvent(incidence, populationLife, postEventLife) ->
                    result {
                        do!
                            Result.requireTrue
                                "Population life expectancy must be conditional on evidence.Age."
                                (RemainingLifeAtAge.age populationLife = evidence.Age)

                        do!
                            Result.requireTrue
                                "Post-event life expectancy must be conditional on evidence.Age."
                                (RemainingLifeAtAge.age postEventLife = evidence.Age)

                        let difference =
                            RemainingLifeAtAge.value populationLife
                            - RemainingLifeAtAge.value postEventLife

                        let! difference =
                            Validation.nonNegative "Population minus post-event life expectancy" difference

                        let! excess = ExcessIncidenceHazard.fromBaseline ratio incidence
                        return ExcessIncidence excess, difference
                    }

            let! weighted =
                AnnualRate.create (
                    SurvivalProbability.value evidence.SurvivorSurvival
                    * ExcessEventHazard.value excess
                )

            let! density =
                Validation.nonNegative
                    "Life loss density"
                    (AnnualRate.value weighted
                     * shortening)

            return {
                Exposure = exposure
                Sex = sex
                Endpoint = endpoint
                SurvivorSurvival = evidence.SurvivorSurvival
                Age = evidence.Age
                ExcessRatio = ratio
                ExcessEventHazard = excess
                SurvivalWeightedExcessRate = weighted
                LifeShortening = shortening
                Density = LifeLossDensity density
            }
        }

    /// Integrates a complete ordered grid by trapezoids without extrapolating or dropping failed samples.
    let integrate window (samples: AgeSpecificLoss list) =
        result {
            do!
                Result.requireTrue
                    "Integration requires at least two age samples."
                    (samples.Length
                     >= 2)

            do!
                Result.requireTrue
                    "Samples must cover both integration bounds exactly."
                    (samples.Head.Age = window.Start
                     && (List.last samples).Age = window.Finish)

            do! Result.requireTrue "Samples must use the window exposure age." (samples.Head.Exposure = window.Exposure)

            /// Checks that integration samples belong to the same calculation.
            let sameContext (sample: AgeSpecificLoss) =
                sample.Exposure = samples.Head.Exposure
                && sample.Sex = samples.Head.Sex
                && sample.Endpoint = samples.Head.Endpoint

            do!
                Result.requireTrue
                    "Samples must use one exposure context, sex and endpoint."
                    (samples
                     |> List.forall sameContext)

            let! intervals =
                samples
                |> List.pairwise
                |> List.traverseResultM (fun (left, right) ->
                    result {
                        do!
                            Result.requireTrue
                                "Survivor survival must not increase with attained age."
                                (SurvivalProbability.value right.SurvivorSurvival
                                 <= SurvivalProbability.value left.SurvivorSurvival)

                        let width =
                            AttainedAge.value right.Age
                            - AttainedAge.value left.Age

                        do! Result.requireTrue "Attained ages must be strictly increasing." (width > 0.0<year>)

                        return
                            0.5
                            * (LifeLossDensity.value left.Density
                               + LifeLossDensity.value right.Density)
                            * width
                    })

            return!
                intervals
                |> List.sum
                |> ExpectedLifeYearsLost.create
        }

    /// Integrates conditional survival to its supplied finite horizon and preserves terminal survival explicitly.
    let restrictedMeanFromSurvival (points: (float<year> * SurvivalProbability) list) =
        result {
            do!
                Result.requireTrue
                    "A survival curve needs at least two points."
                    (points.Length
                     >= 2)

            do!
                Result.requireTrue
                    "Conditional survival must start at elapsed time zero with probability one."
                    (fst points.Head = 0.0<year>
                     && SurvivalProbability.value (snd points.Head) = 1.0)

            let! areas =
                points
                |> List.pairwise
                |> List.traverseResultM (fun ((t0, s0), (t1, s1)) ->
                    result {
                        let width =
                            t1
                            - t0

                        let! _ = Validation.nonNegative "Survival time" t1
                        do! Result.requireTrue "Survival times must strictly increase." (width > 0.0<year>)

                        do!
                            Result.requireTrue
                                "Survival probabilities must not increase."
                                (SurvivalProbability.value s1
                                 <= SurvivalProbability.value s0)

                        return
                            0.5
                            * (SurvivalProbability.value s0
                               + SurvivalProbability.value s1)
                            * width
                    })

            let! area =
                areas
                |> List.sum
                |> Validation.nonNegative "Restricted mean survival"

            return {
                Horizon = fst (List.last points)
                TerminalSurvival = snd (List.last points)
                Area = area
            }
        }

    /// Integrates full conditional remaining life only when the supplied survival tail reaches zero.
    let remainingLifeFromSurvival points =
        result {
            let! restricted = restrictedMeanFromSurvival points

            do!
                Result.requireTrue
                    "Full remaining life requires terminal survival zero. Use restrictedMeanFromSurvival for a finite horizon with an unintegrated tail."
                    (not (RestrictedMeanLife.hasUnintegratedTail restricted))

            return! RemainingLife.create (RestrictedMeanLife.value restricted)
        }