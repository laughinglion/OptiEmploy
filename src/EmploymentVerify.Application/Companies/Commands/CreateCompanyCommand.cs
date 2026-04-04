using EmploymentVerify.Application.Companies.Queries;
using MediatR;

namespace EmploymentVerify.Application.Companies.Commands;

public record CreateCompanyCommand(
    string Name,
    string RegistrationNumber,
    string HrContactName,
    string HrEmail,
    string HrPhone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode,
    bool ForceCall
) : IRequest<CompanyDto>;
