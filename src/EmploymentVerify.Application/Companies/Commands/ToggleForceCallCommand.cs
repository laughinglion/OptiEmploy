using MediatR;

namespace EmploymentVerify.Application.Companies.Commands;

/// <summary>
/// Toggles or explicitly sets the ForceCall flag on a company.
/// When ForceCall is true, verification requests for this company will skip
/// automated email and always be routed to the operator phone queue.
/// </summary>
public record ToggleForceCallCommand(
    Guid CompanyId,
    bool ForceCall
) : IRequest<ToggleForceCallResult>;

public record ToggleForceCallResult(
    Guid CompanyId,
    string CompanyName,
    bool ForceCall
);
