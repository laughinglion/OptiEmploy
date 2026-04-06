using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Queries;

public record GetUserTransactionsQuery(Guid UserId) : IRequest<List<CreditTransactionDto>>;

public record CreditTransactionDto(
    Guid Id,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string TransactionType,
    string Reason,
    Guid? RelatedVerificationId,
    DateTime CreatedAt);

public class GetUserTransactionsQueryHandler : IRequestHandler<GetUserTransactionsQuery, List<CreditTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    public GetUserTransactionsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<CreditTransactionDto>> Handle(GetUserTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.CreditTransactions
            .Where(t => t.UserId == request.UserId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CreditTransactionDto(
                t.Id, t.Amount, t.BalanceBefore, t.BalanceAfter,
                t.TransactionType, t.Reason, t.RelatedVerificationId, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
