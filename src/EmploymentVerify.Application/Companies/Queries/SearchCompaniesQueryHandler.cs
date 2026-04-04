using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Queries;

public class SearchCompaniesQueryHandler : IRequestHandler<SearchCompaniesQuery, List<CompanySearchResult>>
{
    private readonly IApplicationDbContext _context;

    public SearchCompaniesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompanySearchResult>> Handle(SearchCompaniesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.SearchTerm.Trim().Length < 2)
        {
            return [];
        }

        var term = request.SearchTerm.Trim().ToLowerInvariant();

        return await _context.Companies
            .AsNoTracking()
            .Where(c => c.IsActive && c.IsVerified)
            .Where(c => c.Name.ToLower().Contains(term))
            .OrderBy(c => c.Name)
            .Take(10)
            .Select(c => new CompanySearchResult(
                c.Id,
                c.Name,
                c.HrContactName,
                c.HrEmail,
                c.HrPhone))
            .ToListAsync(cancellationToken);
    }
}
