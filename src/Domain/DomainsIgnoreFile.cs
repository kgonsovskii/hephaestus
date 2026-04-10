using System.Text.Json;

namespace Domain;

internal static class DomainsIgnoreFile
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<HashSet<string>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var doc = await JsonSerializer.DeserializeAsync<Dto>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in doc?.Domains ?? [])
        {
            var t = d?.Trim();
            if (!string.IsNullOrEmpty(t))
                set.Add(t);
        }

        return set;
    }

    private sealed class Dto
    {
        public List<string>? Domains { get; set; }
    }
}
