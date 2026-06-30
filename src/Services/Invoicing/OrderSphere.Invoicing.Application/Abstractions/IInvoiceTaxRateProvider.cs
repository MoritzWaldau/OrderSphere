namespace OrderSphere.Invoicing.Application.Abstractions;

public interface IInvoiceTaxRateProvider
{
    decimal DefaultRate { get; }
}
