#r "../bin/Debug/net8.0/LifeYearsLost.dll"
open LifeYearsLost

let invalid =
    IncidentEvent(
        Unchecked.defaultof<IncidenceHazard>,
        Unchecked.defaultof<RemainingLife>,
        Unchecked.defaultof<RemainingLife>
    )