#:package Npgsql@10.0.3

// Development only. What is actually stored as each player's avatar, read
// out of the database rather than fetched through the server.
//
//   dotnet run scripts/avatars.cs
//
// A WebP is a RIFF container: four bytes saying RIFF, a length, four more
// saying WEBP, and then chunks of a fourcc and a length each. Only VP8 or
// VP8L is the picture. Anything else listed beside it came from the file
// somebody uploaded - EXIF is where a phone writes GPS, ICCP is a colour
// profile, XMP is whatever the editor felt like keeping.
//
// The point of re-encoding on the server is that none of those survive, and
// for a long time they did: the encoder copied them across, the code said
// otherwise and so did CLAUDE.md. A row still showing one was stored before
// that was fixed, and uploading again clears it.

using System.Text;
using Npgsql;

var settings = Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..",
    "Rummy51.Server", "appsettings.Development.json");

if (!File.Exists(settings))
{
    settings = Path.Combine(Directory.GetCurrentDirectory(),
        "Rummy51.Server", "appsettings.Development.json");
}

var json = File.ReadAllText(settings);
var at = json.IndexOf("\"Rummy51Db\"", StringComparison.Ordinal);
var opens = json.IndexOf('"', json.IndexOf(':', at) + 1);
var connection = json[(opens + 1)..json.IndexOf('"', opens + 1)];

await using var database = new NpgsqlConnection(connection);
await database.OpenAsync();

await using var asked = new NpgsqlCommand(
    """
    select player_id, content_type, length(image), updated_at, image
    from player_avatars
    order by updated_at desc
    """,
    database);

await using var rows = await asked.ExecuteReaderAsync();
var found = 0;

while (await rows.ReadAsync())
{
    found++;

    var image = (byte[])rows[4];

    Console.WriteLine($"{rows.GetGuid(0)}  {rows.GetString(1)}  {rows.GetInt32(2)}b  {rows.GetDateTime(3):u}");
    Console.WriteLine($"    {Describe(image)}");
}

Console.WriteLine(found == 0 ? "No avatars stored." : $"\n{found} stored.");

static string Describe(byte[] image)
{
    if (image.Length < 12 || Encoding.ASCII.GetString(image, 0, 4) != "RIFF")
    {
        return "not a RIFF container, which nothing here should produce";
    }

    var said = new StringBuilder();
    var carried = new List<string>();

    for (var at = 12; at + 8 <= image.Length;)
    {
        var fourcc = Encoding.ASCII.GetString(image, at, 4).TrimEnd();
        var size = BitConverter.ToInt32(image, at + 4);

        said.Append($"{fourcc}={size} ");

        if (fourcc is "EXIF" or "ICCP" or "XMP")
        {
            carried.Add(fourcc);
        }

        at += 8 + size + (size % 2);
    }

    return carried.Count == 0
        ? $"{said}- pixels only"
        : $"{said}<- still carrying {string.Join(" and ", carried)} from the uploaded file";
}
