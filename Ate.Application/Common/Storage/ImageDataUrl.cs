using System.Text.RegularExpressions;

namespace Ate.Application.Common.Storage;

public sealed record DecodedImage(byte[] Bytes, string ContentType, string Extension);

public static partial class ImageDataUrl
{
    [GeneratedRegex(@"^data:(image/(?:png|jpeg|webp));base64,([A-Za-z0-9+/]+={0,2})$")]
    private static partial Regex Pattern();

    public static DecodedImage? TryDecode(string? dataUrl)
    {
        if (dataUrl == null)
        {
            return null;
        }

        var match = Pattern().Match(dataUrl);
        if (!match.Success)
        {
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(match.Groups[2].Value);
        }
        catch (FormatException)
        {
            return null;
        }

        var contentType = match.Groups[1].Value;
        var extension = DetectExtension(bytes);
        return extension != null && ContentTypeOf(extension) == contentType
            ? new DecodedImage(bytes, contentType, extension)
            : null;
    }

    private static string? DetectExtension(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "png";
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "jpg";
        }

        if (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return "webp";
        }

        return null;
    }

    private static string ContentTypeOf(string extension) => extension switch
    {
        "png" => "image/png",
        "jpg" => "image/jpeg",
        _ => "image/webp",
    };
}
