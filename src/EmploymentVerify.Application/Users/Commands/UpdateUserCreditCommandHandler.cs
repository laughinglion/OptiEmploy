using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Commands;

public class UpdateUserCreditCommandHandler : IRequestHandler<UpdateUserCreditCommand, decimal>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserCreditCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> Handle(UpdateUserCreditCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new InvalidOperationException($"User with ID '{request.UserId}' was not found.");

        var newBalance = user.CreditBalance + request.Amount;

        if (newBalance < 0)
            throw new InvalidOperationException(
                $"Insufficient credit balance. Current balance: {user.CreditBalance:F2}, attempted deduction: {Math.Abs(request.Amount):F2}.");

        user.CreditBalance = newBalance;

        await _context.SaveChangesAsync(cancellationToken);

        return user.CreditBalance;
    }
}
