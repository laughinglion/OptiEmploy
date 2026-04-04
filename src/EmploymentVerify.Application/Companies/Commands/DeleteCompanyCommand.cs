using MediatR;

namespace EmploymentVerify.Application.Companies.Commands;

/// <summary>
/// Soft-deletes a company by setting IsActive = false.
/// Data is preserved for POPIA audit trail compliance.
/// </summary>
public record DeleteCompanyCommand(Guid CompanyId) : IRequest<Unit>;
