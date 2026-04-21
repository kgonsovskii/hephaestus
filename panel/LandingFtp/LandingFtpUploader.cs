using System.Net;

namespace LandingFtp;

/// <summary>FTP(S) upload using <see cref="FtpWebRequest"/> from a URI like <c>ftp://user:pass@host/path/to/folder/</c> (same pattern as WinINet-style URLs).</summary>
internal static class LandingFtpUploader
{
    public static void UploadFile(Uri baseUri, string localFilePath, string remoteFileName)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        var folder = NormalizeFolderUri(baseUri);
        var uploadUri = new Uri(folder, remoteFileName);

        ParseCredentials(uploadUri, out var user, out var password);

        var buf = File.ReadAllBytes(localFilePath);
        UploadBytes(uploadUri, user, password, buf);
    }

    private static Uri NormalizeFolderUri(Uri raw)
    {
        var s = raw.AbsoluteUri.TrimEnd('/');
        return new Uri(s + '/', UriKind.Absolute);
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

    private static void UploadBytes(Uri uploadUri, string user, string password, byte[] buf)
    {
#pragma warning disable SYSLIB0014 // WebRequest/FtpWebRequest obsolete but standard for ftp:// across runtimes here
        var req = (FtpWebRequest)WebRequest.Create(uploadUri);
#pragma warning restore SYSLIB0014

        req.Method = WebRequestMethods.Ftp.UploadFile;
        req.UseBinary = true;
        req.UsePassive = true;
        req.EnableSsl = string.Equals(uploadUri.Scheme, "ftps", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uploadUri.Scheme, "ftpes", StringComparison.OrdinalIgnoreCase);
        req.Credentials = new NetworkCredential(user, password);

        using var ms = new MemoryStream(buf);
        using (var ws = req.GetRequestStream())
            ms.CopyTo(ws);

#pragma warning disable SYSLIB0014
        using var resp = (FtpWebResponse)req.GetResponse();
#pragma warning restore SYSLIB0014

        resp.Close();
    }
}
