using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, AssignUserRoleResult>
{
    private readonly IApplicationDbContext _context;

    public AssignUserRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssignUserRoleResult> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"User with ID '{request.UserId}' was not found.");
        }

        var previousRole = user.Role.ToString();
        user.Role = request.Role;

        await _context.SaveChangesAsync(cancellationToken);

        return new AssignUserRoleResult(
            user.Id,
            user.Email,
            previousRole,
            user.Role.ToString()
        );
    }
}
