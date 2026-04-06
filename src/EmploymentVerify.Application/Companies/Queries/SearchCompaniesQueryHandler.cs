using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EmploymentVerify.Application.Companies.Queries;

public class SearchCompaniesQueryHandler : IRequestHandler<SearchCompaniesQuery, List<CompanySearchResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public SearchCompaniesQueryHandler(IApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<CompanySearchResult>> Handle(SearchCompaniesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm) || request.SearchTerm.Trim().Length < 2)
        {
            return [];
        }

        var term = request.SearchTerm.Trim().ToLowerInvariant();
        var cacheKey = $"company_search:{term}";

        if (_cache.TryGetValue(cacheKey, out List<CompanySearchResult>? cached) && cached is not null)
            return cached;

        var results = await _context.Companies
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

        _cache.Set(cacheKey, results, CacheTtl);
        return results;
    }
}
