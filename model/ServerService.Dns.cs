using System.Diagnostics;

namespace model;

public partial class ServerService
{
    public void UpdateDNS(ServerModel server)
    {
        var first = server.Interfaces.Count >= 1 ? server.Interfaces[0] : server.ServerIp;
        server.PrimaryDns = first;
        server.SecondaryDns = server.PrimaryDns;
        if (server.Interfaces.Count >= 2)
            server.SecondaryDns = server.Interfaces[1];
        if (!string.IsNullOrEmpty(server.StrahServer))
            server.SecondaryDns = server.StrahServer;
    }
    
    public void UpdateIpDomains(ServerModel server)
    {
        server.Interfaces = Dev.GetPublicIPv4Addresses();
        for (int i = server.DomainIps.Count - 1; i >= 0; i--)
        {
            var domainIp = server.DomainIps[i];
            if (string.IsNullOrEmpty(domainIp.Index))
                domainIp.Index = Guid.NewGuid().ToString();
            var cnt = server.DomainIps.Count(a => a.Name == domainIp.Name);
            if (cnt >= 2 || string.IsNullOrEmpty(domainIp.Name))
            {
                domainIp.Name = Guid.NewGuid().ToString();
            }
        }

        for (int i = server.DomainIps.Count - 1; i >= 0; i--)
        {
            var allowedIp = server.Interfaces.Except([server.ServerIp]).ToArray();
            var allIp = server.DomainIps.Select(a => a.IP).ToList();
            var domainIp = server.DomainIps[i];
            var cnt = server.DomainIps.Count(a => a.IP == domainIp.IP);
            if (!allowedIp.Contains(domainIp.IP) || cnt >= 2)
            {
                var freeIp = allowedIp.Except(allIp).FirstOrDefault() ?? "127.0.0.1";
                server.DomainIps[i].IP = freeIp;
            }
        }
    }
}