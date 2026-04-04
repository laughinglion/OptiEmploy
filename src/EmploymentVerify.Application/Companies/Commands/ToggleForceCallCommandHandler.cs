using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Commands;

public class ToggleForceCallCommandHandler : IRequestHandler<ToggleForceCallCommand, ToggleForceCallResult>
{
    private readonly IApplicationDbContext _context;

    public ToggleForceCallCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ToggleForceCallResult> Handle(ToggleForceCallCommand request, CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company is null)
        {
            throw new InvalidOperationException($"Company with ID '{request.CompanyId}' was not found.");
        }

        company.ForceCall = request.ForceCall;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ToggleForceCallResult(
            company.Id,
            company.Name,
            company.ForceCall);
    }
}
