using Microsoft.Extensions.Options;

namespace OrderSphere.Invoicing.Infrastructure.Persistence;

public sealed class ConfiguredInvoiceTaxRateProvider(IOptions<InvoicingOptions> options) : IInvoiceTaxRateProvider
{
    public decimal DefaultRate => options.Value.DefaultVatRate;
}
