using Azure.Provisioning.Search;

var builder = DistributedApplication.CreateBuilder(args);

// In development: populate via dotnet user-secrets set "Parameters:<name>" "<value>" --project src/OrderSphere.AppHost
// In production: values are resolved from Azure Key Vault by azd / Aspire provisioning.
var bffClientSecret = builder.AddParameter("bff-client-secret", secret: true);
var orderingWorkerSecret = builder.AddParameter("ordering-worker-secret", secret: true);
var notificationWorkerSecret = builder.AddParameter("notification-worker-secret", secret: true);
var paymentWorkerSecret = builder.AddParameter("payment-worker-secret", secret: true);

// Stripe (test mode) — optional. Resolved from AppHost configuration / user-secrets so local
// runs work without it; when unset the Payment service falls back to the simulated provider.
// Set locally via:
//   dotnet user-secrets set "Stripe:ApiKey" "sk_test_..." --project src/Hosting/OrderSphere.AppHost
//   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/Hosting/OrderSphere.AppHost
var stripeApiKey = builder.Configuration["Stripe:ApiKey"] ?? "";
var stripeWebhookSecret = builder.Configuration["Stripe:WebhookSecret"] ?? "";

// Non-secret parameters — defaults provided in appsettings.Development.json.
// In Azure: supply via azd environment parameter or Key Vault.
var paymentBypassProviders = builder.AddParameter("payment-bypass-providers");

// Azure Foundry / Azure OpenAI for the advisory agent. Authentication uses
// managed identity (DefaultAzureCredential) — no API key required. Endpoint and
// model deployment are optional: when unset the advisory service degrades
// gracefully (reports unavailable) so local runs work without Azure. Set locally
// via: dotnet user-secrets set "Foundry:Endpoint" "https://...".
var foundryEndpoint = builder.Configuration["Foundry:Endpoint"] ?? "";
var foundryDeployment = builder.Configuration["Foundry:Deployment"] ?? "gpt-4o-mini";
// Embedding model for catalog hybrid search; same Foundry endpoint, separate deployment.
var foundryEmbeddingDeployment = builder.Configuration["Foundry:EmbeddingDeployment"] ?? "text-embedding-3-small";

// Azure Cognitive Services Speech for the voice advisor (C8).
// Local runs: dotnet user-secrets set "Speech:Region" "<region>"
//             dotnet user-secrets set "Speech:SubscriptionKey" "<key>"
// In Azure: Speech:SubscriptionKey should be stored in Key Vault.
var speechRegion = builder.Configuration["Speech:Region"] ?? "";
var speechKey = builder.Configuration["Speech:SubscriptionKey"] ?? "";

// Azure App Configuration for versioned prompt management (C9).
// Local runs: dotnet user-secrets set "AppConfiguration:Endpoint" "https://<name>.azconfig.io"
//             dotnet user-secrets set "AppConfiguration:Label"    "v2"   # optional
// In Azure: the endpoint is provisioned by azd; no explicit secret needed (managed identity).
var appConfigEndpoint = builder.Configuration["AppConfiguration:Endpoint"] ?? "";
var appConfigLabel = builder.Configuration["AppConfiguration:Label"] ?? "";
// Fallback model deployment (optional): advisory retries on network errors against this deployment.
// dotnet user-secrets set "Foundry:FallbackDeployment" "gpt-4o"
var foundryFallbackDeployment = builder.Configuration["Foundry:FallbackDeployment"] ?? "";

// Provisioned by azd in non-dev environments. Parameters above are backed by
// Key Vault secrets at deployment time; no code change required in service projects.
builder.AddAzureKeyVault("ordersphere-kv");

// Application Insights is provisioned and wired after all resources are declared (see bottom).

// Auth0 authority URL for all services. Local default is in appsettings.Development.json;
// in Azure the value is provided via Key Vault / azd parameter.
var oidcAuthority = builder.AddParameter("oidc-authority");

const string OidcAudience = "https://api.ordersphere.dev";

// Deploy as container in all environments — avoids Azure PostgreSQL Flexible Server
// offer restrictions on VS Enterprise subscriptions. pgAdmin is only added locally.
var postgresServer = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

