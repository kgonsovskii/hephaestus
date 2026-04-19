using Domain.Models;

namespace cp.Models;

public sealed class DomainEditRow
{
    public bool Enabled { get; set; }

    public string Domain { get; set; } = "";

    public string? Ip { get; set; }

    public string DomainClass { get; set; } = "";

        public string ContentKind { get; set; } = "javascript";

    public string? RedirectUrl { get; set; }

    public static DomainEditRow FromRecord(DomainRecord r) =>
        new()
        {
            Enabled = r.Enabled,
            Domain = r.Domain,
            Ip = r.Ip,
            DomainClass = r.DomainClass,
            ContentKind = KindToString(r.ContentKind),
            RedirectUrl = r.RedirectUrl
        };

    public DomainRecord ToDomainRecord() =>
        new()
        {
            Enabled = Enabled,
            Domain = Domain.Trim(),
            Ip = string.IsNullOrWhiteSpace(Ip) ? null : Ip.Trim(),
            DomainClass = DomainClass ?? "",
            ContentKind = ParseKind(ContentKind),
            RedirectUrl = string.IsNullOrWhiteSpace(RedirectUrl) ? null : RedirectUrl.Trim()
        };

    private static string KindToString(DomainContentKind k) =>
        k switch
        {
            DomainContentKind.Html => "html",
            DomainContentKind.Redirect => "redirect",
            _ => "javascript"
        };

    private static DomainContentKind ParseKind(string? raw) =>
        (raw ?? "").Trim().ToLowerInvariant() switch
        {
            "html" => DomainContentKind.Html,
            "redirect" => DomainContentKind.Redirect,
            _ => DomainContentKind.JavaScript
        };
}
