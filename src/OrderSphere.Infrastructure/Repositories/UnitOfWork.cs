using OrderSphere.Application.Repositories;
using OrderSphere.Infrastructure.Persistence;


namespace OrderSphere.Infrastructure.Repositories;

public sealed class UnitOfWork(
    OrderSphereDbContext Context, 
    IProductRepository ProductRepository,
    IOutboxRepository OutboxRepository
    ) : IUnitOfWork
{
    public IProductRepository Products => ProductRepository;
    public IOutboxRepository Outbox => OutboxRepository;

    public async Task<int> CommitAsync() => await Context.SaveChangesAsync();
    public void Dispose() => Context.Dispose();
}