if (!builder.ExecutionContext.IsPublishMode)
    postgresServer.WithPgAdmin();

var postgres = postgresServer.AddDatabase("ordersphere-db");
var catalogDb = postgresServer.AddDatabase("catalog-db");
var orderingDb = postgresServer.AddDatabase("ordering-db");
var basketDb = postgresServer.AddDatabase("basket-db");
var paymentDb = postgresServer.AddDatabase("payment-db");
var userProfileDb = postgresServer.AddDatabase("userprofile-db");
var webhooksDb = postgresServer.AddDatabase("webhooks-db");
var notificationDb = postgresServer.AddDatabase("notification-db");
var advisoryDb = postgresServer.AddDatabase("advisory-db");

var serviceBus = builder.AddAzureServiceBus("azure-service-bus")
    .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent));

serviceBus.AddServiceBusQueue("orders")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 10;
    });

serviceBus.AddServiceBusQueue("notification-orders")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

serviceBus.AddServiceBusQueue("payment-requests")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 10;
    });

serviceBus.AddServiceBusQueue("payment-results")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

serviceBus.AddServiceBusQueue("realtime-notifications")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

// Saga compensation loop: Ordering → Payment (refund a captured-but-unconfirmable order),
// Payment → Ordering (refund confirmed → saga terminal).
serviceBus.AddServiceBusQueue("order-confirmation-failed")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

serviceBus.AddServiceBusQueue("payment-refunds")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

// Returns/RMA loop: Ordering → Payment (refund an approved return request).
serviceBus.AddServiceBusQueue("refund-requested")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

serviceBus.AddServiceBusQueue("webhook-events")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

// Order-history read-model projection (A3 — CQRS materialised view). Fanned out from the
// Ordering outbox alongside webhook-events; consumed by the OrderHistoryProjector worker.
serviceBus.AddServiceBusQueue("order-history")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 5;
    });

var redis = builder.AddAzureManagedRedis("redis")
    .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent));


var catalog = builder.AddProject<Projects.OrderSphere_Catalog_Api>("ordersphere-catalog")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience);

var basket = builder.AddProject<Projects.OrderSphere_Basket_Api>("ordersphere-basket")
    .WithReference(basketDb)
    .WithReference(catalog)
    .WaitFor(basketDb)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience)
    .WithEnvironment("Oidc__ClientId", "xY2Mgok7H98OsgFswj8JLC0gcgA6Oegy")
    .WithEnvironment("Oidc__ClientSecret", orderingWorkerSecret);

var ordering = builder.AddProject<Projects.OrderSphere_Ordering_Api>("ordersphere-ordering")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WithReference(catalog)
    .WithReference(basket)
    .WithReference(redis)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience)
    .WithEnvironment("Oidc__ClientId", "xY2Mgok7H98OsgFswj8JLC0gcgA6Oegy")
    .WithEnvironment("Oidc__ClientSecret", orderingWorkerSecret);

// Catalog calls Ordering's internal purchase-verification endpoint for review eligibility.
// Declared after `ordering` exists; service discovery injects Services__Ordering__BaseUrl.
catalog.WithReference(ordering);

// Azure AI Search for catalog hybrid (keyword + vector) search, with the Foundry
// embedding deployment. AI Search has no local emulator: it is provisioned only in
// publish mode (azd), and the reference injects ConnectionStrings__search. Local runs
// read Search__Endpoint from config (empty by default) and degrade to database search.
catalog
    .WithEnvironment("Foundry__Endpoint", foundryEndpoint)
    .WithEnvironment("Foundry__EmbeddingDeployment", foundryEmbeddingDeployment);

