var builder = DistributedApplication.CreateBuilder(args);

// Keycloak via generic container (Aspire.Hosting.Keycloak not yet stable at 13.3.0).
// Admin UI: http://localhost:8080  admin / admin (dev only)
// Realm file is mounted and imported once on first start via --import-realm.
// After first start, run: contracts/keycloak/seed-dev-passwords.ps1
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.1")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
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
    .WithEnvironment("Keycloak__Audience", "account");

var ordering = builder.AddProject<Projects.OrderSphere_Ordering_Api>("ordersphere-ordering")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WithReference(catalog)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority)
    .WithEnvironment("Keycloak__Audience", "account");

builder.AddProject<Projects.OrderSphere_Ordering_Worker>("ordersphere-ordering-worker")
    .WithReference(orderingDb)
    .WithReference(serviceBus)
    .WaitFor(orderingDb)
    .WaitFor(serviceBus);

builder.AddProject<Projects.OrderSphere_Notification_Worker>("ordersphere-notification-worker")
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

var userProfile = builder.AddProject<Projects.OrderSphere_UserProfile_Api>("ordersphere-userprofile")
    .WithReference(userProfileDb)
    .WaitFor(userProfileDb)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority)
    .WithEnvironment("Keycloak__Audience", "account");

var ui = builder.AddProject<Projects.OrderSphere_UI>("ordersphere-ui")
    .WithReference(postgres)
    .WithReference(serviceBus)
    .WithReference(redis)
    .WithReference(ordering)
    .WaitFor(postgres)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WaitFor(ordering)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority)
    .WithEnvironment("Keycloak__ClientId", "web-bff")
    .WithEnvironment("Keycloak__ClientSecret", "web-bff-dev-secret-change-in-prod");

var apiGateway = builder.AddProject<Projects.OrderSphere_ApiGateway>("ordersphere-apigateway")
    .WithReference(ui)
    .WithReference(catalog)
    .WithReference(ordering)
    .WithReference(userProfile)
    .WaitFor(ui)
    .WaitFor(catalog)
    .WaitFor(ordering)
    .WaitFor(userProfile)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority);

builder.AddProject<Projects.OrderSphere_Bff>("ordersphere-bff")
    .WithReference(apiGateway)
    .WaitFor(apiGateway)
    .WithEnvironment("Keycloak__Authority", keycloakRealmAuthority)
    .WithEnvironment("Keycloak__ClientId", "web-bff")
    .WithEnvironment("Keycloak__ClientSecret", "web-bff-dev-secret-change-in-prod");

builder.Build().Run();
