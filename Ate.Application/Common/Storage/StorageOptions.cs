namespace Ate.Application.Common.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = StorageProviders.Local;

    public string PublicBaseUrl { get; set; } = string.Empty;

    public LocalStorageOptions Local { get; set; } = new();
}

public sealed class LocalStorageOptions
{
    public string RootPath { get; set; } = "storage";
}

public static class StorageProviders
{
    public const string Local = "Local";
    public const string AzureBlob = "AzureBlob";
    public const string S3 = "S3";
    public const string Sftp = "Sftp";
}
