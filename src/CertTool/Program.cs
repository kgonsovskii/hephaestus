using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("Wrote cert/hephaestus.pfx (empty password). Trust it with scripts/install-hephaestus-cert-*.ps1");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void Run()
    {
        var start = Path.GetFullPath(AppContext.BaseDirectory);
        var webRoot = FindWebRoot(start, "web", 10)
            ?? throw new InvalidOperationException(
                "Could not locate a 'web' folder with domains.json within 10 ascents of the executable.");

        var domainsPath = Path.Combine(webRoot, "domains.json");
        if (!File.Exists(domainsPath))
            throw new InvalidOperationException($"Missing domains file: {domainsPath}");

        var dnsNames = LoadEnabledDomainNames(domainsPath);
        if (dnsNames.Count == 0)
            throw new InvalidOperationException("No enabled domains in domains.json.");

        var repoRoot = Path.GetFullPath(Path.Combine(webRoot, ".."));
        var certDir = Path.Combine(repoRoot, "cert");
        var pfxPath = Path.Combine(certDir, "hephaestus.pfx");

        if (File.Exists(pfxPath))
            throw new InvalidOperationException($"Refusing to overwrite existing file: {pfxPath}");

        Directory.CreateDirectory(certDir);

        using var cert = CreateSelfSigned(dnsNames);
        var pfxBytes = cert.Export(X509ContentType.Pfx, string.Empty);
        File.WriteAllBytes(pfxPath, pfxBytes);
    }

    private static List<string> LoadEnabledDomainNames(string domainsPath)
    {
        var json = File.ReadAllText(domainsPath);
        var doc = JsonSerializer.Deserialize<DomainsFileDto>(json, JsonOptions);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in doc?.Domains ?? [])
        {
            if (row.Enabled && !string.IsNullOrWhiteSpace(row.Domain))
                set.Add(row.Domain.Trim());
        }

        var list = set.ToList();
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    private static X509Certificate2 CreateSelfSigned(IReadOnlyList<string> dnsNames)
    {
        using var rsa = RSA.Create(2048);
        var cn = dnsNames[0];
        var subject = new X500DistinguishedName($"CN={cn}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
            critical: true));

        var san = new SubjectAlternativeNameBuilder();
        foreach (var name in dnsNames)
            san.AddDnsName(name);

        request.CertificateExtensions.Add(san.Build());

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(25));
    }

    private static string? FindWebRoot(string startDirectory, string folderName, int maxAscents)
    {
        var current = startDirectory;
        for (var step = 0; step < maxAscents; step++)
        {
            var candidate = Path.GetFullPath(Path.Combine(current, folderName));
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "domains.json")))
                return candidate;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        return null;
    }

    private sealed class DomainsFileDto
    {
        public List<DomainsFileRowDto>? Domains { get; set; }
    }

    private sealed class DomainsFileRowDto
    {
        public bool Enabled { get; set; }

        public string? Domain { get; set; }
    }
}
