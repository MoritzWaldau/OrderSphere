namespace OrderSphere.Application.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        IOutboxRepository Outbox { get; }
        Task<int> CommitAsync();
    }
}
