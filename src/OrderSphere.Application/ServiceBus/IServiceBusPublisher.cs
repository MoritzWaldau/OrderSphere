namespace OrderSphere.Application.ServiceBus;

public interface IServiceBusPublisher
{
    Task PublishAsync(string eventType, string payload);
}
