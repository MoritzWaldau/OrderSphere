using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Shared wiring for the <c>WebApplicationFactory</c>-based API integration tests: swapping the
/// relational <see cref="DbContext"/> for an in-memory one and installing the <see cref="TestAuthHandler"/>
/// in place of production JWT Bearer. Each per-service factory composes these.
/// </summary>
internal static class ApiTestHostExtensions
{
    /// <summary>
    /// Configuration every test host needs so that production startup wiring (which reads OIDC and
    /// connection-string keys eagerly) builds without external infrastructure.
    /// </summary>
    public static Dictionary<string, string?> BaseConfig(params (string Key, string Value)[] extra)
    {
        var config = new Dictionary<string, string?>
        {
            // AddOrderSphereJwtAuth throws when these are absent; the values are never used because
            // the Test auth scheme replaces the Bearer handler.
            ["Oidc:Authority"] = "https://test-authority.local/",
            ["Oidc:Audience"] = "test-audience",
            // Aspire's AddNpgsqlDbContext reads the connection string at registration; the relational
            // context is replaced with InMemory before any query runs.
            ["ConnectionStrings:catalog-db"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["ConnectionStrings:basket-db"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["ConnectionStrings:ordering-db"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["ConnectionStrings:payment-db"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["ConnectionStrings:userprofile-db"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["ConnectionStrings:webhooks-db"] = "Host=localhost;Database=test;Username=test;Password=test",
            // Redis: abortConnect=false lets the multiplexer construct without a live server; the
            // HybridCache L2 simply degrades to its in-memory L1.
            ["ConnectionStrings:redis"] = "localhost:6379,abortConnect=false,connectTimeout=200",
            // A SAS-form Service Bus connection string constructs the client lazily (no network, no
            // credential lookup); background dispatchers that would use it are stripped in tests.
            ["ConnectionStrings:azure-service-bus"] =
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=dGVzdA==",
        };

        foreach (var (key, value) in extra)
            config[key] = value;

        return config;
    }

    /// <summary>
    /// Applies the test configuration through <c>UseSetting</c> rather than <c>ConfigureAppConfiguration</c>:
    /// connection strings and OIDC keys are read synchronously while the production builder runs
    /// (before <c>app.Build()</c>), so they must already be present in <c>builder.Configuration</c> at
    /// that point — which only <c>UseSetting</c> guarantees.
    /// </summary>
    public static void ApplyTestConfig(this IWebHostBuilder builder, params (string Key, string Value)[] extra)
    {
        foreach (var (key, value) in BaseConfig(extra))
            builder.UseSetting(key, value);
    }

    /// <summary>Replaces the relational registration of <typeparamref name="TContext"/> with a uniquely-named in-memory store.</summary>
    public static void UseInMemoryDb<TContext>(this IServiceCollection services, string databaseName)
        where TContext : DbContext
    {
        services.RemoveRelationalRegistrations<TContext>();
        services.AddDbContext<TContext>(options => options
            .UseInMemoryDatabase(databaseName)
            // Handlers that open an explicit transaction would otherwise throw on the non-relational provider.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
    }

    /// <summary>
    /// Replaces the relational registration of <typeparamref name="TContext"/> with an in-memory SQLite
    /// database. Unlike the EF InMemory provider, SQLite is relational and therefore materialises owned
    /// types (e.g. <c>Money</c>) and value converters — required for the aggregates that project them.
    /// The schema is created from the model once the host starts.
    /// </summary>
    public static void UseSqliteDb<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.RemoveRelationalRegistrations<TContext>();

        // A single open connection keeps the in-memory database alive for the host's lifetime.
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddSingleton(connection);
        services.AddDbContext<TContext>(options => options.UseSqlite(connection));
        services.AddHostedService<SqliteSchemaInitializer<TContext>>();
    }

    private static void RemoveRelationalRegistrations<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        // Aspire's AddNpgsqlDbContext registers the options, the context, an IDbContextOptionsConfiguration
        // (EF Core 9+) and an NpgsqlDataSource. Every one of these must go, otherwise the Npgsql provider
        // stays configured alongside the replacement and EF throws "only a single provider" at query time.
        var toRemove = services.Where(d =>
            d.ServiceType == typeof(DbContextOptions<TContext>)
            || d.ServiceType == typeof(DbContextOptions)
            || d.ServiceType == typeof(TContext)
            || d.ServiceType.FullName is { } name &&
               (name.Contains("IDbContextOptionsConfiguration") || name.Contains("NpgsqlDataSource")))
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);
    }

    /// <summary>Swaps every authentication handler for the header-driven <see cref="TestAuthHandler"/>.</summary>
    public static void AddTestAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
    }

    /// <summary>
    /// Removes all hosted/background services. API integration tests exercise request handling only;
    /// outbox dispatchers, reservation sweepers and seeders would otherwise start and reach for
    /// infrastructure (Service Bus, Redis, blob/search) that is not present.
    /// </summary>
    public static void RemoveHostedServices(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
            services.Remove(descriptor);
    }
}
