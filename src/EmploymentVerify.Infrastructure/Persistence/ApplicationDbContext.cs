using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<VerificationResponse> VerificationResponses => Set<VerificationResponse>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<OperatorNote> OperatorNotes => Set<OperatorNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(e => e.RegistrationNumber)
                .HasColumnName("registration_number")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(e => e.RegistrationNumber)
                .IsUnique();

            entity.Property(e => e.HrContactName)
                .HasColumnName("hr_contact_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.HrEmail)
                .HasColumnName("hr_email")
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.HrPhone)
                .HasColumnName("hr_phone")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Address)
                .HasColumnName("address")
                .HasMaxLength(500);

            entity.Property(e => e.City)
                .HasColumnName("city")
                .HasMaxLength(100);

            entity.Property(e => e.Province)
                .HasColumnName("province")
                .HasMaxLength(100);

            entity.Property(e => e.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(10);

            entity.Property(e => e.ForceCall)
                .HasColumnName("force_call")
                .HasDefaultValue(false);

            entity.Property(e => e.IsVerified)
                .HasColumnName("is_verified")
                .HasDefaultValue(false);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            // Index for searching by name
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<VerificationRequest>(entity =>
        {
            entity.ToTable("verification_requests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequestorId).HasColumnName("requestor_id").IsRequired();

            entity.Property(e => e.EmployeeFullName).HasColumnName("employee_full_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IdType).HasColumnName("id_type").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.SaIdNumber).HasColumnName("sa_id_number").HasMaxLength(512); // encrypted
            entity.Property(e => e.PassportNumber).HasColumnName("passport_number").HasMaxLength(50);
            entity.Property(e => e.PassportCountry).HasColumnName("passport_country").HasMaxLength(100);

            entity.Property(e => e.CompanyName).HasColumnName("company_name").HasMaxLength(300).IsRequired();
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.JobTitle).HasColumnName("job_title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.EmploymentStartDate).HasColumnName("employment_start_date").IsRequired();
            entity.Property(e => e.EmploymentEndDate).HasColumnName("employment_end_date");

            entity.Property(e => e.HrContactName).HasColumnName("hr_contact_name").HasMaxLength(200);
            entity.Property(e => e.HrEmail).HasColumnName("hr_email").HasMaxLength(256);
            entity.Property(e => e.HrPhone).HasColumnName("hr_phone").HasMaxLength(20);

            // POPIA consent fields — auditable
            entity.Property(e => e.PopiaConsentGiven).HasColumnName("popia_consent_given").IsRequired();
            entity.Property(e => e.AccuracyConfirmed).HasColumnName("accuracy_confirmed").IsRequired();
            entity.Property(e => e.ConsentType).HasColumnName("consent_type").HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.ConsentRecordedAt).HasColumnName("consent_recorded_at").IsRequired();

            entity.Property(e => e.CostAmount).HasColumnName("cost_amount").HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).HasDefaultValue(Domain.Enums.VerificationStatus.Pending);
            entity.Property(e => e.VerificationMethod).HasColumnName("verification_method").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");

            // Indexes
            entity.HasIndex(e => e.RequestorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CompanyId);

            // Relationships
            entity.HasOne(e => e.Requestor).WithMany().HasForeignKey(e => e.RequestorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VerificationResponse>(entity =>
        {
            entity.ToTable("verification_responses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VerificationRequestId).HasColumnName("verification_request_id").IsRequired();
            entity.Property(e => e.RespondedBy).HasColumnName("responded_by").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ResponseType).HasColumnName("response_type").HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.ConfirmedJobTitle).HasColumnName("confirmed_job_title").HasMaxLength(200);
            entity.Property(e => e.ConfirmedStartDate).HasColumnName("confirmed_start_date");
            entity.Property(e => e.ConfirmedEndDate).HasColumnName("confirmed_end_date");
            entity.Property(e => e.IsCurrentlyEmployed).HasColumnName("is_currently_employed");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at").IsRequired();

            entity.HasIndex(e => e.VerificationRequestId).IsUnique();

            entity.HasOne(e => e.VerificationRequest)
                .WithMany()
                .HasForeignKey(e => e.VerificationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("email_verification_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VerificationRequestId).HasColumnName("verification_request_id").IsRequired();
            entity.Property(e => e.Token).HasColumnName("token").HasMaxLength(128).IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.VerificationRequestId);

            entity.HasOne(e => e.VerificationRequest)
                .WithMany()
                .HasForeignKey(e => e.VerificationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OperatorNote>(entity =>
        {
            entity.ToTable("operator_notes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VerificationRequestId).HasColumnName("verification_request_id").IsRequired();
            entity.Property(e => e.OperatorId).HasColumnName("operator_id").IsRequired();
            entity.Property(e => e.CallOutcome).HasColumnName("call_outcome").HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.VerificationRequestId);
            entity.HasIndex(e => e.OperatorId);

            entity.HasOne(e => e.VerificationRequest)
                .WithMany()
                .HasForeignKey(e => e.VerificationRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Operator)
                .WithMany()
                .HasForeignKey(e => e.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CompanyName)
                .HasColumnName("company_name")
                .HasMaxLength(200);

            entity.Property(e => e.CreditBalance)
                .HasColumnName("credit_balance")
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.IsEmailVerified)
                .HasColumnName("is_email_verified")
                .HasDefaultValue(false);

            entity.Property(e => e.EmailVerificationToken)
                .HasColumnName("email_verification_token")
                .HasMaxLength(256);

            entity.HasIndex(e => e.EmailVerificationToken)
                .IsUnique()
                .HasFilter("email_verification_token IS NOT NULL");

            entity.Property(e => e.EmailVerificationTokenExpiresAt)
                .HasColumnName("email_verification_token_expires_at");

            entity.Property(e => e.EmailVerifiedAt)
                .HasColumnName("email_verified_at");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
        });
    }
}
