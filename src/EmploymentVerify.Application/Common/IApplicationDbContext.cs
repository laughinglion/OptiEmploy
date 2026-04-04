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
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
