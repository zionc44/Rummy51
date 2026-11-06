using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Rummy51.Infrastructure.Identity;

namespace Rummy51.Server.Authentication;

internal static class GoogleAuthenticationSetup
{
    /// <summary>
    /// Adds Google only when credentials are present, so a checkout without
    /// them still starts and still signs people in with a password. Without
    /// this the app would refuse to run on any machine that has not been given
    /// the secret.
    /// </summary>
    public static AuthenticationBuilder AddGoogleIfConfigured(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        var google = configuration
            .GetSection($"{AuthenticationSettings.SectionName}:{nameof(AuthenticationSettings.Google)}")
            .Get<GoogleSettings>() ?? new GoogleSettings();

        if (!google.IsConfigured)
        {
            return builder;
        }

        return builder.AddGoogle(options =>
        {
            options.ClientId = google.ClientId;
            options.ClientSecret = google.ClientSecret;

            // Google's answer lands in Identity's external cookie rather than
            // signing anyone in. Nobody is signed in until we have decided
            // whether this identity may join an account that already exists.
            options.SignInScheme = IdentityConstants.ExternalScheme;

            // Not mapped by default, and it is the whole reason avatars work.
            options.ClaimActions.MapJsonKey("picture", "picture");
        });
    }
}
