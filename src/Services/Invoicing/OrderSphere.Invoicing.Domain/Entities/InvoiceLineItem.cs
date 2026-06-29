namespace OrderSphere.Invoicing.Domain.Entities;

public sealed class InvoiceLineItem
{
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
