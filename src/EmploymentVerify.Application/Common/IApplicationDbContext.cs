using EmploymentVerify.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Common;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<VerificationRequest> VerificationRequests { get; }
    DbSet<VerificationResponse> VerificationResponses { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<OperatorNote> OperatorNotes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<CreditTransaction> CreditTransactions { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
