using MediatR;

namespace EmploymentVerify.Application.Auth.Commands;

/// <summary>
/// Finds or creates a user from an external SSO provider (Google, Microsoft).
/// On first SSO login, creates a Requestor account using the provider's email and name.
/// Returns a JWT for use by the Web app.
/// </summary>
public record SsoLoginCommand(
    string Email,
    string FullName,
    string Provider) : IRequest<LoginResult>;
