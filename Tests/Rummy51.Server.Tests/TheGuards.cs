namespace Rummy51.Server.Tests;

/// <summary>
/// Where a credential is required, and what comes back without one. Cookie
/// authentication's own answer is a redirect to a login page that does not
/// exist here, which would reach Angular as a stray 404 - replacing it is
/// the whole of why this project configures the scheme at all, and nothing
/// but this notices if the replacement is dropped.
/// </summary>
public class TheGuards(TheServer server) : IClassFixture<TheServer>
{
    [Theory]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("POST", "/api/auth/logout")]
    [InlineData("GET", "/api/tables")]
    [InlineData("POST", "/api/tables")]
    [InlineData("POST", "/api/tables/8a3b0c1d-0000-4000-8000-000000000000/draw")]
    [InlineData("PUT", "/api/account/display-name")]
    [InlineData("POST", "/api/account/password")]
    [InlineData("GET", "/api/players/8a3b0c1d-0000-4000-8000-000000000000/avatar")]
    public async Task Nothing_behind_a_credential_answers_without_one(string method, string path)
    {
        var answer = await Asking(method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode);
    }

    [Fact]
    public async Task And_the_hub_is_behind_one_too()
    {
        var answer = await Asking("GET", "/hubs/lobby");

        Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode);
    }

    /// <summary>A redirect is what this replaced, so it must not be one.</summary>
    [Fact]
    public async Task And_says_so_rather_than_pointing_at_a_page_that_is_not_there()
    {
        var answer = await Asking("GET", "/api/auth/me");

        Assert.Null(answer.Headers.Location);
        Assert.NotEqual(HttpStatusCode.Redirect, answer.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, answer.StatusCode);
    }

    /// <summary>
    /// The way in. A guard on either of these is a door that cannot be
    /// opened from outside, and no test but this would say so.
    ///
    /// Signing in through Google is the third and is not here. Nothing
    /// configures a provider in this host, so the route answers 500 from
    /// inside the action rather than anything about its guard - and a test
    /// reading that as "not 401" would be passing for a reason that has
    /// nothing to do with what it claims.
    /// </summary>
    [Theory]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/auth/register")]
    public async Task But_the_way_in_is_open(string method, string path)
    {
        var answer = await Asking(method, path);

        Assert.NotEqual(HttpStatusCode.Unauthorized, answer.StatusCode);
    }

    /// <summary>
    /// Rule 11.1 over HTTP: a hand is one player's and nobody else's, and
    /// the way that is kept is that no route serves one to a stranger.
    /// </summary>
    [Fact]
    public async Task Rule_11_1_and_a_hand_is_not_served_to_somebody_signed_out()
    {
        var answer = await Asking("GET", "/api/tables/8a3b0c1d-0000-4000-8000-000000000000");

        Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode);
    }

    private async Task<HttpResponseMessage> Asking(string method, string path)
    {
        using var caller = server.Caller();
        using var asked = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = Answered.Sent("{}")
        };

        return await caller.SendAsync(asked);
    }
}
