using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetWorkQueueQueryHandler : IRequestHandler<GetWorkQueueQuery, PagedResult<WorkQueueItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkQueueQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<WorkQueueItemDto>> Handle(GetWorkQueueQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VerificationRequests
            .AsNoTracking()
            .Where(v => v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InProgress)
            .OrderBy(v => v.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new WorkQueueItemDto(
                v.Id,
                v.EmployeeFullName,
                v.CompanyName,
                v.HrPhone,
                v.CreatedAt,
                v.Status.ToString(),
                v.ConsentType.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkQueueItemDto>(items, totalCount, page, pageSize);
    }
}
