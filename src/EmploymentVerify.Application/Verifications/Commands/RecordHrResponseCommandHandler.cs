using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Commands;

public class RecordHrResponseCommandHandler : IRequestHandler<RecordHrResponseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RecordHrResponseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RecordHrResponseCommand request, CancellationToken cancellationToken)
    {
        var emailToken = await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (emailToken is null)
            return false;

        if (emailToken.IsUsed)
            return false;

        if (emailToken.ExpiresAt < DateTime.UtcNow)
            return false;

        var verification = await _context.VerificationRequests
            .FirstOrDefaultAsync(v => v.Id == emailToken.VerificationRequestId, cancellationToken);

        if (verification is null)
            return false;

        var response = new VerificationResponse
        {
            Id = Guid.NewGuid(),
            VerificationRequestId = emailToken.VerificationRequestId,
            RespondedBy = request.RespondedBy,
            ResponseType = request.ResponseType,
            ConfirmedJobTitle = request.ConfirmedJobTitle,
            ConfirmedStartDate = request.ConfirmedStartDate,
            ConfirmedEndDate = request.ConfirmedEndDate,
            IsCurrentlyEmployed = request.IsCurrentlyEmployed,
            Notes = request.Notes,
            RespondedAt = DateTime.UtcNow
        };

        _context.VerificationResponses.Add(response);

        emailToken.IsUsed = true;

        verification.Status = request.ResponseType == ResponseType.Denied
            ? VerificationStatus.Denied
            : VerificationStatus.Confirmed;
        verification.VerificationMethod = VerificationMethod.Email;
        verification.CompletedAt = DateTime.UtcNow;
        verification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
