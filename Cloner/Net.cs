using System.Net.Sockets;

public class NetworkUtils
{
    public static bool Test(string serverIp)
    {
        TcpClient client = new TcpClient();
        try
        {
            IAsyncResult asyncResult = client.BeginConnect(serverIp, 5985, null, null);
            bool success = asyncResult.AsyncWaitHandle.WaitOne(4000, false); // 4000 ms = 4 seconds timeout

            if (success && client.Connected)
            {
                client.EndConnect(asyncResult);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
        finally
        {
            client.Close();
        }
    }
}