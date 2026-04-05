using EmploymentVerify.Infrastructure.Authorization;
using EmploymentVerify.Web.Authentication;
using EmploymentVerify.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Bearer token handler — attaches JWT from cookie claims to every API request
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

// Configure HttpClient for API calls
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001";
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<BearerTokenHandler>();

// Add controllers for OAuth callback endpoints
builder.Services.AddControllers();

// Configure external SSO authentication (Google + Microsoft)
builder.Services.AddExternalSsoAuthentication(builder.Configuration);

// Configure role-based authorization policies
builder.Services.AddAppAuthorization();

// Bind auth settings for injection
builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection(GoogleAuthSettings.SectionName));
builder.Services.Configure<MicrosoftAuthSettings>(
    builder.Configuration.GetSection(MicrosoftAuthSettings.SectionName));

var app = builder.Build();

// Log whether external SSO providers are configured
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
if (GoogleAuthenticationExtensions.IsGoogleAuthConfigured(builder.Configuration))
{
    logger.LogInformation("Google SSO is configured and enabled");
}
else
{
    logger.LogWarning("Google SSO is NOT configured. Set Authentication:Google:ClientId and ClientSecret to enable");
}

if (MicrosoftAuthenticationExtensions.IsMicrosoftAuthConfigured(builder.Configuration))
{
    logger.LogInformation("Microsoft SSO is configured and enabled");
}
else
{
    logger.LogWarning("Microsoft SSO is NOT configured. Set Authentication:Microsoft:ClientId and ClientSecret to enable");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // Maps OAuth callback controller routes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
