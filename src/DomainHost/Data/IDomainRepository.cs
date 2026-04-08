using DomainHost.Models;

namespace DomainHost.Data;

public interface IDomainRepository
{
    Task<IReadOnlyList<DomainRecord>> LoadEnabledDomainsAsync(CancellationToken cancellationToken);
}
