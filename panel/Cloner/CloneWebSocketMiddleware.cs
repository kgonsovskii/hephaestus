using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cloner;

public sealed class CloneWebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClonerRemoteInstall _cloner;
    private readonly ILogger<CloneWebSocketMiddleware> _logger;

    public CloneWebSocketMiddleware(RequestDelegate next, IClonerRemoteInstall cloner, ILogger<CloneWebSocketMiddleware> logger)
    {
        _next = next;
        _cloner = cloner;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals("/clone-ws", StringComparison.OrdinalIgnoreCase) ||
            !context.WebSockets.IsWebSocketRequest)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var authz = context.RequestServices.GetRequiredService<IAuthorizationService>();
        var allowed = await authz.AuthorizeAsync(context.User, context, "AllowFromIpRange").ConfigureAwait(false);
        if (!allowed.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!Guid.TryParse(context.Request.Query["runId"], out var runId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing or invalid runId query parameter.").ConfigureAwait(false);
            return;
        }

        var reader = _cloner.TrySubscribeLogReader(runId);
        if (reader == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Unknown or expired runId.").ConfigureAwait(false);
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var buffer = new byte[4096];
        try
        {
            while (await reader.WaitToReadAsync(context.RequestAborted).ConfigureAwait(false))
            {
                while (reader.TryRead(out var line))
                {
                    var payload = System.Text.Encoding.UTF8.GetBytes(line + Environment.NewLine);
                    await socket.SendAsync(
                        new ArraySegment<byte>(payload),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        context.RequestAborted).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cloner WebSocket closed for {RunId}", runId);
        }
        finally
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None)
                    .ConfigureAwait(false);
        }
    }
}
