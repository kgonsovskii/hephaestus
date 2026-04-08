using DomainHost.Models;

namespace DomainHost.Services;

public interface IWebFileResolver
{
    WebFileResolution Resolve(DomainRecord record, PathString requestPath);
}
