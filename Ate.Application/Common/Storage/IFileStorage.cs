namespace Ate.Application.Common.Storage;

public interface IFileStorage
{
    Task SaveAsync(FileArea area, string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<StoredFile?> OpenReadAsync(FileArea area, string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(FileArea area, string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(FileArea area, string key, CancellationToken cancellationToken = default);

    string GetPublicUrl(string key);
}

public enum FileArea
{
    Public,
    Private,
}

public sealed record StoredFile(Stream Content, string ContentType, long? Length);
