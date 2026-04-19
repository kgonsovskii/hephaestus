using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Commons;
using FluentAssertions;

namespace Tests;

[TestClass]
public sealed class NetworkAddressPreferenceTests
{
    [TestMethod]
    public void TryGetPreferredAddresses_DoesNotThrow()
    {
        var act = () => NetworkAddressPreference.TryGetPreferredAddresses(out _, out _);
        act.Should().NotThrow();
    }

        [TestMethod]
    public void TryGetPreferredAddresses_PrintsDiagnostics_AlwaysPass()
    {
        NetworkAddressPreference.TryGetPreferredAddresses(out var v4, out var v6);
        Console.WriteLine(
            "[NetworkAddressPreference] preferred IPv4={0}, IPv6={1}",
            v4?.ToString() ?? "null",
            v6?.ToString() ?? "null");
    }

    [TestMethod]
    public void TryGetPreferredAddresses_WhenIpv6Returned_ItIsGlobalUnicastOrUla()
    {
        NetworkAddressPreference.TryGetPreferredAddresses(out _, out var v6);
        if (v6 is null)
            return;

        (NetworkAddressPreference.IsIpv6GlobalUnicast(v6) || NetworkAddressPreference.IsIpv6UniqueLocal(v6))
            .Should().BeTrue("preferred IPv6 must be global unicast or unique-local (not link-local)");
    }

        [TestMethod]
    public void TryGetPreferredAddresses_HasIpv6_WhenMachineExposesGlobalOrUla()
    {
        if (!MachineHasGlobalOrUlaIpv6OnAllowedInterfaces())
        {
            Assert.Inconclusive(
                "No global/ULA IPv6 on a non-skipped interface — cannot assert AAAA source address here.");
        }

        NetworkAddressPreference.TryGetPreferredAddresses(out _, out var v6);
        if (v6 is null)
        {
            Assert.Fail(
                "Expected preferred IPv6: the OS has global/ULA IPv6 on an allowed interface, but " +
                "TryGetPreferredAddresses returned null. Check NetworkAddressPreference vs interface ordering.");
        }

        (NetworkAddressPreference.IsIpv6GlobalUnicast(v6) || NetworkAddressPreference.IsIpv6UniqueLocal(v6))
            .Should().BeTrue();
    }

    [TestMethod]
    [DataRow("2001:4860:4860::8888", true)]
    [DataRow("2a00:1450:4010:c01::8a", true)]
    [DataRow("fe80::1", false)]
    [DataRow("ff02::1", false)]
    public void IsIpv6GlobalUnicast_Classifies(string address, bool expected)
    {
        var ip = IPAddress.Parse(address);
        NetworkAddressPreference.IsIpv6GlobalUnicast(ip).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("fd12:3456::1", true)]
    [DataRow("fc00::1", true)]
    [DataRow("2001:db8::1", false)]
    [DataRow("fe80::1", false)]
    public void IsIpv6UniqueLocal_Classifies(string address, bool expected)
    {
        var ip = IPAddress.Parse(address);
        NetworkAddressPreference.IsIpv6UniqueLocal(ip).Should().Be(expected);
    }

        private static bool MachineHasGlobalOrUlaIpv6OnAllowedInterfaces()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;
            if (ShouldSkipInterfaceForTests(ni.Name, ni.Description))
                continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                var addr = ua.Address;
                if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                if (addr.IsIPv6LinkLocal || addr.IsIPv6Multicast)
                    continue;
                if (NetworkAddressPreference.IsIpv6GlobalUnicast(addr) ||
                    NetworkAddressPreference.IsIpv6UniqueLocal(addr))
                    return true;
            }
        }

        return false;
    }

    private static bool ShouldSkipInterfaceForTests(string name, string description)
    {
        var d = description + " " + name;
        if (string.IsNullOrEmpty(d))
            return false;
        d = d.ToUpperInvariant();
        string[] bad =
        [
            "LOOPBACK", "VMWARE", "VIRTUALBOX", "HYPER-V", "VIRTUAL ", " TAP", "TUN", "WSL",
            "VETHERNET", "BLUETOOTH", "NPCAP", "VPN", "TAILSCALE", "ZERO TIER", "ZEROTIER",
            "NORDLYNX", "WIREGUARD", "DOCKER", "VBOX", "PANGP", "NETMON", "FILTER"
        ];
        return bad.Any(x => d.Contains(x, StringComparison.Ordinal));
    }
}
