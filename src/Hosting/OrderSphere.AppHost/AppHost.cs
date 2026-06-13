var builder = DistributedApplication.CreateBuilder(args);

// ── Secret parameters ─────────────────────────────────────────────────────────
// In development: populate via dotnet user-secrets set "Parameters:<name>" "<value>" --project src/OrderSphere.AppHost
// In production: values are resolved from Azure Key Vault by azd / Aspire provisioning.
var bffClientSecret = builder.AddParameter("bff-client-secret", secret: true);
var orderingWorkerSecret = builder.AddParameter("ordering-worker-secret", secret: true);
var notificationWorkerSecret = builder.AddParameter("notification-worker-secret", secret: true);
var paymentWorkerSecret = builder.AddParameter("payment-worker-secret", secret: true);

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

// ── Azure Key Vault ───────────────────────────────────────────────────────────
// Provisioned by azd in non-dev environments. Parameters above are backed by
// Key Vault secrets at deployment time; no code change required in service projects.
builder.AddAzureKeyVault("ordersphere-kv");

// ── Keycloak ──────────────────────────────────────────────────────────────────
// In run mode (local dev): generic container started automatically.
//   Admin UI: http://localhost:8080  admin / <keycloak-admin-password from user-secrets>
//   Realm file is mounted and imported once on first start via --import-realm.
//   After first start, run: contracts/keycloak/seed-dev-passwords.ps1
// In publish mode (Azure): Keycloak is externally hosted; container is excluded.
IResourceBuilder<ContainerResource>? keycloak = null;

if (!builder.ExecutionContext.IsPublishMode)
{
    // Declared inside the run-mode guard: the admin password is only consumed by the
    // local Keycloak container. Declaring it at the top level would make it a required
    // secure input of the published Azure manifest, where Keycloak is externally hosted
    // and the value is unused — forcing azd to demand a value it never applies.
    var keycloakAdminPwd = builder.AddParameter("keycloak-admin-password", secret: true);

    keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.1")
        .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
        .WithEnvironment("KEYCLOAK_ADMIN", "admin")
        .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPwd)
        .WithBindMount("../../../contracts/keycloak/ordersphere-realm.json", "/opt/keycloak/data/import/ordersphere-realm.json", isReadOnly: true)
        .WithArgs("start-dev", "--import-realm")
        .WithVolume("keycloak-data", "/opt/keycloak/data")
        .WithLifetime(ContainerLifetime.Persistent)
        // Wait until the realm is fully imported before any service starts JWT validation.
        .WithHttpHealthCheck("/realms/ordersphere/.well-known/openid-configuration", endpointName: "http");
}

// Authority URL for all services. Local default is in appsettings.Development.json;
// in Azure the value is provided via Key Vault / azd parameter.
var keycloakAuthority = builder.AddParameter("keycloak-realm-authority");

var postgresServer = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithPgAdmin().WithLifetime(ContainerLifetime.Persistent));

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

serviceBus.AddServiceBusQueue("webhook-events")
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
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "catalog-api");
if (keycloak is not null) catalog.WaitFor(keycloak);

var basket = builder.AddProject<Projects.OrderSphere_Basket_Api>("ordersphere-basket")
    .WithReference(basketDb)
    .WithReference(catalog)
    .WaitFor(basketDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "basket-api")
    .WithEnvironment("Keycloak__ClientId", "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);
if (keycloak is not null) basket.WaitFor(keycloak);

var ordering = builder.AddProject<Projects.OrderSphere_Ordering_Api>("ordersphere-ordering")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WithReference(catalog)
    .WithReference(basket)
    .WithReference(redis)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "ordering-api")
    // Service-account credentials used by HttpCatalogClient (client_credentials grant).
    .WithEnvironment("Keycloak__ClientId", "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);
if (keycloak is not null) ordering.WaitFor(keycloak);

builder.AddProject<Projects.OrderSphere_Ordering_Worker>("ordersphere-ordering-worker")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    // Pre-wired for future M2M calls (e.g. Catalog enrichment).
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);

