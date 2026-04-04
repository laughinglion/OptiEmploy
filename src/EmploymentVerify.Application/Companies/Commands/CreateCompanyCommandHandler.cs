using EmploymentVerify.Application.Common;
using EmploymentVerify.Application.Companies.Queries;
using EmploymentVerify.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Companies.Commands;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCompanyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var normalizedRegNumber = request.RegistrationNumber.Trim();

        var exists = await _context.Companies
            .AnyAsync(c => c.RegistrationNumber == normalizedRegNumber, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"A company with registration number '{normalizedRegNumber}' already exists.");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            RegistrationNumber = normalizedRegNumber,
            HrContactName = request.HrContactName.Trim(),
            HrEmail = request.HrEmail.Trim().ToLowerInvariant(),
            HrPhone = request.HrPhone.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            Province = request.Province?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            ForceCall = request.ForceCall,
            IsVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        return CompanyMapper.ToDto(company);
    }
}