if (builder.ExecutionContext.IsPublishMode)
{
    // Free tier: zero-cost for DEV/learning. Note its limits (small vector quota,
    // limited index count, no semantic ranker) — move to Basic for production-like load.
    var search = builder.AddAzureSearch("search")
        .ConfigureInfrastructure(infra =>
        {
            var service = infra.GetProvisionableResources().OfType<SearchService>().Single();
            service.SearchSkuName = SearchServiceSkuName.Free;
        });

    // WithReference grants the catalog identity SearchServiceContributor +
    // SearchIndexDataContributor by default (azd generates the role assignments in this
    // legacy container-app-environment model). The catalog needs both: ServiceContributor
    // to create the index (CatalogSearchInitializer), IndexDataContributor to write/query
    // documents (AzureAiProductSearchIndex). Explicit WithRoleAssignments is unsupported
    // here — it requires AddAzureContainerAppEnvironment, which this AppHost does not use.
    catalog.WithReference(search);

    // Azure Blob Storage for catalog product images (private container, SAS URLs).
    // azd auto-generates Storage Blob Data Contributor on the catalog identity via WithReference.
    var storage = builder.AddAzureStorage("storage");
    var images = storage.AddBlobs("images");
    catalog.WithReference(images);
}
else
{
    catalog.WithEnvironment("Search__Endpoint", builder.Configuration["Search:Endpoint"] ?? "");
    catalog.WithEnvironment("Blob__Endpoint", builder.Configuration["Blob:Endpoint"] ?? "");
}

var orderingWorker = builder.AddProject<Projects.OrderSphere_Ordering_Worker>("ordersphere-ordering-worker")
    .WithHttpsEndpoint()
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WithReference(catalog)
    .WithReference(redis)
    .WaitFor(ordering)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience)
    .WithEnvironment("Oidc__ClientId", "xY2Mgok7H98OsgFswj8JLC0gcgA6Oegy")
    .WithEnvironment("Oidc__ClientSecret", orderingWorkerSecret);

var notificationWorker = builder.AddProject<Projects.OrderSphere_Notification_Worker>("ordersphere-notification-worker")
    .WithHttpsEndpoint()
    .WithReference(notificationDb)
    .WithReference(serviceBus)
    .WaitFor(notificationDb)
    .WaitFor(serviceBus)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__ClientId", "xAGJ3VxnOenpai2dGgkTId3dWBhOXMqz")
    .WithEnvironment("Oidc__ClientSecret", notificationWorkerSecret);

var payment = builder.AddProject<Projects.OrderSphere_Payment_Api>("ordersphere-payment")
    .WithReference(paymentDb)
    .WithReference(serviceBus)
    .WithReference(redis)
    .WaitFor(paymentDb)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience)
    .WithEnvironment("Stripe__ApiKey", stripeApiKey)
    .WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret);

var paymentWorker = builder.AddProject<Projects.OrderSphere_Payment_Worker>("ordersphere-payment-worker")
    .WithHttpsEndpoint()
    .WithReference(paymentDb)
    .WithReference(serviceBus)
    .WithReference(redis)
    .WaitFor(paymentDb)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__ClientId", "ub8w9SMhhUZddhRGxG2xcNU96Wx87csW")
    .WithEnvironment("Oidc__ClientSecret", paymentWorkerSecret)
    .WithEnvironment("Payment__BypassProviders", paymentBypassProviders)
    .WithEnvironment("Stripe__ApiKey", stripeApiKey);

var userProfile = builder.AddProject<Projects.OrderSphere_UserProfile_Api>("ordersphere-userprofile")
    .WithReference(userProfileDb)
    .WaitFor(userProfileDb)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience);

// Notification worker fetches per-user channel preferences from UserProfile on each event.
notificationWorker
    .WithReference(userProfile)
    .WaitFor(userProfile);

var webhooks = builder.AddProject<Projects.OrderSphere_Webhooks_Api>("ordersphere-webhooks")
    .WithReference(webhooksDb)
    .WaitFor(webhooksDb)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__Audience", OidcAudience);

var webhooksWorker = builder.AddProject<Projects.OrderSphere_Webhooks_Worker>("ordersphere-webhooks-worker")
    .WithHttpsEndpoint()
    .WithReference(webhooksDb)
    .WithReference(serviceBus)
    .WithReference(redis)
    .WaitFor(webhooksDb)
    .WaitFor(serviceBus)
    .WaitFor(redis);

