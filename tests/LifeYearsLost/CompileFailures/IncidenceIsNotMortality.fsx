#r "../bin/Debug/net8.0/LifeYearsLost.dll"
open LifeYearsLost
let invalid = ExcessMortalityHazard.fromBaseline (Unchecked.defaultof<ExcessHazardRatio>) (Unchecked.defaultof<IncidenceHazard>)
