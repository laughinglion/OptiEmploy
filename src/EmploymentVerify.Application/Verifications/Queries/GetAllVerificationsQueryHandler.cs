using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetAllVerificationsQueryHandler : IRequestHandler<GetAllVerificationsQuery, PagedResult<VerificationSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllVerificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<VerificationSummaryDto>> Handle(GetAllVerificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VerificationRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<VerificationStatus>(request.StatusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(v => v.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VerificationSummaryDto(
                v.Id,
                v.EmployeeFullName,
                v.CompanyName,
                v.JobTitle,
                v.Status.ToString(),
                v.CreatedAt,
                v.CompletedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<VerificationSummaryDto>(items, totalCount, page, pageSize);
    }
}
