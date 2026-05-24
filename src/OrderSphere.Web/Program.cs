using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OrderSphere.Web.Auth;
using OrderSphere.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<OrderSphere.Web.App>("#app");

// CSRF protection services
builder.Services.AddScoped<CsrfTokenService>();
builder.Services.AddScoped<AntiforgeryDelegatingHandler>();

// Base HttpClient — all API calls go to the same BFF origin.
// AntiforgeryDelegatingHandler attaches X-XSRF-TOKEN on all mutating requests.
builder.Services.AddScoped(sp =>
{
    var csrfService = sp.GetRequiredService<CsrfTokenService>();
    var antiforgeryHandler = new AntiforgeryDelegatingHandler(csrfService)
    {
        InnerHandler = new HttpClientHandler()
    };
    return new HttpClient(antiforgeryHandler)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
});

// Auth — BFF cookie pattern: state is derived from /bff/user
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, BffAuthStateProvider>();

// API clients
builder.Services.AddScoped<ICatalogClient, CatalogClient>();
builder.Services.AddScoped<IOrderingClient, OrderingClient>();
builder.Services.AddScoped<IUserProfileClient, UserProfileClient>();

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
