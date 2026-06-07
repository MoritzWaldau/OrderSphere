using Microsoft.EntityFrameworkCore;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.Advisory.Domain.Entities;
using OrderSphere.BuildingBlocks.Extensions;

namespace OrderSphere.Advisory.Infrastructure.Persistence;

public sealed class AdvisoryDbContext(DbContextOptions<AdvisoryDbContext> options)
    : DbContext(options), IAdvisoryDbContext
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdvisoryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
