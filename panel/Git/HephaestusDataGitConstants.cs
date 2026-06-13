namespace Git;

public static class HephaestusDataGitConstants
{
    public const string RepositoryUrl = "https://github.com/kgonsovskii/hephaestus_data.git";

    /// <summary>Xor-hex blob; use <see cref="install/shared/crypt-git-pat.ps1"/> to rotate.</summary>
    public const string EncryptedAccessToken =
        "2f0c040014072c041407185845092a3000011c2c400b25061c3843060b1901285d26407b271446252901323a10187405127d171876450d24363c151712243e163d04411f51207a710214200928312b3e3a3c0c5f230851495878012807";

    public static string AccessToken => HephaestusDataGitCrypt.Decrypt(EncryptedAccessToken);

    public static string CloneUrl =>
        $"https://x-access-token:{AccessToken}@github.com/kgonsovskii/hephaestus_data.git";

    /// <summary>First chars of PAT for logs (verify deployed build matches source).</summary>
    public static string TokenFingerprint
    {
        get
        {
            var token = AccessToken;
            return token.Length >= 20 ? token[..20] + "..." : token;
        }
    }
}
