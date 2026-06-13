using Microsoft.Extensions.Configuration;

namespace Git;

internal static class HephaestusDataGitCredentials
{
    internal static string ResolveAccessToken(string startDirectory)
    {
        var path = Path.Combine(startDirectory, "appsettings.json");
        if (File.Exists(path))
        {
            var token = new ConfigurationBuilder()
                .AddJsonFile(path, optional: true, reloadOnChange: false)
                .Build()
                .GetSection("Git")["HephaestusDataAccessToken"]?
                .Trim();
            if (!string.IsNullOrEmpty(token))
                return token;
        }

        return HephaestusDataGitConstants.DefaultAccessToken;
    }

    internal static string CloneUrl(string startDirectory) =>
        $"https://x-access-token:{ResolveAccessToken(startDirectory)}@github.com/kgonsovskii/hephaestus_data.git";
}
