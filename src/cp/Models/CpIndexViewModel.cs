using model;

namespace cp.Models;

public sealed class CpIndexViewModel
{
    public required ServerModel Server { get; init; }

    public required IReadOnlyList<DomainEditRow> DomainRows { get; init; }

    public string? DomainsResult { get; init; }
}
