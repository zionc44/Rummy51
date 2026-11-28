using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Rummy51.Server.Tests;

/// <summary>
/// Caddy ends TLS and forwards plain HTTP. A request it was asked over HTTPS
/// has to reach the application as one, or the redirect to HTTPS sends the
/// browser back to Caddy for ever - on the first request of the first
/// deployment, and nowhere on a developer's machine.
/// </summary>
public class BehindTheProxy(TheServer server) : IClassFixture<TheServer>
{
    [Fact]
    public async Task A_request_the_proxy_was_asked_over_https_reaches_the_application()
    {
        var answer = await Asking(forwardedAs: "https");

        Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode);
    }

    /// <summary>The test above passes for nothing unless the redirect is really there.</summary>
    [Fact]
    public async Task While_one_it_was_asked_over_http_is_still_sent_to_https()
    {
        var answer = await Asking(forwardedAs: "http");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, answer.StatusCode);
    }

    private async Task<HttpResponseMessage> Asking(string forwardedAs)
    {
        using var caller = WithAnHttpsPort().CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        request.Headers.Add("X-Forwarded-Proto", forwardedAs);

        return await caller.SendAsync(request);
    }

    // Without a port to send it to, the redirect does nothing at all, and both
    // tests would pass with the forwarded headers or without them.
    private WebApplicationFactory<Program> WithAnHttpsPort() =>
        server.WithWebHostBuilder(builder => builder.UseSetting("https_port", "443"));
}
