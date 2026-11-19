using Ate.Application.Common.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _publicBaseUrl;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.Local.RootPath;
        _root = Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured));
        _publicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
    }

    public async Task SaveAsync(FileArea area, string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = PathOf(area, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = path + ".tmp";
        await using (var file = File.Create(temp))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(temp, path, overwrite: true);
    }

    public Task<StoredFile?> OpenReadAsync(FileArea area, string key, CancellationToken cancellationToken = default)
    {
        var path = PathOf(area, key);
        if (!File.Exists(path))
        {
            return Task.FromResult<StoredFile?>(null);
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<StoredFile?>(new StoredFile(stream, ContentTypeOf(key), stream.Length));
    }

    public Task DeleteAsync(FileArea area, string key, CancellationToken cancellationToken = default)
    {
        var path = PathOf(area, key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(FileArea area, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(PathOf(area, key)));

    public string GetPublicUrl(string key) => $"{_publicBaseUrl}/{key}";

    private string PathOf(FileArea area, string key)
    {
        if (!StorageKeys.IsValid(key))
        {
            throw new ArgumentException($"Invalid storage key '{key}'.", nameof(key));
        }

        var areaRoot = Path.Combine(_root, area == FileArea.Public ? "public" : "private");
        var path = Path.GetFullPath(Path.Combine(areaRoot, key));

        if (!path.StartsWith(areaRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Storage key '{key}' escapes the storage root.", nameof(key));
        }

        return path;
    }

    private static string ContentTypeOf(string key) => Path.GetExtension(key) switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };
}