builder.AddProject<Projects.OrderSphere_Notification_Worker>("ordersphere-notification-worker")
    .WithReference(notificationDb)
    .WithReference(serviceBus)
    .WaitFor(notificationDb)
    .WaitFor(serviceBus)
    // Pre-wired for future M2M calls (e.g. UserProfile enrichment).
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "notification-worker")
    .WithEnvironment("Keycloak__ClientSecret", notificationWorkerSecret);

var payment = builder.AddProject<Projects.OrderSphere_Payment_Api>("ordersphere-payment")
    .WithReference(paymentDb)
    .WithReference(serviceBus)
    .WaitFor(paymentDb)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "payment-api");
if (keycloak is not null) payment.WaitFor(keycloak);

builder.AddProject<Projects.OrderSphere_Payment_Worker>("ordersphere-payment-worker")
    .WithReference(paymentDb)
    .WithReference(serviceBus)
    .WaitFor(paymentDb)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "payment-worker")
    .WithEnvironment("Keycloak__ClientSecret", paymentWorkerSecret)
    .WithEnvironment("Payment__BypassProviders", paymentBypassProviders);

var userProfile = builder.AddProject<Projects.OrderSphere_UserProfile_Api>("ordersphere-userprofile")
    .WithReference(userProfileDb)
    .WaitFor(userProfileDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "userprofile-api");
if (keycloak is not null) userProfile.WaitFor(keycloak);

var webhooks = builder.AddProject<Projects.OrderSphere_Webhooks_Api>("ordersphere-webhooks")
    .WithReference(webhooksDb)
    .WaitFor(webhooksDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "webhooks-api");
if (keycloak is not null) webhooks.WaitFor(keycloak);

builder.AddProject<Projects.OrderSphere_Webhooks_Worker>("ordersphere-webhooks-worker")
    .WithReference(webhooksDb)
    .WithReference(serviceBus)
    .WaitFor(webhooksDb)
    .WaitFor(serviceBus);

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
    .WithEnvironment("Keycloak__Authority", keycloakAuthority);
if (keycloak is not null) apiGateway.WaitFor(keycloak);

// MCP server: exposes OrderSphere data as Model Context Protocol tools over Streamable
// HTTP. Consumed by the internal advisory agent and by external MCP clients. Calls the
// API Gateway with the caller's forwarded bearer token (user-scoped tools).
var mcpServer = builder.AddProject<Projects.OrderSphere_Mcp_Server>("ordersphere-mcp")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority);
if (keycloak is not null) mcpServer.WaitFor(keycloak);

// Advisory agent: Azure OpenAI/Foundry-backed chat that reaches OrderSphere data
// exclusively through the MCP server, forwarding the end-user's bearer token.
var advisory = builder.AddProject<Projects.OrderSphere_Advisory_Api>("ordersphere-advisory")
    .WithReference(mcpServer)
    .WithReference(advisoryDb)
    .WaitFor(mcpServer)
    .WaitFor(advisoryDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Foundry__Endpoint", foundryEndpoint)
    .WithEnvironment("Foundry__Deployment", foundryDeployment)
    // Concrete endpoint reference, not the logical name: the MCP client transport
    // uses a plain HttpClient without Aspire service discovery.
    .WithEnvironment("Services__Mcp__BaseUrl", mcpServer.GetEndpoint("http"));
if (keycloak is not null) advisory.WaitFor(keycloak);

builder.AddProject<Projects.OrderSphere_Bff>("ordersphere-bff")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WithReference(advisory)
    .WithReference(redis)
    .WithReference(serviceBus)
    .WaitFor(apiGateway)
    .WaitFor(advisory)
    .WaitFor(redis)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "web-bff")
    .WithEnvironment("Keycloak__ClientSecret", bffClientSecret);

builder.Build().Run();
