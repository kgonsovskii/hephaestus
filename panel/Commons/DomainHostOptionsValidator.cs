using Microsoft.Extensions.Options;

namespace Commons;

public sealed class DomainHostOptionsValidator : IValidateOptions<DomainHostOptions>
{
    public ValidateOptionsResult Validate(string? name, DomainHostOptions options)
    {
        var errors = CollectErrors(options);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    public static void ValidateOrThrow(DomainHostOptions options)
    {
        var errors = CollectErrors(options);
        if (errors.Count > 0)
            throw new OptionsValidationException(
                DomainHostOptions.SectionName,
                typeof(DomainHostOptions),
                errors);
    }

    private static List<string> CollectErrors(DomainHostOptions o)
    {
        var errors = new List<string>();

        void RequireFileName(string property, string? value)
        {
            var t = value?.Trim() ?? "";
            if (t.Length == 0)
            {
                errors.Add($"{DomainHostOptions.SectionName}:{property} is missing or empty.");
                return;
            }

            if (t is "." or ".." || t.Contains(Path.DirectorySeparatorChar) || t.Contains(Path.AltDirectorySeparatorChar))
                errors.Add($"{DomainHostOptions.SectionName}:{property} must be a single file name, not a path ('{value}').");
        }

        void RequireDirSegment(string property, string? value)
        {
            var t = value?.Trim().Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? "";
            if (t.Length == 0)
            {
                errors.Add($"{DomainHostOptions.SectionName}:{property} is missing or empty.");
                return;
            }

            if (t is "." or ".." || t.Contains(Path.DirectorySeparatorChar) || t.Contains(Path.AltDirectorySeparatorChar))
                errors.Add($"{DomainHostOptions.SectionName}:{property} must be a single directory name, not a path ('{value}').");
        }

        void RequireRelativePath(string property, string? value, string anchor)
        {
            var t = value?.Trim() ?? "";
            if (t.Length == 0)
            {
                errors.Add($"{DomainHostOptions.SectionName}:{property} is missing or empty.");
                return;
            }

            if (Path.IsPathRooted(t))
                errors.Add($"{DomainHostOptions.SectionName}:{property} must be relative to {anchor}, not an absolute path ('{value}').");
        }

        RequireRelativePath(nameof(o.RepositoryRoot), o.RepositoryRoot, "application base directory");
        RequireDirSegment(nameof(o.HephaestusData), o.HephaestusData);
        RequireDirSegment(nameof(o.WebRoot), o.WebRoot);
        RequireFileName(nameof(o.DomainsFileName), o.DomainsFileName);
        RequireDirSegment(nameof(o.CertDirectoryName), o.CertDirectoryName);
        RequireFileName(nameof(o.CertPfxFileName), o.CertPfxFileName);
        RequireFileName(nameof(o.CertPublicCerFileName), o.CertPublicCerFileName);

        if (o.HttpPort is < 1 or > 65535)
            errors.Add($"{DomainHostOptions.SectionName}:{nameof(o.HttpPort)} must be between 1 and 65535 (got {o.HttpPort}).");

        if (o.HttpsPort is < 1 or > 65535)
            errors.Add($"{DomainHostOptions.SectionName}:{nameof(o.HttpsPort)} must be between 1 and 65535 (got {o.HttpsPort}).");

        if (o.StaticFileCacheMaxAgeSeconds is < 0 or > 86400)
            errors.Add($"{DomainHostOptions.SectionName}:{nameof(o.StaticFileCacheMaxAgeSeconds)} must be between 0 and 86400 (got {o.StaticFileCacheMaxAgeSeconds}).");

        if (o.RetryDelaySeconds is < 1 or > 3600)
            errors.Add($"{DomainHostOptions.SectionName}:{nameof(o.RetryDelaySeconds)} must be between 1 and 3600 (got {o.RetryDelaySeconds}).");

        return errors;
    }
}
