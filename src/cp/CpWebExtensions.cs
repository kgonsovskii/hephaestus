using cp.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using model;

namespace cp;

/// <summary>Registers cp services and maps the site under <see cref="CpSettings.SitePathPrefix"/> on the host <see cref="WebApplication"/>.</summary>
public static class CpWebExtensions
{
    /// <summary>Registers control-panel services (MVC, session, auth, BackSvc). Call before <c>Build()</c>.</summary>
    public static WebApplicationBuilder AddCp(this WebApplicationBuilder builder)
    {
        BackSvc.Initialize();

        builder.Services.AddSingleton<ServerService>();
        builder.Services.AddHostedService<BackSvc>();
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
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AllowFromIpRange", policy =>
                    policy.RequireAssertion(context =>
                    {
                        var httpContext = context.Resource as HttpContext;
                        if (httpContext == null)
                            return false;

                        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
                        var isIpAllowed = BackSvc.IsIpAllowed(remoteIp);
                        return isAuthenticated || isIpAllowed;
                    }));
            });
        }

        return builder;
    }

    /// <summary>Maps the cp middleware branch at <see cref="CpSettings.SitePathPrefix"/> (e.g. <c>/cp</c>). Call after <c>Build()</c>, before host middleware that should not see <c>/cp</c> requests.</summary>
    public static WebApplication UseCpSite(this WebApplication app)
    {
        app.Map(CpSettings.SitePathPrefix, ConfigureCpBranch);
        return app;
    }

    private static void ConfigureCpBranch(IApplicationBuilder cp)
    {
        cp.UseDeveloperExceptionPage();

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
        cp.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
