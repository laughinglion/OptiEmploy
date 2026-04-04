using MediatR;

namespace EmploymentVerify.Application.Companies.Queries;

/// <summary>
/// Lightweight query for company autocomplete search.
/// Returns only active, verified companies matching the search term.
/// Used by the verification request form for company name autocomplete.
/// </summary>
public record SearchCompaniesQuery(string SearchTerm) : IRequest<List<CompanySearchResult>>;

public record CompanySearchResult(
    Guid Id,
    string Name,
    string? HrContactName,
    string? HrEmail,
    string? HrPhone);
