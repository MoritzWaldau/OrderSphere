var builder = DistributedApplication.CreateBuilder(args);

// ── Secret parameters ─────────────────────────────────────────────────────────
// In development: populate via user-secrets (see commands in docs/auth/secrets-rotation.md).
// In production: values are resolved from Azure Key Vault by azd / Aspire provisioning.
var keycloakAdminPwd = builder.AddParameter("keycloak-admin-password",    secret: true);
var bffClientSecret = builder.AddParameter("bff-client-secret",          secret: true);
var orderingWorkerSecret = builder.AddParameter("ordering-worker-secret",      secret: true);
var notificationWorkerSecret = builder.AddParameter("notification-worker-secret", secret: true);
var paymentWorkerSecret = builder.AddParameter("payment-worker-secret",      secret: true);

// Non-secret parameters — defaults provided in appsettings.Development.json.
// In Azure: supply via azd environment parameter or Key Vault.
var paymentBypassProviders = builder.AddParameter("payment-bypass-providers");

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
if (!builder.ExecutionContext.IsPublishMode)
{
    builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.1")
        .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
        .WithEnvironment("KEYCLOAK_ADMIN", "admin")
        .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPwd)
        .WithBindMount("../../contracts/keycloak/ordersphere-realm.json", "/opt/keycloak/data/import/ordersphere-realm.json", isReadOnly: true)
        .WithArgs("start-dev", "--import-realm")
        .WithVolume("keycloak-data", "/opt/keycloak/data")
        .WithLifetime(ContainerLifetime.Persistent);
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

var basket = builder.AddProject<Projects.OrderSphere_Basket_Api>("ordersphere-basket")
    .WithReference(basketDb)
    .WithReference(catalog)
    .WaitFor(basketDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "basket-api")
    .WithEnvironment("Keycloak__ClientId", "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);

var ordering = builder.AddProject<Projects.OrderSphere_Ordering_Api>("ordersphere-ordering")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WithReference(catalog)
    .WithReference(basket)
    .WithReference(redis)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithEnvironment("Keycloak__Authority",   keycloakAuthority)
    .WithEnvironment("Keycloak__Audience",     "ordering-api")
    // Service-account credentials used by HttpCatalogClient (client_credentials grant).
    .WithEnvironment("Keycloak__ClientId",     "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);

builder.AddProject<Projects.OrderSphere_Ordering_Worker>("ordersphere-ordering-worker")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    // Pre-wired for future M2M calls (e.g. Catalog enrichment).
    .WithEnvironment("Keycloak__Authority",   keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId",     "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);

builder.AddProject<Projects.OrderSphere_Notification_Worker>("ordersphere-notification-worker")
    .WithReference(notificationDb)
    .WithReference(serviceBus)
    .WaitFor(notificationDb)
    .WaitFor(serviceBus)
    // Pre-wired for future M2M calls (e.g. UserProfile enrichment).
    .WithEnvironment("Keycloak__Authority",   keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId",     "notification-worker")
    .WithEnvironment("Keycloak__ClientSecret", notificationWorkerSecret);

var payment = builder.AddProject<Projects.OrderSphere_Payment_Api>("ordersphere-payment")
    .WithReference(paymentDb)
    .WithReference(serviceBus)
    .WaitFor(paymentDb)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "payment-api");

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

var webhooks = builder.AddProject<Projects.OrderSphere_Webhooks_Api>("ordersphere-webhooks")
    .WithReference(webhooksDb)
    .WaitFor(webhooksDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "webhooks-api");

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

builder.AddProject<Projects.OrderSphere_Bff>("ordersphere-bff")
    .WithReference(apiGateway)
    .WithReference(redis)
    .WithReference(serviceBus)
    .WaitFor(apiGateway)
    .WaitFor(redis)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority",   keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId",     "web-bff")
    .WithEnvironment("Keycloak__ClientSecret", bffClientSecret);

builder.Build().Run();
