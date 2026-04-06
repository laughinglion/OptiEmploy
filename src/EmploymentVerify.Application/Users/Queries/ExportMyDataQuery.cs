using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Queries;

/// <summary>
/// POPIA Article 23 — Right of Access: returns all personal data held about the user.
/// </summary>
public record ExportMyDataQuery(Guid UserId) : IRequest<MyDataExportDto?>;

public record MyDataExportDto(
    Guid UserId,
    string Email,
    string FullName,
    string? CompanyName,
    string? PhoneNumber,
    string Role,
    decimal CreditBalance,
    bool IsEmailVerified,
    DateTime AccountCreatedAt,
    List<VerificationDataItem> Verifications,
    List<CreditTransactionItem> CreditTransactions,
    DateTime ExportedAt);

public record VerificationDataItem(
    Guid Id,
    string EmployeeFullName,
    string CompanyName,
    string JobTitle,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate,
    string Status,
    string ConsentType,
    DateTime ConsentRecordedAt,
    DateTime SubmittedAt,
    DateTime? CompletedAt);

public record CreditTransactionItem(
    DateTime Date,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string Reason);

public class ExportMyDataQueryHandler : IRequestHandler<ExportMyDataQuery, MyDataExportDto?>
{
    private readonly IApplicationDbContext _context;

    public ExportMyDataQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<MyDataExportDto?> Handle(ExportMyDataQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null) return null;

        var verifications = await _context.VerificationRequests
            .AsNoTracking()
            .Where(v => v.RequestorId == request.UserId)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VerificationDataItem(
                v.Id,
                v.EmployeeFullName,
                v.CompanyName,
                v.JobTitle,
                v.EmploymentStartDate,
                v.EmploymentEndDate,
                v.Status.ToString(),
                v.ConsentType.ToString(),
                v.ConsentRecordedAt,
                v.CreatedAt,
                v.CompletedAt))
            .ToListAsync(cancellationToken);

        var transactions = await _context.CreditTransactions
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CreditTransactionItem(
                t.CreatedAt,
                t.TransactionType,
                t.Amount,
                t.BalanceAfter,
                t.Reason))
            .ToListAsync(cancellationToken);

        return new MyDataExportDto(
            user.Id,
            user.Email,
            user.FullName,
            user.CompanyName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.CreditBalance,
            user.IsEmailVerified,
            user.CreatedAt,
            verifications,
            transactions,
            DateTime.UtcNow);
    }
}
