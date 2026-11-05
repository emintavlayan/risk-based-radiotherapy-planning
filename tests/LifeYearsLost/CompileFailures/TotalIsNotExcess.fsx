#r "../bin/Debug/net8.0/LifeYearsLost.dll"
open LifeYearsLost

let invalid =
    ExcessIncidenceHazard.fromBaseline (Unchecked.defaultof<HazardRatio>) (Unchecked.defaultof<IncidenceHazard>)