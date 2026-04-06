var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithPgAdmin()
    .AddDatabase("ordersphere-db");

var serviceBus = builder.AddAzureServiceBus("azure-service-bus")
    .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent));

serviceBus.AddServiceBusQueue("orders")
    .WithProperties(cfg =>
    { 
        cfg.MaxDeliveryCount = 1;
    });

builder.AddProject<Projects.OrderSphere_UI>("ordersphere-ui")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();
