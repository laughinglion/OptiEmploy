using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Application.Verifications.Commands;

public class SubmitVerificationCommandHandler : IRequestHandler<SubmitVerificationCommand, SubmitVerificationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly PricingSettings _pricingSettings;

    public SubmitVerificationCommandHandler(IApplicationDbContext context, IMediator mediator, IOptions<PricingSettings> pricingOptions)
    {
        _context = context;
        _mediator = mediator;
        _pricingSettings = pricingOptions.Value;
    }

    public async Task<SubmitVerificationResult> Handle(SubmitVerificationCommand request, CancellationToken cancellationToken)
    {
        // AC 27 — check requestor has sufficient credit
        var requestor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.RequestorId, cancellationToken);

        if (requestor is null)
            throw new InvalidOperationException("Requestor not found.");

        if (requestor.CreditBalance < _pricingSettings.VerificationCostCredits)
            throw new InvalidOperationException(
                $"Insufficient credit balance. Required: {_pricingSettings.VerificationCostCredits:F2}, available: {requestor.CreditBalance:F2}.");

        // Parse enums from string values (already validated by FluentValidation)
        var idType = Enum.Parse<IdentificationType>(request.IdType);
        var consentType = Enum.Parse<ConsentType>(request.ConsentType);

        // AC 11 / 16 — determine routing: email or operator queue
        Company? company = null;
        var routeToOperator = true;
        var hrEmail = request.HrEmail?.Trim().ToLowerInvariant();

        if (request.SelectedCompanyId.HasValue)
        {
            company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == request.SelectedCompanyId.Value && c.IsActive, cancellationToken);

            if (company is not null)
            {
                hrEmail = company.HrEmail;
                routeToOperator = company.ForceCall;
            }
        }

        var verification = new VerificationRequest
        {
            Id = Guid.NewGuid(),
            RequestorId = request.RequestorId,
            EmployeeFullName = request.EmployeeFullName.Trim(),
            IdType = idType,
            SaIdNumber = idType == IdentificationType.SaIdNumber ? request.SaIdNumber?.Trim() : null,
            PassportNumber = idType == IdentificationType.Passport ? request.PassportNumber?.Trim() : null,
            PassportCountry = idType == IdentificationType.Passport ? request.PassportCountry?.Trim() : null,
            CompanyName = request.CompanyName.Trim(),
            CompanyId = request.SelectedCompanyId,
            JobTitle = request.JobTitle.Trim(),
            EmploymentStartDate = request.EmploymentStartDate,
            EmploymentEndDate = request.EmploymentEndDate,
            HrContactName = company?.HrContactName ?? (string.IsNullOrWhiteSpace(request.HrContactName) ? null : request.HrContactName.Trim()),
            HrEmail = hrEmail,
            HrPhone = company?.HrPhone ?? (string.IsNullOrWhiteSpace(request.HrPhone) ? null : request.HrPhone.Trim()),

            // POPIA consent — recorded with timestamp for audit trail
            PopiaConsentGiven = request.ConsentToPopia,
            AccuracyConfirmed = request.ConsentAccuracy,
            ConsentType = consentType,
            ConsentRecordedAt = DateTime.UtcNow,

            // AC 26 — record cost for this verification
            CostAmount = _pricingSettings.VerificationCostCredits,

            Status = VerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.VerificationRequests.Add(verification);

        // AC 27 — deduct credit atomically with a conditional update to prevent double-spend
        var cost = _pricingSettings.VerificationCostCredits;
        var rowsUpdated = await _context.Users
            .Where(u => u.Id == request.RequestorId && u.CreditBalance >= cost)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.CreditBalance, u => u.CreditBalance - cost), cancellationToken);

        if (rowsUpdated == 0)
            throw new InvalidOperationException(
                $"Insufficient credit balance. Required: {cost:F2}, available: {requestor.CreditBalance:F2}.");

        // Detach the stale user entity before re-reading
        _context.Entry(requestor).State = EntityState.Detached;

        // Re-read balance after atomic deduction for accurate transaction record
        var newBalance = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.RequestorId)
            .Select(u => u.CreditBalance)
            .FirstAsync(cancellationToken);

        var creditTx = new Domain.Entities.CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = requestor.Id,
            Amount = -cost,
            BalanceBefore = newBalance + cost,
            BalanceAfter = newBalance,
            TransactionType = "Debit",
            Reason = $"Verification request submitted",
            RelatedVerificationId = verification.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.CreditTransactions.Add(creditTx);

        await _context.SaveChangesAsync(cancellationToken);

        // AC 11 — auto-send email if company is in directory and NOT force-call
        if (!routeToOperator && !string.IsNullOrWhiteSpace(hrEmail))
        {
            try
            {
                var baseUrl = request.BaseUrl ?? string.Empty;
                await _mediator.Send(new SendVerificationEmailCommand(verification.Id, baseUrl), cancellationToken);
            }
            catch
            {
                // AC 16 — email failure: leave as Pending so operator queue picks it up
                // Status is already Pending; no further action needed
            }
        }
        // AC 16 — force-call or not in directory: stays Pending, visible in operator work queue

        return new SubmitVerificationResult(
            verification.Id,
            verification.Status.ToString(),
            verification.CreatedAt);
    }
}
