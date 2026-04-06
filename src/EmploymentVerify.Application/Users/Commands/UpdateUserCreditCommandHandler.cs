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

        var balanceBefore = user.CreditBalance;
        var newBalance = user.CreditBalance + request.Amount;

        if (newBalance < 0)
            throw new InvalidOperationException(
                $"Insufficient credit balance. Current balance: {user.CreditBalance:F2}, attempted deduction: {Math.Abs(request.Amount):F2}.");

        user.CreditBalance = newBalance;

        var transaction = new Domain.Entities.CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Amount = request.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = newBalance,
            TransactionType = request.Amount > 0 ? "Credit" : "Debit",
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };
        _context.CreditTransactions.Add(transaction);

        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "AdminCreditAdjustment",
            Description = $"Admin adjusted credits by {request.Amount:F2} for user {user.Email}. Reason: {request.Reason}",
            TargetUserId = user.Id,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return user.CreditBalance;
    }
}
