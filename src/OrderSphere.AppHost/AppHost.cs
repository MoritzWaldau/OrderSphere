var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.OrderSphere_UI>("ordersphere-ui");

builder.Build().Run();
