using System.Text;
using System.Text.Json;

namespace Rummy51.Server.Tests;

/// <summary>
/// A failure as the client actually receives it: parsed out of the body
/// rather than read off the object the controller returned, since the
/// whole claim is about what crossed the wire.
/// </summary>
public sealed record Answered(
    HttpStatusCode Status,
    string? MediaType,
    string? Title,
    string? TraceId,
    IReadOnlyList<Answered.Failed> Errors)
{
    public sealed record Failed(string Code, IReadOnlyList<string> Parameters);

    public static async Task<Answered> Of(HttpResponseMessage answer)
    {
        using var body = JsonDocument.Parse(await answer.Content.ReadAsStringAsync());
        var root = body.RootElement;

        return new Answered(
            answer.StatusCode,
            answer.Content.Headers.ContentType?.MediaType,
            Text(root, "title"),
            Text(root, "traceId"),
            [.. Listed(root).Select(Read)]);
    }

    /// <summary>JSON without a body of its own, for the posts that send one.</summary>
    public static StringContent Sent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    public IReadOnlyList<string> Codes => [.. Errors.Select(error => error.Code)];

    private static Failed Read(JsonElement error) =>
        new(
            error.GetProperty("code").GetString()!,
            [.. error.GetProperty("parameters").EnumerateArray().Select(value => value.GetString()!)]);

    private static IEnumerable<JsonElement> Listed(JsonElement root) =>
        root.TryGetProperty("errors", out var errors) ? errors.EnumerateArray() : [];

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) ? value.GetString() : null;
}
