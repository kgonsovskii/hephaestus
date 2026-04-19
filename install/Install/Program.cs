using System.Text.Json;
using Microsoft.Extensions.Configuration;

internal static class Program
{
    public static async Task<int> Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var tech = config.GetSection("Technitium");
        var baseUrl = (tech["BaseUrl"] ?? "http://127.0.0.1:5380").Trim().TrimEnd('/');
        var newPassword = tech["Password"];
        if (string.IsNullOrEmpty(newPassword))
        {
            Console.Error.WriteLine("Commons appsettings: Technitium:Password is missing or empty.");
            return 1;
        }

        if (!baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            baseUrl = "http://" + baseUrl;

        using var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl + "/"),
            Timeout = TimeSpan.FromMinutes(2)
        };

        Console.WriteLine($"[hephaestus-install] Waiting for Technitium at {baseUrl} …");
        var up = false;
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var ping = await http.GetAsync("/");
                if (ping.IsSuccessStatusCode)
                {
                    up = true;
                    break;
                }
            }
            catch
            {
                /* retry */
            }

            await Task.Delay(2000);
        }

        if (!up)
        {
            Console.Error.WriteLine("Timed out waiting for Technitium HTTP.");
            return 1;
        }

        const string defaultPass = "admin";
        const string user = "admin";

        var token = await LoginAsync(http, user, defaultPass);
        if (token != null)
        {
            if (string.Equals(newPassword, defaultPass, StringComparison.Ordinal))
            {
                Console.WriteLine("[hephaestus-install] Technitium admin password is default (admin); Commons appsettings matches.");
                return 0;
            }

            var changed = await ChangePasswordAsync(http, token, defaultPass, newPassword);
            if (changed)
            {
                Console.WriteLine("[hephaestus-install] Technitium admin password set from Commons appsettings (Technitium:Password).");
                return 0;
            }

            return 1;
        }

        token = await LoginAsync(http, user, newPassword);
        if (token != null)
        {
            Console.WriteLine("[hephaestus-install] Technitium admin password already matches Commons appsettings.");
            return 0;
        }

        Console.Error.WriteLine("[hephaestus-install] Could not log in (default admin/admin or Technitium:Password).");
        return 1;
    }

    private static async Task<string?> LoginAsync(HttpClient http, string user, string pass)
    {
        var q =
            $"api/user/login?user={Uri.EscapeDataString(user)}&pass={Uri.EscapeDataString(pass)}&includeInfo=false";
        string json;
        try
        {
            json = await http.GetStringAsync(q);
        }
        catch
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("status", out var st) ||
            !string.Equals(st.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
            return null;
        if (doc.RootElement.TryGetProperty("token", out var tok))
            return tok.GetString();
        return null;
    }

    private static async Task<bool> ChangePasswordAsync(
        HttpClient http,
        string token,
        string currentPass,
        string newPass)
    {
        var q =
            $"api/user/changePassword?token={Uri.EscapeDataString(token)}&pass={Uri.EscapeDataString(currentPass)}&newPass={Uri.EscapeDataString(newPass)}";
        string json;
        try
        {
            json = await http.GetStringAsync(q);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"changePassword request failed: {ex.Message}");
            return false;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("status", out var st) &&
               string.Equals(st.GetString(), "ok", StringComparison.OrdinalIgnoreCase);
    }
}
