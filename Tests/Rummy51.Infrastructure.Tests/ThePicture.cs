using System.Text;
using Rummy51.Infrastructure.Avatars;

namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// The one place in the project that reads bytes a stranger chose. What
/// it must do is narrow and is not a rule: a square of a known size, in
/// one known format, carrying nothing the uploaded file carried.
///
/// Nothing here asks an imaging library what came out. The size is read
/// out of the WebP container by hand, so that what checks the picture is
/// not also what drew it - which is what lets the library underneath be
/// changed and the answer still mean something.
/// </summary>
public class ThePicture
{
    /// <summary>A string no encoder would produce, written into the EXIF.</summary>
    private const string InTheMetadata = "RUMMY51FIXTUREGPS";

    [Fact]
    public async Task A_picture_comes_back_as_one_webp()
    {
        var square = await Squared("wide-with-exif.jpg");

        Assert.NotNull(square);
        Assert.Equal("RIFF", Ascii(square, 0, 4));
        Assert.Equal("WEBP", Ascii(square, 8, 4));
    }

    [Fact]
    public async Task And_square_at_the_size_the_screen_asks_for()
    {
        var square = await Squared("wide-with-exif.jpg");

        Assert.Equal((SquarePicture.Side, SquarePicture.Side), SizeOf(square!));
    }

    /// <summary>
    /// The whole reason the server re-encodes a picture the browser has
    /// already cropped: a phone writes where you were standing into the
    /// file, and the client cannot be trusted to have removed it.
    /// </summary>
    [Fact]
    public async Task And_nothing_the_uploaded_file_was_carrying()
    {
        var uploaded = await File.ReadAllBytesAsync(Path.Combine("Pictures", "wide-with-exif.jpg"));
        var square = await Squared("wide-with-exif.jpg");

        Assert.Contains(InTheMetadata, Encoding.ASCII.GetString(uploaded));
        Assert.DoesNotContain(InTheMetadata, Encoding.ASCII.GetString(square!));
    }

    /// <summary>
    /// Refused on the header alone. A small file can describe an enormous
    /// picture, and the memory to decode one is the picture's size.
    /// </summary>
    [Fact]
    public async Task A_picture_too_large_to_decode_is_refused_before_it_is()
    {
        Assert.Null(await Squared("too-big.jpg"));
    }

    [Fact]
    public async Task And_so_is_anything_that_is_not_a_picture_at_all()
    {
        Assert.Null(await From("this is not a picture"u8.ToArray()));
        Assert.Null(await From([]));
    }

    private static async Task<byte[]?> Squared(string name) =>
        await From(await File.ReadAllBytesAsync(Path.Combine("Pictures", name)));

    private static async Task<byte[]?> From(byte[] bytes)
    {
        using var uploaded = new MemoryStream(bytes);

        return await SquarePicture.FromAsync(uploaded, CancellationToken.None);
    }

    /// <summary>
    /// What a WebP says it is, read out of its container. The three shapes
    /// are lossy, lossless and extended, and an encoder may choose any of
    /// them - which is itself a reason not to ask the encoder.
    /// </summary>
    private static (int Width, int Height)? SizeOf(byte[] webp)
    {
        if (webp.Length < 30 || Ascii(webp, 0, 4) != "RIFF" || Ascii(webp, 8, 4) != "WEBP")
        {
            return null;
        }

        return Ascii(webp, 12, 4) switch
        {
            "VP8 " => (Short(webp, 26) & 0x3FFF, Short(webp, 28) & 0x3FFF),
            "VP8L" => ((Word(webp, 21) & 0x3FFF) + 1, ((Word(webp, 21) >> 14) & 0x3FFF) + 1),
            "VP8X" => (Triple(webp, 24) + 1, Triple(webp, 27) + 1),
            _ => null
        };
    }

    private static string Ascii(byte[]? bytes, int at, int length) =>
        bytes is null ? string.Empty : Encoding.ASCII.GetString(bytes, at, length);

    private static int Short(byte[] bytes, int at) => bytes[at] | (bytes[at + 1] << 8);

    private static int Triple(byte[] bytes, int at) => Short(bytes, at) | (bytes[at + 2] << 16);

    private static int Word(byte[] bytes, int at) => Triple(bytes, at) | (bytes[at + 3] << 24);
}
