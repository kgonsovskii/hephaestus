using cp;

namespace DomainHost;

public sealed class DomainHostMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, DomainHostRequestHandler handler)
    {
        
        if (context.Request.Path.StartsWithSegments(CpSettings.SitePathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (await handler.TryHandleAsync(context).ConfigureAwait(false))
            return;

        await next(context).ConfigureAwait(false);
    }
}
