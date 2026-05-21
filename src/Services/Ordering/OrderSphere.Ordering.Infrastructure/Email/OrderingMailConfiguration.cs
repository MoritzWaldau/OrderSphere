namespace OrderSphere.Ordering.Infrastructure.Email;

public sealed class OrderingMailConfiguration
{
    public string ConnectionString { get; set; } = "";
    public string SenderAddress { get; set; } = "";
}
