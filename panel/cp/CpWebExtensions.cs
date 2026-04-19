using Cloner;
using cp.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using model;

namespace cp;

public static class CpWebExtensions
{
        public static WebApplicationBuilder AddCp(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ServerService>();
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
            builder.Services.AddControllersWithViews()
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
                    options.AccessDeniedPath = "/auth";
                    options.LoginPath = "/auth";
                    options.LogoutPath = "/auth/logout";
                });
            builder.Services.AddAuthorization();
        }

        return builder;
    }

        public static WebApplication UseCpSite(this WebApplication app)
    {
        var prefix = new PathString(CpSettings.SitePathPrefix);
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out _, out _),
            branch =>
            {
                branch.Use(RewriteCpPrefix);
                ConfigureCpBranch(branch);
            });
        return app;
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

    private static void ConfigureCpBranch(IApplicationBuilder cp)
    {
        cp.UseDeveloperExceptionPage();
        cp.UseWebSockets();

        CpPipeline.DataServe(cp);

        if (CpSettings.IsSuperHost)
        {
            CpPipeline.ForwarderMode(cp);
            return;
        }

        cp.UseRouting();
        cp.UseSession();
        cp.UseAuthentication();
        cp.UseAuthorization();
        cp.UseClonerCloneSupport();
        cp.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
