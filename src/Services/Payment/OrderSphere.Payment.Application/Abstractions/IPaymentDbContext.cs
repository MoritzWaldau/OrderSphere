using Microsoft.EntityFrameworkCore;
using OrderSphere.Payment.Domain.Entities;

namespace OrderSphere.Payment.Application.Abstractions;

public interface IPaymentDbContext
{
    DbSet<PaymentRecord> Payments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
