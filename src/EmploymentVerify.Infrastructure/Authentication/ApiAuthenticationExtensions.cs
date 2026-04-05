using System.Text;
using EmploymentVerify.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EmploymentVerify.Infrastructure.Authentication;

/// <summary>
/// Extension methods to register JWT bearer authentication and authorization for the API project.
/// </summary>
public static class ApiAuthenticationExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication and configures token validation.
    /// Call this in the API Program.cs: <c>builder.Services.AddApiAuthentication(builder.Configuration);</c>
    /// </summary>
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);

        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings are not configured. Add a 'Jwt' section to appsettings.json.");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be configured and be at least 32 characters.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Return 401/403 with machine-readable error for API consumers
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var body = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "unauthorized",
                        message = "Authentication is required to access this resource."
                    });
                    return context.Response.WriteAsync(body);
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var body = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "forbidden",
                        message = "You do not have permission to access this resource."
                    });
                    return context.Response.WriteAsync(body);
                }
            };
        });

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGeneratorAdapter>();

        return services;
    }
}

/// <summary>Adapts JwtTokenService to the Application-layer IJwtTokenGenerator interface.</summary>
internal sealed class JwtTokenGeneratorAdapter : IJwtTokenGenerator
{
    private readonly IJwtTokenService _jwtTokenService;
    public JwtTokenGeneratorAdapter(IJwtTokenService jwtTokenService) => _jwtTokenService = jwtTokenService;
    public string GenerateToken(Domain.Entities.User user) => _jwtTokenService.GenerateAccessToken(user);
}
