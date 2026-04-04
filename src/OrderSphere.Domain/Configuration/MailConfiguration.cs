namespace OrderSphere.Domain.Configuration;

public sealed class MailConfiguration
{
    public required string ConnectionString { get; set; }
    public required string SenderAddress { get; set; }
}
