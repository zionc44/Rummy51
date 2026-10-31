using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Rummy51.Infrastructure.Identity;

namespace Rummy51.Server.Authentication;

/// <summary>
/// How the browser is authenticated is a concern of the host, not of the
/// storage layer, so the cookie is configured here rather than alongside the
/// Identity stores.
/// </summary>
internal static class CookieAuthenticationSetup
{
    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptionsMonitor<AuthenticationSettings>>(Configure);

        return services;
    }

    private static void Configure(
        CookieAuthenticationOptions cookie,
        IOptionsMonitor<AuthenticationSettings> settings)
    {
        cookie.Cookie.Name = "rummy51.auth";
        // JavaScript cannot read it, so cross-site scripting cannot steal it.
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Lax, not Strict: it still withholds the cookie from a cross-site POST,
        // which is the CSRF vector, but a player arriving from an outside link
        // does not land on the site looking signed out.
        cookie.Cookie.SameSite = SameSiteMode.Lax;

        cookie.ExpireTimeSpan = TimeSpan.FromMinutes(settings.CurrentValue.IdleTimeoutMinutes);
        // Every request restarts the clock, so the setting above is an idle
        // timeout rather than a session length.
        cookie.SlidingExpiration = true;

        // This is an API. The defaults redirect an unauthenticated caller to a
        // login page that does not exist, which reaches Angular as a stray 404.
        cookie.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        cookie.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }
}
