using System.Net;

namespace model;

public static class UrlHelper
{
    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // First decode in case it's already encoded
        string decoded = WebUtility.UrlDecode(url);

        // Then encode it once
        string encoded = WebUtility.UrlEncode(decoded);

        return encoded;
    }
}