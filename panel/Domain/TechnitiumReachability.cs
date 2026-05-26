using System.Net.Http;
using System.Net.Sockets;

namespace Domain;

internal static class TechnitiumReachability
{
    public static bool IsUnreachable(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SocketException { SocketErrorCode: SocketError.ConnectionRefused })
                return true;
            if (e is HttpRequestException && e.InnerException is SocketException)
                return true;
        }

        return false;
    }
}
