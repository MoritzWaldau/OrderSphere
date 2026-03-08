using Microsoft.EntityFrameworkCore;
using OrderSphere.Application.Repositories;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.UI
{
    public sealed class OutboxWorker(
        IUnitOfWork UnitOfWork, 
        IServiceBusPublisher Bus) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var events = await UnitOfWork.Outbox.GetUnprocessedEventsAsync(20, stoppingToken);

                foreach (var evt in events)
                {
                    try
                    {
                        await Bus.PublishAsync(evt.Type, evt.Payload);
                        await UnitOfWork.Outbox.MarkProcessedAsync(evt, stoppingToken);
                    }
                    catch
                    {
                        // Retry / Logging / Dead Letter
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
