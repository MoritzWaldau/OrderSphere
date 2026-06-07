using Microsoft.EntityFrameworkCore;
using OrderSphere.Advisory.Domain.Entities;

namespace OrderSphere.Advisory.Application.Abstractions;

// Persistence abstraction for advisory conversations. Declared in Application,
// implemented by AdvisoryDbContext in Infrastructure; consumers depend on this
// interface, never the concrete context.
public interface IAdvisoryDbContext
{
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationMessage> ConversationMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
