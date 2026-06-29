using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Invoicing.Infrastructure.Pdf;
using OrderSphere.Invoicing.Infrastructure.Persistence;

namespace OrderSphere.Invoicing.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInvoicingInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<InvoicingDbContext>("invoicing-db");

        builder.Services.AddScoped<IInvoicingDbContext>(sp =>
            sp.GetRequiredService<InvoicingDbContext>());

        builder.Services.AddScoped<IInvoicePdfService, QuestPdfInvoiceService>();

        builder.Services.AddScoped<IInvoiceNumberGenerator, SequentialInvoiceNumberGenerator>();

        builder.Services.AddSingleton(sp => new BlobStorageClients(
            sp.GetRequiredService<IConfiguration>(), "InvoiceBlob:Endpoint", "invoices", "invoices"));

        builder.Services.AddScoped<IBlobStorageService>(sp =>
        {
            var clients = sp.GetRequiredService<BlobStorageClients>();
            return clients.IsEnabled
                ? new AzureBlobStorageService(
                    clients,
                    sp.GetRequiredService<ILogger<AzureBlobStorageService>>())
                : DisabledBlobStorageService.Instance;
        });

        builder.Services.AddAzureServiceBusEventBus();

        return builder;
    }
}
