using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetWorkQueueQueryHandler : IRequestHandler<GetWorkQueueQuery, List<WorkQueueItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkQueueQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkQueueItemDto>> Handle(GetWorkQueueQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.VerificationRequests
            .AsNoTracking()
            .Where(v => v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InProgress)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new WorkQueueItemDto(
                v.Id,
                v.EmployeeFullName,
                v.CompanyName,
                v.HrPhone,
                v.CreatedAt,
                v.Status.ToString(),
                v.ConsentType.ToString()))
            .ToListAsync(cancellationToken);

        return items;
    }
}
