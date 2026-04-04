using EmploymentVerify.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Tests.Authentication;

public class MicrosoftAuthenticationExtensionsTests
{
    private static IConfiguration BuildConfig(
        string clientId = "",
        string clientSecret = "",
        string tenantId = "common",
        string callbackPath = "/signin-microsoft")
    {
        var configData = new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "",
            ["Authentication:Google:ClientSecret"] = "",
            ["Authentication:Microsoft:ClientId"] = clientId,
            ["Authentication:Microsoft:ClientSecret"] = clientSecret,
            ["Authentication:Microsoft:TenantId"] = tenantId,
            ["Authentication:Microsoft:CallbackPath"] = callbackPath,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void IsMicrosoftAuthConfigured_ReturnsFalse_WhenEmpty()
    {
        var config = BuildConfig();
        Assert.False(MicrosoftAuthenticationExtensions.IsMicrosoftAuthConfigured(config));
    }

    [Fact]
    public void IsMicrosoftAuthConfigured_ReturnsFalse_WhenPlaceholder()
    {
        var config = BuildConfig(clientId: "your-microsoft-client-id", clientSecret: "secret");
        Assert.False(MicrosoftAuthenticationExtensions.IsMicrosoftAuthConfigured(config));
    }

    [Fact]
    public void IsMicrosoftAuthConfigured_ReturnsTrue_WhenConfigured()
    {
        var config = BuildConfig(clientId: "real-id", clientSecret: "real-secret");
        Assert.True(MicrosoftAuthenticationExtensions.IsMicrosoftAuthConfigured(config));
    }

    [Fact]
    public void MicrosoftScheme_HasExpectedValue()
    {
        Assert.Equal("Microsoft", MicrosoftAuthenticationExtensions.MicrosoftScheme);
    }

    [Fact]
    public async Task AddExternalSsoAuthentication_RegistersMicrosoftScheme()
    {
        var config = BuildConfig(clientId: "test-id", clientSecret: "test-secret");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternalSsoAuthentication(config);

        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var microsoftScheme = await schemeProvider.GetSchemeAsync(
            MicrosoftAuthenticationExtensions.MicrosoftScheme);

        Assert.NotNull(microsoftScheme);
        Assert.Equal("Microsoft", microsoftScheme.DisplayName);
    }

    [Fact]
    public async Task AddExternalSsoAuthentication_RegistersGoogleScheme()
    {
        var config = BuildConfig(clientId: "test-id", clientSecret: "test-secret");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternalSsoAuthentication(config);

        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var googleScheme = await schemeProvider.GetSchemeAsync("Google");

        Assert.NotNull(googleScheme);
    }

    [Fact]
    public async Task AddExternalSsoAuthentication_RegistersCookieScheme()
    {
        var config = BuildConfig();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternalSsoAuthentication(config);

        var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        var cookieScheme = await schemeProvider.GetSchemeAsync("Cookies");

        Assert.NotNull(cookieScheme);
    }
}
