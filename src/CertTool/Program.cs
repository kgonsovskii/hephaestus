using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Commons;

internal static class Program
{
    private const string SubjectName = "CN=Hephaestus LAN TLS,O=Hephaestus";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("Wrote cert/hephaestus.pfx (password: 123). DomainHost loads this file; clients run scripts/install-hephaestus-cert.ps1 (installs public cert to Root only, no private key on clients).");
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
        var repoRoot = HephaestusRepoPaths.ResolveRepositoryRoot(start);
        var webRoot = HephaestusRepoPaths.WebDirectory(repoRoot, "web");
        if (!Directory.Exists(webRoot))
            throw new InvalidOperationException($"Web directory not found: {webRoot}");

        var domainsPath = HephaestusRepoPaths.FileUnderWeb(repoRoot, "web", "domains.json");
        if (!File.Exists(domainsPath))
            throw new InvalidOperationException($"Missing domains file: {domainsPath}");

        var dnsNames = LoadEnabledDomainNames(domainsPath);
        if (dnsNames.Count == 0)
            throw new InvalidOperationException("No enabled domains in domains.json.");

        var certDir = HephaestusRepoPaths.CertDirectory(repoRoot, "cert");
        var pfxPath = Path.Combine(certDir, "hephaestus.pfx");

        if (File.Exists(pfxPath))
            throw new InvalidOperationException($"Refusing to overwrite existing file: {pfxPath}");

        Directory.CreateDirectory(certDir);

        using var cert = CreateLanTlsCertificate(dnsNames);
        File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx, "123"));
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

    private static X509Certificate2 CreateLanTlsCertificate(IReadOnlyList<string> dnsNames)
    {
        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName(SubjectName);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
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

        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
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
