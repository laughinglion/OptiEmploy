using MediatR;

namespace EmploymentVerify.Application.Companies.Queries;

public record GetCompanyByIdQuery(Guid CompanyId) : IRequest<CompanyDto?>;

public record CompanyDto(
    Guid Id,
    string Name,
    string RegistrationNumber,
    string HrContactName,
    string HrEmail,
    string HrPhone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode,
    bool ForceCall,
    bool IsVerified,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
