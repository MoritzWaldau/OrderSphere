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

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithPgAdmin().WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase("ordersphere-db");

var serviceBus = builder.AddAzureServiceBus("azure-service-bus")
    .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent));

serviceBus.AddServiceBusQueue("orders")
    .WithProperties(cfg =>
    {
        cfg.MaxDeliveryCount = 10;
    });

var redis = builder.AddAzureManagedRedis("redis")
    .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent));

builder.AddProject<Projects.OrderSphere_UI>("ordersphere-ui")
    .WithReference(postgres)
    .WithReference(serviceBus)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(serviceBus)
    .WaitFor(redis);

builder.AddProject<Projects.OrderSphere_Worker>("ordersphere-worker")
    .WithReference(postgres)
    .WithReference(serviceBus)
    .WaitFor(postgres)
    .WaitFor(serviceBus);

builder.Build().Run();
