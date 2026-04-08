using System.Diagnostics.CodeAnalysis;
using DomainHost.Models;

namespace DomainHost.Services;

public interface IDomainCatalog
{
    bool TryGetBestMatch(string host, [NotNullWhen(true)] out DomainRecord? record);
}
