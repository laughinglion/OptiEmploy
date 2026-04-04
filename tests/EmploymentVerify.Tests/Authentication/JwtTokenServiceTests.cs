using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Tests.Authentication;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(JwtSettings? settings = null)
    {
        settings ??= new JwtSettings
        {
            SecretKey = "ThisIsAVerySecureTestKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 30
        };

        return new JwtTokenService(Options.Create(settings));
    }

    private static User CreateUser(UserRole role = UserRole.Requestor)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            Role = role,
            IsEmailVerified = true,
            PasswordHash = "hashed"
        };
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtString()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateAccessToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT has three parts separated by dots
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var service = CreateService();
        var user = CreateUser(UserRole.Admin);

        var token = service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(user.FullName, jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Admin", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_SetsCorrectIssuerAndAudience()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains("TestAudience", jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_SetsExpirationFromSettings()
    {
        var settings = new JwtSettings
        {
            SecretKey = "ThisIsAVerySecureTestKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };
        var service = CreateService(settings);
        var user = CreateUser();

        var token = service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Expiration should be approximately 60 minutes from now
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(59));
        Assert.True(jwt.ValidTo < expectedExpiry.AddMinutes(1));
    }

    [Fact]
    public void ValidateToken_ReturnsClaimsPrincipal_ForValidToken()
    {
        var service = CreateService();
        var user = CreateUser(UserRole.Operator);

        var token = service.GenerateAccessToken(user);
        var principal = service.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.True(principal!.Identity!.IsAuthenticated);

        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Operator", roleClaim!.Value);
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForInvalidToken()
    {
        var service = CreateService();

        var principal = service.ValidateToken("invalid.token.value");

        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForTokenSignedWithDifferentKey()
    {
        var service1 = CreateService(new JwtSettings
        {
            SecretKey = "ThisIsFirstSecureTestKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 30
        });
        var service2 = CreateService(new JwtSettings
        {
            SecretKey = "ThisIsSecondSecureTestKeyAtLeast32CharactersDiff!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 30
        });

        var token = service1.GenerateAccessToken(CreateUser());
        var principal = service2.ValidateToken(token);

        Assert.Null(principal);
    }

    [Fact]
    public void Constructor_ThrowsIfSecretKeyTooShort()
    {
        var settings = new JwtSettings
        {
            SecretKey = "short",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };

        Assert.Throws<InvalidOperationException>(() => CreateService(settings));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Requestor)]
    [InlineData(UserRole.Operator)]
    public void GenerateAccessToken_IncludesCorrectRoleForEachUserRole(UserRole role)
    {
        var service = CreateService();
        var user = CreateUser(role);

        var token = service.GenerateAccessToken(user);
        var principal = service.ValidateToken(token);

        Assert.NotNull(principal);
        var roleClaim = principal!.FindFirst(ClaimTypes.Role);
        Assert.Equal(role.ToString(), roleClaim!.Value);
    }
}
