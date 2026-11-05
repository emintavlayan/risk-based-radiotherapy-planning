namespace LifeYearsLost

open FsToolkit.ErrorHandling

/// Represents one differential DVH bin with a fractional volume.
type FractionalDoseBin = { Dose: AbsorbedDose; Fraction: float }

/// Represents one differential DVH bin with an absolute volume.
type AbsoluteDoseBin = {
    Dose: AbsorbedDose
    Volume: float<cm3>
}

/// Represents a validated differential histogram covering the complete organ.
type DifferentialDvh = private DifferentialDvh of FractionalDoseBin list

[<RequireQualifiedAccess>]
module DifferentialDvh =
    /// Validates fractional bins without silently normalizing incomplete or cumulative histograms.
    let ofFractions (bins: FractionalDoseBin list) =
        result {
            do! Result.requireTrue "A differential DVH needs at least one bin." (not bins.IsEmpty)

            let! _ =
                bins
                |> List.traverseResultM (fun bin -> Validation.fraction "DVH fraction" bin.Fraction)

            let total =
                bins
                |> List.sumBy (fun bin -> bin.Fraction)

            do!
                Result.requireTrue
                    "Differential DVH fractions must sum to one."
                    (abs (
                        total
                        - 1.0
                     )
                     <= 1e-9)

            return DifferentialDvh bins
        }

    /// Converts complete absolute-volume bins to fractions using their summed volume exactly once.
    let ofAbsoluteVolumes (bins: AbsoluteDoseBin list) =
        result {
            let! _ =
                bins
                |> List.traverseResultM (fun bin -> Validation.nonNegative "Bin volume" bin.Volume)

            let total =
                bins
                |> List.sumBy (fun bin -> bin.Volume)

            do!
                Result.requireTrue
                    "Total organ volume must be finite and positive."
                    (System.Double.IsFinite(float total)
                     && total > 0.0<cm3>)

            return!
                bins
                |> List.map (fun bin -> {
                    Dose = bin.Dose
                    Fraction =
                        bin.Volume
                        / total
                })
                |> ofFractions
        }

    /// Returns the validated differential fractional bins.
    let bins (DifferentialDvh bins) =
        bins

    /// Calculates mean organ dose from a differential histogram.
    let meanDose (DifferentialDvh bins) =
        bins
        |> List.sumBy (fun bin ->
            bin.Fraction
            * AbsorbedDose.value bin.Dose)
        |> AbsorbedDose.create