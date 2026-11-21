using SkiaSharp;

namespace Rummy51.Infrastructure.Avatars;

/// <summary>
/// A picture somebody uploaded, turned into one square picture of our own.
/// Its own thing because it is the only place in the project that reads
/// bytes a stranger chose.
/// </summary>
internal static class SquarePicture
{
    /// <summary>Fifty on the screen, three times that on a dense one.</summary>
    public const int Side = 256;

    /// <summary>
    /// Refused before decoding, because the memory a picture costs to
    /// decode is the picture's size and not the file's.
    /// </summary>
    public const int LargestAccepted = 8000;

    public const string Webp = "image/webp";

    /// <summary>Enough for a face at this size, and well short of lossless.</summary>
    private const int Quality = 80;

    /// <summary>
    /// A square drawn from its pixels, and only its pixels. Nothing the
    /// uploaded file was carrying is read and nothing is copied, so the
    /// metadata goes, and the GPS a phone writes into it goes too.
    ///
    /// Null for anything unreadable or too large - neither is a fault, and
    /// both are what the player is told.
    /// </summary>
    public static async Task<byte[]?> FromAsync(Stream image, CancellationToken cancellationToken)
    {
        using var buffered = new MemoryStream();

        await image.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        try
        {
            return Squared(buffered);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return null;
        }
    }

    private static byte[]? Squared(Stream uploaded)
    {
        // The header alone, which is what makes the size a guard rather
        // than a thing discovered by running out of memory.
        using var codec = SKCodec.Create(uploaded);

        if (codec is null || TooLarge(codec.Info))
        {
            return null;
        }

        using var decoded = SKBitmap.Decode(codec);

        return decoded is null ? null : Drawn(decoded);
    }

    private static byte[] Drawn(SKBitmap decoded)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Side, Side));

        surface.Canvas.DrawBitmap(
            decoded,
            MiddleSquareOf(decoded),
            SKRect.Create(Side, Side),
            // Almost always a reduction, and a cubic one keeps a face
            // readable where dropping pixels does not.
            new SKSamplingOptions(SKCubicResampler.Mitchell));

        using var drawn = surface.Snapshot();
        using var encoded = drawn.Encode(SKEncodedImageFormat.Webp, Quality);

        return encoded.ToArray();
    }

    /// <summary>
    /// What is kept of a picture that is not already square. The player has
    /// usually cropped it themselves, so this is the floor and not the
    /// feature.
    /// </summary>
    private static SKRect MiddleSquareOf(SKBitmap decoded)
    {
        var side = Math.Min(decoded.Width, decoded.Height);

        return SKRect.Create((decoded.Width - side) / 2f, (decoded.Height - side) / 2f, side, side);
    }

    private static bool TooLarge(SKImageInfo described) =>
        described.Width > LargestAccepted || described.Height > LargestAccepted;
}
