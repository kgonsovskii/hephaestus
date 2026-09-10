using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LandingFtp;

/// <summary>
/// Uploads landing files to the folder in the configured FTP URL, for example
/// <c>ftp://user:pass@host/site.host/</c>. The URL path is the remote folder as-is.
/// </summary>
internal static class LandingFtpUploader
{
    private static readonly Encoding FtpEncoding = Encoding.ASCII;

    public static void UploadFile(Uri baseUri, string localFilePath, string remoteFileName)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteFileName);

        if (remoteFileName.IndexOfAny(['/', '\\']) >= 0)
            throw new ArgumentException("Remote file name must not contain a path.", nameof(remoteFileName));

        var folder = NormalizeFolderUri(baseUri);
        ParseCredentials(folder, out var user, out var password);
        var buf = File.ReadAllBytes(localFilePath);
        UploadBytes(folder, user, password, remoteFileName, buf);
    }

    /// <summary>Keeps the URL path unchanged except for a trailing slash.</summary>
    internal static Uri NormalizeFolderUri(Uri raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        if (!string.Equals(raw.Scheme, "ftp", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Landing FTP supports ftp:// only (got '{raw.Scheme}').");
        }

        var builder = new UriBuilder(raw)
        {
            Path = EnsureFolderPath(raw.AbsolutePath),
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }

    internal static string EnsureFolderPath(string absolutePath)
    {
        var trimmed = (absolutePath ?? "/").Replace('\\', '/').Trim('/');
        return trimmed.Length == 0 ? "/" : "/" + trimmed + "/";
    }

    private static void ParseCredentials(Uri ftpUri, out string user, out string password)
    {
        user = Uri.UnescapeDataString(ftpUri.UserInfo ?? "");
        password = "";

        var idx = user.IndexOf(':');
        if (idx >= 0)
        {
            password = Uri.UnescapeDataString(user[(idx + 1)..]);
            user = Uri.UnescapeDataString(user[..idx]);
        }

        if (string.IsNullOrEmpty(user))
            throw new InvalidOperationException("FTP URL must include credentials (ftp://user:password@host/...).");
    }

    private static void UploadBytes(Uri folderUri, string user, string password, string remoteFileName, byte[] buf)
    {
        var port = folderUri.IsDefaultPort ? 21 : folderUri.Port;
        using var control = new TcpClient();
        control.ReceiveTimeout = 30_000;
        control.SendTimeout = 30_000;
        control.Connect(folderUri.Host, port);

        using var stream = control.GetStream();
        Expect(ReadReply(stream), 220);

        Send(stream, "USER " + user);
        var userReply = ReadReply(stream);
        if (userReply.Code == 331)
        {
            Send(stream, "PASS " + password);
            Expect(ReadReply(stream), 230);
        }
        else
        {
            Expect(userReply, 230);
        }

        Send(stream, "TYPE I");
        Expect(ReadReply(stream), 200);

        EnsureDirectory(stream, folderUri.AbsolutePath);

        var pasvPort = EnterPassive(stream);
        using var data = ConnectData(control, pasvPort);
        using var dataStream = data.GetStream();

        Send(stream, "STOR " + remoteFileName);
        Expect(ReadReply(stream), 125, 150);

        dataStream.Write(buf, 0, buf.Length);
        dataStream.Flush();
        data.Close();

        Expect(ReadReply(stream), 226, 250);

        try
        {
            Send(stream, "QUIT");
            ReadReply(stream);
        }
        catch (IOException)
        {
            // Server may close after QUIT.
        }
    }

    private static void EnsureDirectory(Stream stream, string absolutePath)
    {
        var trimmed = absolutePath.Replace('\\', '/').Trim('/');
        if (trimmed.Length == 0)
            return;

        Send(stream, "CWD /");
        Expect(ReadReply(stream), 250);

        foreach (var segment in trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            Send(stream, "CWD " + segment);
            var cwd = ReadReply(stream);
            if (cwd.Code is 250 or 200)
                continue;

            Send(stream, "MKD " + segment);
            var mkd = ReadReply(stream);
            if (mkd.Code is not (257 or 250 or 550))
                throw new InvalidOperationException($"FTP MKD {segment} failed: {mkd.Raw}");

            Send(stream, "CWD " + segment);
            Expect(ReadReply(stream), 250);
        }
    }

    private static int EnterPassive(Stream stream)
    {
        Send(stream, "PASV");
        var reply = ReadReply(stream);
        Expect(reply, 227);

        var open = reply.Raw.LastIndexOf('(');
        var close = reply.Raw.LastIndexOf(')');
        if (open < 0 || close <= open)
            throw new InvalidOperationException("FTP PASV reply was missing host/port: " + reply.Raw);

        var parts = reply.Raw[(open + 1)..close].Split(',');
        if (parts.Length < 6)
            throw new InvalidOperationException("FTP PASV reply was malformed: " + reply.Raw);

        var p1 = int.Parse(parts[^2].Trim(), CultureInfo.InvariantCulture);
        var p2 = int.Parse(parts[^1].Trim(), CultureInfo.InvariantCulture);
        return (p1 * 256) + p2;
    }

    private static TcpClient ConnectData(TcpClient control, int pasvPort)
    {
        var peer = (IPEndPoint)control.Client.RemoteEndPoint!;
        var data = new TcpClient();
        data.ReceiveTimeout = 30_000;
        data.SendTimeout = 30_000;
        // Use the control-connection address, not the PASV advertised IP (often 127.0.0.1 behind NAT).
        data.Connect(new IPEndPoint(peer.Address, pasvPort));
        return data;
    }

    private static void Send(Stream stream, string command)
    {
        var bytes = FtpEncoding.GetBytes(command + "\r\n");
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static void Expect(FtpReply reply, params int[] codes)
    {
        foreach (var code in codes)
        {
            if (reply.Code == code)
                return;
        }

        throw new InvalidOperationException($"FTP command failed ({reply.Code}): {reply.Raw}");
    }

    private static FtpReply ReadReply(Stream stream)
    {
        var first = ReadLine(stream);
        if (first.Length < 3
            || !int.TryParse(first.AsSpan(0, 3), NumberStyles.None, CultureInfo.InvariantCulture, out var code))
            throw new InvalidOperationException("Invalid FTP reply: " + first);

        var raw = first;
        if (first.Length > 3 && first[3] == '-')
        {
            var prefix = code.ToString("D3", CultureInfo.InvariantCulture) + " ";
            while (true)
            {
                var next = ReadLine(stream);
                raw += "\n" + next;
                if (next.StartsWith(prefix, StringComparison.Ordinal))
                    break;
            }
        }

        return new FtpReply(code, raw);
    }

    private static string ReadLine(Stream stream)
    {
        var buffer = new MemoryStream();
        while (true)
        {
            var b = stream.ReadByte();
            if (b < 0)
                throw new EndOfStreamException("FTP control connection closed.");
            if (b == '\n')
                break;
            if (b != '\r')
                buffer.WriteByte((byte)b);
        }

        return FtpEncoding.GetString(buffer.ToArray());
    }

    private readonly struct FtpReply
    {
        public FtpReply(int code, string raw)
        {
            Code = code;
            Raw = raw;
        }

        public int Code { get; }
        public string Raw { get; }
    }
}
