using MediatR;

namespace EmploymentVerify.Application.Companies.Queries;

public record ListCompaniesQuery(
    string? SearchTerm = null,
    bool IncludeInactive = false
) : IRequest<List<CompanyDto>>;
