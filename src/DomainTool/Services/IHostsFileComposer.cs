namespace DomainTool.Services;

public interface IHostsFileComposer
{
    string Compose(IReadOnlyList<string> domainNames);
}
