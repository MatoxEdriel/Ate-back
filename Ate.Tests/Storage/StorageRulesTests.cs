using Ate.Application.Common.Storage;

namespace Ate.Tests.Storage;

public class StorageRulesTests
{
    private static string DataUrl(string mime, params byte[] bytes) => $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
    private static readonly byte[] Webp = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P', 1];

    [Theory]
    [InlineData("image/png", "png")]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/webp", "webp")]
    public void Decodes_WhenTheBytesMatchTheDeclaredType(string mime, string extension)
    {
        var bytes = mime switch { "image/png" => Png, "image/jpeg" => Jpeg, _ => Webp };

        var image = ImageDataUrl.TryDecode(DataUrl(mime, bytes));

        Assert.NotNull(image);
        Assert.Equal(mime, image!.ContentType);
        Assert.Equal(extension, image.Extension);
        Assert.Equal(bytes, image.Bytes);
    }

    [Fact]
    public void Rejects_WhenTheBytesDoNotMatchTheDeclaredType() =>
        Assert.Null(ImageDataUrl.TryDecode(DataUrl("image/png", Jpeg)));

    [Fact]
    public void Rejects_AnythingThatIsNotAnImageDataUrl()
    {
        Assert.Null(ImageDataUrl.TryDecode("https://example.com/logo.png"));
        Assert.Null(ImageDataUrl.TryDecode("data:image/svg+xml;base64,PHN2Zz4="));
        Assert.Null(ImageDataUrl.TryDecode("data:image/png;base64,***"));
    }

    [Theory]
    [InlineData("logos/restaurants/01a17b66/3f2a.webp", true)]
    [InlineData("logos/../secrets.txt", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("logos\\win.png", false)]
    [InlineData("", false)]
    [InlineData("logos//double.png", false)]
    [InlineData("Logos/Mayus.png", false)]
    public void ValidatesStorageKeys(string key, bool valid) =>
        Assert.Equal(valid, StorageKeys.IsValid(key));
}
