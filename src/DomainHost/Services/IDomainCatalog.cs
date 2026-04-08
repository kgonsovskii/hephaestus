using System.Diagnostics.CodeAnalysis;
using DomainHost.Models;

namespace DomainHost.Services;

public interface IDomainCatalog
{
    /// <summary>
    /// Walks DNS-style suffixes: <c>123.mc.yandex.com</c> → <c>mc.yandex.com</c> → … until a catalog domain matches.
    /// </summary>
    bool TryGetBestMatch(string host, [NotNullWhen(true)] out DomainRecord? record);
}
