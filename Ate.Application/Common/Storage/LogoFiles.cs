using Ate.Application.Common.Interfaces;
using Ate.Domain.Common;
using Ate.Domain.Errors;

namespace Ate.Application.Common.Storage;

public interface ILogoFiles
{
    Task<Result<string>> SaveAsync(string dataUrl, string ownerFolder, Guid ownerId, CancellationToken cancellationToken = default);

    Task DeleteStoredAsync(string? storedValue, CancellationToken cancellationToken = default);

    string? ToPublicUrl(string? storedValue);
}

public sealed class LogoFiles : ILogoFiles, IScopedDependency
{
    public const string Restaurants = "restaurants";
    public const string Companies = "companies";

    private readonly IFileStorage _storage;

    public LogoFiles(IFileStorage storage)
    {
        _storage = storage;
    }

    public async Task<Result<string>> SaveAsync(string dataUrl, string ownerFolder, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var image = ImageDataUrl.TryDecode(dataUrl);
        if (image == null)
        {
            return Result.Failure<string>(FileErrors.InvalidImage);
        }

        var key = StorageKeys.Logo(ownerFolder, ownerId, image.Extension);
        using var content = new MemoryStream(image.Bytes, writable: false);
        await _storage.SaveAsync(FileArea.Public, key, content, image.ContentType, cancellationToken);
        return Result.Success(key);
    }

    public Task DeleteStoredAsync(string? storedValue, CancellationToken cancellationToken = default) =>
        IsStoredKey(storedValue)
            ? _storage.DeleteAsync(FileArea.Public, storedValue!, cancellationToken)
            : Task.CompletedTask;

    public string? ToPublicUrl(string? storedValue) =>
        IsStoredKey(storedValue) ? _storage.GetPublicUrl(storedValue!) : storedValue;

    private static bool IsStoredKey(string? value) =>
        value != null && value.StartsWith("logos/", StringComparison.Ordinal) && StorageKeys.IsValid(value);
}
