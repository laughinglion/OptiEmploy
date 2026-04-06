using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Commands;

public record UpdateProfileCommand(
    Guid UserId,
    string FullName,
    string? CompanyName,
    string? PhoneNumber) : IRequest<UpdateProfileResult>;

public record UpdateProfileResult(Guid UserId, string Email, string FullName, string? CompanyName, string? PhoneNumber);

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateProfileCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<UpdateProfileResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException($"User '{request.UserId}' not found.");

        user.FullName = request.FullName.Trim();
        user.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return new UpdateProfileResult(user.Id, user.Email, user.FullName, user.CompanyName, user.PhoneNumber);
    }
}
