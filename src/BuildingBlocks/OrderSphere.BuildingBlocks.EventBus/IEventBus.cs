namespace OrderSphere.BuildingBlocks.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, string destination, CancellationToken ct = default)
        where TEvent : IntegrationEvent;
}
