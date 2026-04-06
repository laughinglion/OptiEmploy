using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Queries;

public record ListUsersQuery(string? Role = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserSummaryDto>>;

public record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? CompanyName,
    decimal CreditBalance,
    bool IsActive,
    DateTime CreatedAt);

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public ListUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserSummaryDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<Domain.Enums.UserRole>(request.Role, ignoreCase: true, out var role))
        {
            query = query.Where(u => u.Role == role);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.ToString(),
                u.CompanyName,
                u.CreditBalance,
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSummaryDto>(items, totalCount, page, pageSize);
    }
}
