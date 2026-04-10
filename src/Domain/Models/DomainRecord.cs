namespace Domain.Models;

public sealed class DomainRecord
{
    public required bool Enabled { get; init; }

    public required string Domain { get; init; }

    public string? Ip { get; init; }

    public string DomainClass { get; init; } = "";

    public required DomainContentKind ContentKind { get; init; }

    public string? RedirectUrl { get; init; }
}
