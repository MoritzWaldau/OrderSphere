using System.Reflection;
using System.Text.Json.Serialization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string VersionEndpointPath = "/version";

    // Same version source as the /version endpoint; stamped onto every telemetry signal as service.version.
    private static readonly string ServiceVersion =
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? "unknown";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // EF Core logs every SQL command at Information by default. Default it to Warning so the
        // database story comes from traces (DB spans), not log spam — raise the
        // "Microsoft.EntityFrameworkCore.Database.Command" category where raw SQL is needed.
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

        builder.Services.AddOpenTelemetry()
            // Resource identity: service.name, service.version and deployment.environment make every
            // signal filterable by service and environment in the Aspire dashboard / App Insights.
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: ServiceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
                ]))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Custom OrderSphere business metrics — single meter name across all services.
                    .AddMeter("OrderSphere");
            })
            .WithTracing(tracing =>
            {
                // Parent-based ratio sampler; ratio from "OpenTelemetry:TracesSampleRatio"
                // (default 1.0 = sample everything). Lower it in production to control cost.
                // Azure Monitor applies its own sampler via APPLICATIONINSIGHTS_SAMPLING_PERCENTAGE.
                var sampleRatio = double.TryParse(
                    builder.Configuration["OpenTelemetry:TracesSampleRatio"], out var ratio) ? ratio : 1.0;

                tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(sampleRatio)))
                    .AddSource(builder.Environment.ApplicationName)
                    // Event-bus publish/consume spans (string literal avoids coupling
                    // ServiceDefaults to the EF-bound EventBus.AzureServiceBus project).
                    .AddSource("OrderSphere.EventBus")
                    // Per-request (MediatR/CQRS handler) spans from the LoggingBehavior.
                    .AddSource("OrderSphere.Application")
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                            && !context.Request.Path.StartsWithSegments(VersionEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Liveness — the app process is running and responsive.
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        // Readiness dependency checks are registered by the Aspire client integrations:
        // Aspire.Npgsql.EntityFrameworkCore.PostgreSQL (per-service DB), Aspire.StackExchange.Redis,
        // and Aspire.Azure.Messaging.ServiceBus each register their own check under the resource
        // name. There is no shared "ordersphere-db" connection, so no generic DB check is added here.

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks(HealthEndpointPath);

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        // Expose the compiled product version (from VersionPrefix in Directory.Build.props).
        app.MapGet(VersionEndpointPath, () =>
        {
            var assembly = Assembly.GetEntryAssembly();
            var informational = assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return Results.Ok(new
            {
                service = assembly?.GetName().Name,
                version = informational ?? assembly?.GetName().Version?.ToString()
            });
        })
        .WithName("Version")
        .ExcludeFromDescription();

        return app;
    }
}
