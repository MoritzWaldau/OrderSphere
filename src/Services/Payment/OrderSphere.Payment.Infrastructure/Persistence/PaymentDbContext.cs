using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Extensions;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Application.Abstractions;
using OrderSphere.Payment.Domain.Entities;
using OrderSphere.Payment.Infrastructure.Outbox;

namespace OrderSphere.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(
    DbContextOptions<PaymentDbContext> options,
    IPublisher publisher) : DbContext(options), IPaymentDbContext
{
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public void AddOutboxMessage(string type, string content)
        => OutboxMessages.Add(new OutboxMessage { Type = type, Content = content });

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditFields();

        var events = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .SelectMany(e => e.PopDomainEvents())
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var @event in events)
            await publisher.Publish(@event, cancellationToken);

        return result;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<PaymentId>().HaveConversion<PaymentIdConverter>();
        configurationBuilder.Properties<OrderId>().HaveConversion<OrderIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
