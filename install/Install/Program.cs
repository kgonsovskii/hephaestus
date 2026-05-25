using Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

internal static class Program
{
    public static async Task<int> Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var options = new TechnitiumOptions();
        config.GetSection(TechnitiumOptions.SectionName).Bind(options);

        var baseUrl = options.BaseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(options.Password))
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
        var dns = new TechnitiumDnsClient(http, NullLogger<TechnitiumDnsClient>.Instance);

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
            }

            await Task.Delay(2000);
        }

        if (!up)
        {
            Console.Error.WriteLine("Timed out waiting for Technitium HTTP.");
            return 1;
        }

        const string defaultPass = "admin";
        var user = string.IsNullOrWhiteSpace(options.User) ? "admin" : options.User.Trim();

        string? token = await TryLoginAsync(dns, user, defaultPass);
        if (token != null)
        {
            if (string.Equals(options.Password, defaultPass, StringComparison.Ordinal))
            {
                Console.WriteLine("[hephaestus-install] Technitium admin password is default (admin); Commons appsettings matches.");
                await ApplyTechnitiumPolicyAsync(dns, token, options);
                return 0;
            }

            var changed = await ChangePasswordAsync(http, token, defaultPass, options.Password);
            if (changed)
            {
                Console.WriteLine("[hephaestus-install] Technitium admin password set from Commons appsettings (Technitium:Password).");
                token = await TryLoginAsync(dns, user, options.Password) ?? token;
                await ApplyTechnitiumPolicyAsync(dns, token, options);
                return 0;
            }

            return 1;
        }

        token = await TryLoginAsync(dns, user, options.Password);
        if (token != null)
        {
            Console.WriteLine("[hephaestus-install] Technitium admin password already matches Commons appsettings.");
            await ApplyTechnitiumPolicyAsync(dns, token, options);
            return 0;
        }

        Console.Error.WriteLine("[hephaestus-install] Could not log in (default admin/admin or Technitium:Password).");
        return 1;
    }

    private static async Task<string?> TryLoginAsync(
        TechnitiumDnsClient dns,
        string user,
        string pass)
    {
        try
        {
            return await dns.LoginAsync(user, pass, CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private static async Task ApplyTechnitiumPolicyAsync(
        TechnitiumDnsClient dns,
        string token,
        TechnitiumOptions options)
    {
        try
        {
            await dns.ApplyGlobalForwardersAsync(token, options, CancellationToken.None);
            Console.WriteLine("[hephaestus-install] Technitium global forwarders applied.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[hephaestus-install] Could not set Technitium global DNS forwarders: {ex.Message}");
        }

        try
        {
            await dns.ApplyRecursionPolicyAsync(token, options, CancellationToken.None);
            Console.WriteLine("[hephaestus-install] Technitium recursion policy applied.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[hephaestus-install] Could not set Technitium recursion policy: {ex.Message}");
        }
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

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("status", out var st) &&
               string.Equals(st.GetString(), "ok", StringComparison.OrdinalIgnoreCase);
    }
}
