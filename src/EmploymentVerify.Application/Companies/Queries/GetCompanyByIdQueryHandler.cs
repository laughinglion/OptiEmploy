using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Queries;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCompanyByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        return company is null ? null : CompanyMapper.ToDto(company);
    }
}
