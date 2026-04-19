using Microsoft.AspNetCore.Builder;

namespace Cloner;

public static class ClonerCpPipelineExtensions
{
    /// <summary>WebSocket log stream for remote install (path <c>/clone-ws</c> after cp prefix rewrite).</summary>
    public static IApplicationBuilder UseClonerCloneSupport(this IApplicationBuilder cp)
    {
        cp.UseWebSockets();
        cp.UseMiddleware<CloneWebSocketMiddleware>();
        return cp;
    }
}
