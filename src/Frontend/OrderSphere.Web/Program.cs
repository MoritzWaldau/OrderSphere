using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;
using OrderSphere.Web.Auth;
using OrderSphere.Web.Services;
using Polly;

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<OrderSphere.Web.App>("#app");

// CSRF protection service.
builder.Services.AddScoped<CsrfTokenService>();

// HTTP message handlers (transient so the HttpClientFactory places one per client).
builder.Services.AddTransient<AntiforgeryDelegatingHandler>();
builder.Services.AddTransient<LoggingHandler>();

var apiBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

// "api" — all REST calls. Handlers run in registration order (outer→inner): logging,
// resilience (retry/circuit-breaker/timeout), CSRF. Only idempotent GETs are retried;
// mutations are never replayed.
var apiClient = builder.Services.AddHttpClient("api", client => client.BaseAddress = apiBaseAddress);
apiClient.AddHttpMessageHandler<LoggingHandler>();
apiClient.AddResilienceHandler("api-pipeline", ConfigureResilience);
apiClient.AddHttpMessageHandler<AntiforgeryDelegatingHandler>();

// "advisor" — chat/SSE. Deliberately no resilience: a request timeout would abort the
// streamed token response.
var advisorClient = builder.Services.AddHttpClient("advisor", client => client.BaseAddress = apiBaseAddress);
advisorClient.AddHttpMessageHandler<LoggingHandler>();
advisorClient.AddHttpMessageHandler<AntiforgeryDelegatingHandler>();

// Inject the "api" client wherever a plain HttpClient is requested.
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

// Auth — BFF cookie pattern: state is derived from /bff/user
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, BffAuthStateProvider>();

// API clients
builder.Services.AddScoped<ICatalogClient, CatalogClient>();
builder.Services.AddScoped<IOrderingClient, OrderingClient>();
builder.Services.AddScoped<IUserProfileClient, UserProfileClient>();
builder.Services.AddScoped<IAdvisorClient, AdvisorClient>();

// Admin clients
builder.Services.AddScoped<IAdminCatalogClient, AdminCatalogClient>();
builder.Services.AddScoped<IAdminOrderingClient, AdminOrderingClient>();
builder.Services.AddScoped<IAdminUserClient, AdminUserClient>();

// Application state
builder.Services.AddScoped<CartState>();
builder.Services.AddScoped<NotificationHubClient>();
builder.Services.AddSingleton<ThemeState>();

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 8000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Outlined;
});

await builder.Build().RunAsync();

// Resilience pipeline for the "api" client: retry idempotent GETs on server errors,
// trip a circuit breaker on sustained failures, and cap each attempt at 30s.
static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> pipeline)
{
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 2,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(300),
        // The request method is unknown for transport exceptions, so those are not
        // retried — a non-idempotent mutation must never be replayed.
        ShouldHandle = static args =>
        {
            var method = args.Outcome.Result?.RequestMessage?.Method;
            var idempotent = method == HttpMethod.Get || method == HttpMethod.Head;
            return ValueTask.FromResult(
                idempotent && args.Outcome.Result is { } response && (int)response.StatusCode >= 500);
        },
    });

    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 10,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(15),
    });

    pipeline.AddTimeout(TimeSpan.FromSeconds(30));
}
