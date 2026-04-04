using EmploymentVerify.Application.Companies.Queries;
using EmploymentVerify.Domain.Entities;

namespace EmploymentVerify.Application.Companies;

/// <summary>
/// Maps Company domain entities to CompanyDto records.
/// </summary>
internal static class CompanyMapper
{
    internal static CompanyDto ToDto(Company company) => new(
        company.Id,
        company.Name,
        company.RegistrationNumber,
        company.HrContactName,
        company.HrEmail,
        company.HrPhone,
        company.Address,
        company.City,
        company.Province,
        company.PostalCode,
        company.ForceCall,
        company.IsVerified,
        company.IsActive,
        company.CreatedAt,
        company.UpdatedAt
    );
}
