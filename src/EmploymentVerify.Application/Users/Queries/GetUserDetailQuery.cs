using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Queries;

public record GetUserDetailQuery(Guid UserId) : IRequest<UserDetailDto?>;

public record UserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string? CompanyName,
    string? PhoneNumber,
    string Role,
    decimal CreditBalance,
    bool IsActive,
    bool IsEmailVerified,
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    DateTime CreatedAt,
    int TotalVerifications,
    int CompletedVerifications);

public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUserDetailQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<UserDetailDto?> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null) return null;

        var totalVerifications = await _context.VerificationRequests
            .CountAsync(v => v.RequestorId == request.UserId, cancellationToken);

        var completedVerifications = await _context.VerificationRequests
            .CountAsync(v => v.RequestorId == request.UserId && v.CompletedAt != null, cancellationToken);

        return new UserDetailDto(
            user.Id,
            user.Email,
            user.FullName,
            user.CompanyName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.CreditBalance,
            user.IsActive,
            user.IsEmailVerified,
            user.FailedLoginAttempts,
            user.LockedUntil,
            user.CreatedAt,
            totalVerifications,
            completedVerifications);
    }
}
