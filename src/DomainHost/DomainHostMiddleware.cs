namespace DomainHost;

public sealed class DomainHostMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, DomainHostRequestHandler handler)
    {
        if (await handler.TryHandleAsync(context).ConfigureAwait(false))
            return;

        await next(context).ConfigureAwait(false);
    }
}
