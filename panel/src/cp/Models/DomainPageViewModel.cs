namespace cp.Models;

public sealed class DomainPageViewModel
{
    public required IReadOnlyList<DomainEditRow> DomainRows { get; init; }

    public required IReadOnlyList<string> ClassFolderNames { get; init; }

    public string? Message { get; init; }
}
