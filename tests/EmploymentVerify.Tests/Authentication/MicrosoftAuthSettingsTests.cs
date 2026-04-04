using EmploymentVerify.Web.Authentication;

namespace EmploymentVerify.Tests.Authentication;

public class MicrosoftAuthSettingsTests
{
    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("Authentication:Microsoft", MicrosoftAuthSettings.SectionName);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var settings = new MicrosoftAuthSettings();

        Assert.Equal(string.Empty, settings.ClientId);
        Assert.Equal(string.Empty, settings.ClientSecret);
        Assert.Equal("common", settings.TenantId);
        Assert.Equal("/signin-microsoft", settings.CallbackPath);
    }

    [Fact]
    public void Authority_IsComputedFromTenantId()
    {
        var settings = new MicrosoftAuthSettings { TenantId = "my-tenant-guid" };

        Assert.Equal("https://login.microsoftonline.com/my-tenant-guid/v2.0", settings.Authority);
    }

    [Fact]
    public void Authority_UsesCommonByDefault()
    {
        var settings = new MicrosoftAuthSettings();

        Assert.Equal("https://login.microsoftonline.com/common/v2.0", settings.Authority);
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenClientIdEmpty()
    {
        var settings = new MicrosoftAuthSettings
        {
            ClientId = "",
            ClientSecret = "secret"
        };

        Assert.False(settings.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenClientSecretEmpty()
    {
        var settings = new MicrosoftAuthSettings
        {
            ClientId = "some-client-id",
            ClientSecret = ""
        };

        Assert.False(settings.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenClientIdIsPlaceholder()
    {
        var settings = new MicrosoftAuthSettings
        {
            ClientId = "your-microsoft-client-id",
            ClientSecret = "secret"
        };

        Assert.False(settings.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue_WhenCredentialsConfigured()
    {
        var settings = new MicrosoftAuthSettings
        {
            ClientId = "real-client-id",
            ClientSecret = "real-secret"
        };

        Assert.True(settings.IsEnabled);
    }
}
