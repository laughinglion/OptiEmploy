using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Commands;

public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeleteCompanyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company is null)
        {
            throw new InvalidOperationException($"Company with ID '{request.CompanyId}' not found.");
        }

        // Soft delete — mark as inactive rather than removing
        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
