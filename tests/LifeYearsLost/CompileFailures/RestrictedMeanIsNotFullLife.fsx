#r "../bin/Debug/net8.0/LifeYearsLost.dll"
open LifeYearsLost

let invalid =
    RemainingLifeAtAge.create (Unchecked.defaultof<AttainedAge>) (Unchecked.defaultof<RestrictedMeanLife>)