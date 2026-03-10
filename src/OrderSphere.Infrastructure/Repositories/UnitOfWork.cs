using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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


    private IDbContextTransaction _transaction;

    public async Task BeginTransactionAsync()
    {
        _transaction = await Context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await Context.SaveChangesAsync();

        if (_transaction != null)
            await _transaction.CommitAsync();
    }

    public void Dispose() => ((IDisposable)Context).Dispose();

    public async Task RollbackAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }
}
