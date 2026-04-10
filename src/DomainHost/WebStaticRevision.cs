using System.Threading;

namespace DomainHost;

/// <summary>Bumped when anything under the Hephaestus <c>web/</c> tree changes so static ETags rotate.</summary>
public sealed class WebStaticRevision
{
    private long _value;

    public long Current => Interlocked.CompareExchange(ref _value, 0, 0);

    public long Bump() => Interlocked.Increment(ref _value);
}
