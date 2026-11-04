namespace Shared

/// Represents application readiness independently of the scientific domain.
type ApplicationStatus = { Name: string; Stage: string }

/// Represents the infrastructure API before scientific workflows are introduced.
type IApplicationApi = {
    getStatus: unit -> Async<ApplicationStatus>
}
