using System.Text.Json;

namespace Rummy51.Server.Tests;

/// <summary>
/// What crosses when nothing went wrong. Two promises live here and both
/// are kept by configuration rather than by code, which is exactly the
/// kind that is dropped without anybody noticing.
/// </summary>
public class TheWire(TheServer server) : IClassFixture<TheServer>
{
    /// <summary>
    /// Names and not ordinals, so a client switching on "provider" keeps
    /// working when a value is inserted above it.
    /// </summary>
    [Fact]
    public async Task An_enum_crosses_as_its_name()
    {
        var player = await SigningIn();

        Assert.Equal(JsonValueKind.String, player.GetProperty("avatarSource").ValueKind);
        Assert.Equal("provider", player.GetProperty("avatarSource").GetString());
    }

    [Fact]
    public async Task And_a_property_as_its_name_in_camel_case()
    {
        var player = await SigningIn();

        Assert.True(player.TryGetProperty("displayName", out _));
        Assert.False(player.TryGetProperty("DisplayName", out _));
    }

    /// <summary>
    /// What the client is told about a player never includes a secret, and
    /// Identity hangs several off the entity this is built from.
    /// </summary>
    [Fact]
    public async Task And_nothing_about_a_player_that_is_a_secret()
    {
        var player = await SigningIn();

        Assert.Equal(
            ["avatarSource", "avatarUrl", "displayName", "email", "hasPassword",
             "playerId", "providerAvatarUrl", "uploadedAvatarUrl"],
            player.EnumerateObject().Select(one => one.Name).Order());
    }

    /// <summary>A player who did not exist a moment ago, which is a 201.</summary>
    [Fact]
    public async Task Registering_answers_with_the_code_for_something_made()
    {
        using var caller = server.Caller();
        var answer = await caller.PostAsync("/api/auth/register", Answered.Sent(
            """{ "email": "a@example.test", "password": "a password", "displayName": "Somebody" }"""));

        Assert.Equal(HttpStatusCode.Created, answer.StatusCode);
    }

    private async Task<JsonElement> SigningIn()
    {
        server.Accounts.Succeeding();

        using var caller = server.Caller();
        var answer = await caller.PostAsync("/api/auth/login", Answered.Sent(
            """{ "email": "somebody@example.test", "password": "a password" }"""));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);

        using var body = JsonDocument.Parse(await answer.Content.ReadAsStringAsync());

        return body.RootElement.Clone();
    }
}
