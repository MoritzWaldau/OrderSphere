namespace OrderSphere.BuildingBlocks.EventBus;

public interface IEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
