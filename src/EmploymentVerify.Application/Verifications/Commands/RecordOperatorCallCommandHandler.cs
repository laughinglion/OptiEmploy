using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Commands;

public class RecordOperatorCallCommandHandler : IRequestHandler<RecordOperatorCallCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RecordOperatorCallCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RecordOperatorCallCommand request, CancellationToken cancellationToken)
    {
        var verification = await _context.VerificationRequests
            .FirstOrDefaultAsync(v => v.Id == request.VerificationRequestId, cancellationToken);

        if (verification is null)
            return false;

        var note = new OperatorNote
        {
            Id = Guid.NewGuid(),
            VerificationRequestId = request.VerificationRequestId,
            OperatorId = request.OperatorId,
            CallOutcome = request.CallOutcome,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.OperatorNotes.Add(note);

        switch (request.CallOutcome)
        {
            case CallOutcome.Confirmed:
                verification.Status = VerificationStatus.Confirmed;
                verification.VerificationMethod = VerificationMethod.Phone;
                verification.CompletedAt = DateTime.UtcNow;

                var response = new VerificationResponse
                {
                    Id = Guid.NewGuid(),
                    VerificationRequestId = request.VerificationRequestId,
                    RespondedBy = request.OperatorId.ToString(),
                    ResponseType = ResponseType.Confirmed,
                    ConfirmedJobTitle = request.ConfirmedJobTitle,
                    ConfirmedStartDate = request.ConfirmedStartDate,
                    ConfirmedEndDate = request.ConfirmedEndDate,
                    IsCurrentlyEmployed = request.IsCurrentlyEmployed,
                    Notes = request.ResponseNotes,
                    RespondedAt = DateTime.UtcNow
                };
                _context.VerificationResponses.Add(response);
                break;

            case CallOutcome.Denied:
                verification.Status = VerificationStatus.Denied;
                verification.VerificationMethod = VerificationMethod.Phone;
                verification.CompletedAt = DateTime.UtcNow;
                break;

            case CallOutcome.Unreachable:
            case CallOutcome.CallbackScheduled:
                verification.Status = VerificationStatus.InProgress;
                break;
        }

        verification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
