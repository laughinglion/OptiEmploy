using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public class SsoLoginCommandHandler : IRequestHandler<SsoLoginCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public SsoLoginCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> Handle(SsoLoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // First-time SSO login — create account automatically
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = string.Empty, // SSO users have no password
                FullName = request.FullName.Trim(),
                Role = UserRole.Requestor,
                CreditBalance = 0m,
                IsActive = true,
                IsEmailVerified = true, // Provider has already verified the email
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (!user.IsActive)
        {
            return new LoginResult(false, null, null, null, null, null,
                "Your account has been deactivated. Please contact support.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResult(true, token, user.Id, user.Email, user.FullName, user.Role.ToString(), null);
    }
}
