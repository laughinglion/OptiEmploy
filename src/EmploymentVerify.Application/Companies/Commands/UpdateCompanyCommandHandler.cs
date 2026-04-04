using EmploymentVerify.Application.Common;
using EmploymentVerify.Application.Companies.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Commands;

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateCompanyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company is null)
        {
            throw new InvalidOperationException($"Company with ID '{request.CompanyId}' was not found.");
        }

        var normalizedRegNumber = request.RegistrationNumber.Trim();

        // Check for duplicate registration number (excluding current company)
        var duplicate = await _context.Companies
            .AnyAsync(c => c.RegistrationNumber == normalizedRegNumber && c.Id != request.CompanyId, cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Another company with registration number '{normalizedRegNumber}' already exists.");
        }

        company.Name = request.Name.Trim();
        company.RegistrationNumber = normalizedRegNumber;
        company.HrContactName = request.HrContactName.Trim();
        company.HrEmail = request.HrEmail.Trim().ToLowerInvariant();
        company.HrPhone = request.HrPhone.Trim();
        company.Address = request.Address?.Trim();
        company.City = request.City?.Trim();
        company.Province = request.Province?.Trim();
        company.PostalCode = request.PostalCode?.Trim();
        company.ForceCall = request.ForceCall;
        company.IsActive = request.IsActive;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return CompanyMapper.ToDto(company);
    }
}
