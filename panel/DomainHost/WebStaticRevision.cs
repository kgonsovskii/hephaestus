using System.Threading;

namespace DomainHost;

public sealed class WebStaticRevision
{
    private long _value;

    public long Current => Interlocked.CompareExchange(ref _value, 0, 0);

    public long Bump() => Interlocked.Increment(ref _value);
}
