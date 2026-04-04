using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Queries;

public class ListCompaniesQueryHandler : IRequestHandler<ListCompaniesQuery, List<CompanyDto>>
{
    private readonly IApplicationDbContext _context;

    public ListCompaniesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompanyDto>> Handle(ListCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Companies.AsNoTracking().AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.RegistrationNumber.ToLower().Contains(term));
        }

        var companies = await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return companies.Select(CompanyMapper.ToDto).ToList();
    }
}
