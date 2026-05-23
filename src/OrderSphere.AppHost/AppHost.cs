var builder = DistributedApplication.CreateBuilder(args);

// ── Secret parameters ─────────────────────────────────────────────────────────
// In development: populate via user-secrets (see commands in docs/auth/secrets-rotation.md).
// In production: values are resolved from Azure Key Vault by azd / Aspire provisioning.
var keycloakAdminPwd = builder.AddParameter("keycloak-admin-password",    secret: true);
var bffClientSecret = builder.AddParameter("bff-client-secret",          secret: true);
var orderingWorkerSecret = builder.AddParameter("ordering-worker-secret",      secret: true);
var notificationWorkerSecret = builder.AddParameter("notification-worker-secret", secret: true);

// ── Azure Key Vault ───────────────────────────────────────────────────────────
// Provisioned by azd in non-dev environments. Parameters above are backed by
// Key Vault secrets at deployment time; no code change required in service projects.
builder.AddAzureKeyVault("ordersphere-kv");

// ── Keycloak ──────────────────────────────────────────────────────────────────
// Generic container (Aspire.Hosting.Keycloak not yet stable at 13.3.0).
// Admin UI: http://localhost:8080  admin / <keycloak-admin-password from user-secrets>
// Realm file is mounted and imported once on first start via --import-realm.
// After first start, run: contracts/keycloak/seed-dev-passwords.ps1
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.1")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPwd)
    .WithBindMount("../../contracts/keycloak/ordersphere-realm.json", "/opt/keycloak/data/import/ordersphere-realm.json", isReadOnly: true)
    .WithArgs("start-dev", "--import-realm")
    .WithVolume("keycloak-data", "/opt/keycloak/data")
    .WithLifetime(ContainerLifetime.Persistent);

var postgresServer = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithPgAdmin().WithLifetime(ContainerLifetime.Persistent));

var postgres = postgresServer.AddDatabase("ordersphere-db");
var catalogDb = postgresServer.AddDatabase("catalog-db");
var orderingDb = postgresServer.AddDatabase("ordering-db");
var userProfileDb = postgresServer.AddDatabase("userprofile-db");

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

var redis = builder.AddAzureManagedRedis("redis")
    .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent));

// Keycloak runs in a generic container — Aspire can't model it as a typed resource,
// so we hard-code the realm URL the UI/Gateway/BFF use to validate tokens.
const string keycloakRealmAuthority = "http://localhost:8080/realms/ordersphere";

var catalog = builder.AddProject<Projects.OrderSphere_Catalog_Api>("ordersphere-catalog")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority)
    .WithEnvironment("Keycloak__Audience", "catalog-api");

var ordering = builder.AddProject<Projects.OrderSphere_Ordering_Api>("ordersphere-ordering")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WithReference(catalog)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority",   keycloakRealmAuthority)
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
    .WithEnvironment("Keycloak__Authority",   keycloakRealmAuthority)
    .WithEnvironment("Keycloak__ClientId",     "ordering-worker")
    .WithEnvironment("Keycloak__ClientSecret", orderingWorkerSecret);

builder.AddProject<Projects.OrderSphere_Notification_Worker>("ordersphere-notification-worker")
    .WithReference(serviceBus)
    .WaitFor(serviceBus)
    // Pre-wired for future M2M calls (e.g. UserProfile enrichment).
    .WithEnvironment("Keycloak__Authority",   keycloakRealmAuthority)
    .WithEnvironment("Keycloak__ClientId",     "notification-worker")
    .WithEnvironment("Keycloak__ClientSecret", notificationWorkerSecret);

var userProfile = builder.AddProject<Projects.OrderSphere_UserProfile_Api>("ordersphere-userprofile")
    .WithReference(userProfileDb)
    .WaitFor(userProfileDb)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority)
    .WithEnvironment("Keycloak__Audience", "userprofile-api");

var apiGateway = builder.AddProject<Projects.OrderSphere_ApiGateway>("ordersphere-apigateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WithReference(userProfile)
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WaitFor(userProfile)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority);

builder.AddProject<Projects.OrderSphere_Bff>("ordersphere-bff")
    .WithReference(apiGateway)
    .WithReference(redis)
    .WaitFor(apiGateway)
    .WaitFor(redis)
    .WithEnvironment("Keycloak__Authority",   keycloakRealmAuthority)
    .WithEnvironment("Keycloak__ClientId",     "web-bff")
    .WithEnvironment("Keycloak__ClientSecret", bffClientSecret);

builder.Build().Run();
