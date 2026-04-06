using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Queries;

public record GetMyProfileQuery(Guid UserId) : IRequest<MyProfileDto?>;

public record MyProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string? CompanyName,
    string? PhoneNumber,
    string Role,
    decimal CreditBalance,
    bool IsEmailVerified,
    DateTime CreatedAt);

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfileDto?>
{
    private readonly IApplicationDbContext _context;

    public GetMyProfileQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<MyProfileDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new MyProfileDto(
                u.Id,
                u.Email,
                u.FullName,
                u.CompanyName,
                u.PhoneNumber,
                u.Role.ToString(),
                u.CreditBalance,
                u.IsEmailVerified,
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
