using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Commons;

internal static class Program
{
    private const string CaSubjectName = "CN=Hephaestus Development Root CA,O=Hephaestus";
    private const string LeafSubjectName = "CN=Hephaestus TLS,O=Hephaestus";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("Wrote cert/hephaestus.pfx (server leaf, empty password) and cert/hephaestus-root.cer (trust this root once).");
            Console.WriteLine("Run scripts/install-hephaestus-cert-*.ps1 to add the root to Trusted Root (not the PFX).");
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
        var rootCerPath = Path.Combine(certDir, "hephaestus-root.cer");

        if (File.Exists(pfxPath))
            throw new InvalidOperationException($"Refusing to overwrite existing file: {pfxPath}");
        if (File.Exists(rootCerPath))
            throw new InvalidOperationException($"Refusing to overwrite existing file: {rootCerPath}");

        Directory.CreateDirectory(certDir);

        using var caCert = CreateCaCertificate();
        using var leafCert = CreateLeafCertificate(caCert, dnsNames);

        File.WriteAllBytes(rootCerPath, caCert.Export(X509ContentType.Cert));
        File.WriteAllBytes(pfxPath, leafCert.Export(X509ContentType.Pfx, string.Empty));
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

    private static X509Certificate2 CreateCaCertificate()
    {
        var rsa = RSA.Create(4096);
        var subject = new X500DistinguishedName(CaSubjectName);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(
            certificateAuthority: true,
            hasPathLengthConstraint: false,
            pathLengthConstraint: 0,
            critical: true));

        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature,
            critical: true));

        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(25));
    }

    private static X509Certificate2 CreateLeafCertificate(X509Certificate2 issuerCa, IReadOnlyList<string> dnsNames)
    {
        var leafRsa = RSA.Create(2048);
        var subject = new X500DistinguishedName(LeafSubjectName);
        var request = new CertificateRequest(subject, leafRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

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

        Span<byte> serial = stackalloc byte[8];
        RandomNumberGenerator.Fill(serial);
        serial[0] &= 0x7F;

        using var publicLeaf = request.Create(issuerCa, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2), serial.ToArray());
        return publicLeaf.CopyWithPrivateKey(leafRsa);
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
