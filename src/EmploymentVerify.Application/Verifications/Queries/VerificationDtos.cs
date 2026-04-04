namespace EmploymentVerify.Application.Verifications.Queries;

public record VerificationSummaryDto(
    Guid Id, string EmployeeFullName, string CompanyName, string JobTitle,
    string Status, DateTime SubmittedAt, DateTime? CompletedAt);

public record VerificationDetailDto(
    Guid Id, Guid RequestorId, string EmployeeFullName, string EmployeeIdNumber,
    string CompanyName, string JobTitle, DateOnly EmploymentStartDate, DateOnly? EmploymentEndDate,
    string Status, string? VerificationMethod, DateTime SubmittedAt, DateTime? CompletedAt,
    string ConsentType, VerificationResponseDto? Response);

public record VerificationResponseDto(
    string RespondedBy, string ResponseType, string? ConfirmedJobTitle,
    DateOnly? ConfirmedStartDate, DateOnly? ConfirmedEndDate, bool? IsCurrentlyEmployed,
    string? Notes, DateTime RespondedAt);

public record WorkQueueItemDto(
    Guid Id, string EmployeeFullName, string CompanyName, string? HrContactPhone,
    DateTime SubmittedAt, string Status, string ConsentType);

public record VerificationStatsDto(
    int TotalToday, int TotalThisMonth, int ConfirmedCount, int DeniedCount,
    int PendingCount, decimal ConfirmationRate);
