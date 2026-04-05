using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return new LoginResult(false, null, null, null, null, null, "Invalid email or password.");

        if (!user.IsActive)
            return new LoginResult(false, null, null, null, null, null, "Your account is inactive. Please verify your email or contact support.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return new LoginResult(false, null, null, null, null, null, "Invalid email or password.");

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResult(true, token, user.Id, user.Email, user.FullName, user.Role.ToString(), null);
    }
}
