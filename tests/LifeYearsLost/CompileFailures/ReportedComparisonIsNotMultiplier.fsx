#r "../bin/Debug/net8.0/LifeYearsLost.dll"
open LifeYearsLost
let invalid = ExcessHazardMultiplier.apply (Unchecked.defaultof<ReportedSexComparison>) (Unchecked.defaultof<ExcessHazardRatio>)
