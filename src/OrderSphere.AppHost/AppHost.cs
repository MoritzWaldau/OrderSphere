var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithPgAdmin()
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
