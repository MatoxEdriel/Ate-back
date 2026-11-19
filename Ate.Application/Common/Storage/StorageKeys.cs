using System.Text.RegularExpressions;

namespace Ate.Application.Common.Storage;

public static partial class StorageKeys
{
    public const int MaxLength = 300;

    [GeneratedRegex(@"^[a-z0-9_-]+(\.[a-z0-9_-]+)*(/[a-z0-9_-]+(\.[a-z0-9_-]+)*)*$")]
    private static partial Regex SafeKey();

    public static bool IsValid(string? key) =>
        !string.IsNullOrEmpty(key) && key.Length <= MaxLength && SafeKey().IsMatch(key);

    public static string Logo(string ownerFolder, Guid ownerId, string extension) =>
        $"logos/{ownerFolder}/{ownerId:N}/{Guid.NewGuid():N}.{extension}";
}