var apiGateway = builder.AddProject<Projects.OrderSphere_ApiGateway>("ordersphere-apigateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WithReference(basket)
    .WithReference(payment)
    .WithReference(userProfile)
    .WithReference(webhooks)
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WaitFor(basket)
    .WaitFor(payment)
    .WaitFor(userProfile)
    .WaitFor(webhooks)
    .WithEnvironment("Oidc__Authority", oidcAuthority);

// MCP server: exposes OrderSphere data as Model Context Protocol tools over Streamable
// HTTP. Consumed by the internal advisory agent and by external MCP clients. Calls the
// API Gateway with the caller's forwarded bearer token (user-scoped tools).
var mcpServer = builder.AddProject<Projects.OrderSphere_Mcp_Server>("ordersphere-mcp")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway)
    .WithEnvironment("Oidc__Authority", oidcAuthority);

// Advisory agent: Azure OpenAI/Foundry-backed chat that reaches OrderSphere data
// exclusively through the MCP server, forwarding the end-user's bearer token.
var advisory = builder.AddProject<Projects.OrderSphere_Advisory_Api>("ordersphere-advisory")
    .WithReference(mcpServer)
    .WithReference(advisoryDb)
    .WithReference(redis)
    .WaitFor(mcpServer)
    .WaitFor(advisoryDb)
    .WaitFor(redis)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Foundry__Endpoint", foundryEndpoint)
    .WithEnvironment("Foundry__Deployment", foundryDeployment)
    .WithEnvironment("Speech__Region", speechRegion)
    .WithEnvironment("Speech__SubscriptionKey", speechKey)
    .WithEnvironment("AppConfiguration__Endpoint", appConfigEndpoint)
    .WithEnvironment("AppConfiguration__Label", appConfigLabel)
    .WithEnvironment("Foundry__FallbackDeployment", foundryFallbackDeployment)
    // Concrete endpoint reference, not the logical name: the MCP client transport
    // uses a plain HttpClient without Aspire service discovery.
    .WithEnvironment("Services__Mcp__BaseUrl", mcpServer.GetEndpoint("https"));

// The gateway proxies advisor traffic like every other service. Wired after declaration
// (and without WaitFor) to break the apiGateway → advisory → mcp → apiGateway cycle:
// the gateway only needs advisory's address for service discovery, not its startup.
apiGateway.WithReference(advisory);

var bff = builder.AddProject<Projects.OrderSphere_Bff>("ordersphere-bff")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WithReference(redis)
    .WithReference(serviceBus)
    .WithReference(userProfile)
    .WaitFor(apiGateway)
    .WaitFor(redis)
    .WaitFor(serviceBus)
    .WaitFor(userProfile)
    .WithEnvironment("Oidc__Authority", oidcAuthority)
    .WithEnvironment("Oidc__ClientId", "B70xhPsEf7EBrKbpZiUZHoXmBIATbrDO")
    .WithEnvironment("Oidc__ClientSecret", bffClientSecret);

// Provisioned only in Azure (publish mode / azd up). Locally, OTEL signals flow
// to the Aspire dashboard via the OTLP exporter — no Azure connection required.
// WithReference injects APPLICATIONINSIGHTS_CONNECTION_STRING so the Azure Monitor
// exporter in ServiceDefaults activates automatically without per-service changes.
if (builder.ExecutionContext.IsPublishMode)
{
    var appInsights = builder.AddAzureApplicationInsights("appinsights");
    catalog.WithReference(appInsights);
    basket.WithReference(appInsights);
    ordering.WithReference(appInsights);
    orderingWorker.WithReference(appInsights);
    notificationWorker.WithReference(appInsights);
    payment.WithReference(appInsights);
    paymentWorker.WithReference(appInsights);
    userProfile.WithReference(appInsights);
    webhooks.WithReference(appInsights);
    webhooksWorker.WithReference(appInsights);
    apiGateway.WithReference(appInsights);
    mcpServer.WithReference(appInsights);
    advisory.WithReference(appInsights);
    bff.WithReference(appInsights);
}

builder.Build().Run();
