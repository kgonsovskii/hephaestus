using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace model;

public static class UrlHelper
{
    public static string NormalizeUri(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // If the input is already a well-formed URI, return it as-is
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        // Otherwise, try to decode and re-encode it safely
        try
        {
            var decoded = Uri.UnescapeDataString(url);
            var normalized = Uri.EscapeUriString(decoded);
            return normalized;
        }
        catch
        {
            return url;
        }
    }
    
        
    public static string GetFileNameFromUrl(string url, string SomeBaseUri)
    {
        try
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(new Uri(SomeBaseUri), url);

            var result = Path.GetFileName(uri.LocalPath);
            result = Path.GetFileNameWithoutExtension(result);
            return result;
        }
        catch (Exception e)
        {
            return url;
        }
    }
    
    public static string HashUrlTo5Chars(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
            // Convert hash to hex and take first 5 lowercase chars
            StringBuilder sb = new StringBuilder();

            for (int i = 0; sb.Length < 5 && i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2")); // hex lowercase
            }

            return sb.ToString().Substring(0, 5);
        }
    }
    
    public static void DownloadFile(string url, string fileName)
    {
        using (WebClient client = new WebClient())
        {
            client.DownloadFile(new Uri(url), fileName);
        }
    }
}