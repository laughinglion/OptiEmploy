using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Queries;

public record ListUsersQuery(string? Role = null) : IRequest<List<UserSummaryDto>>;

public record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? CompanyName,
    decimal CreditBalance,
    bool IsActive,
    DateTime CreatedAt);

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, List<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public ListUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserSummaryDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<Domain.Enums.UserRole>(request.Role, ignoreCase: true, out var role))
        {
            query = query.Where(u => u.Role == role);
        }

        return await query
            .OrderBy(u => u.FullName)
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
    }
}
