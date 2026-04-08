namespace DomainTool.Services;

public interface IDomainNameSource
{
    Task<IReadOnlyList<string>> GetEnabledDomainNamesAsync(CancellationToken cancellationToken = default);
}
