namespace OrderSphere.Catalog.Infrastructure.Blob;

public sealed class DisabledBlobStorageService : IBlobStorageService
{
    public static readonly DisabledBlobStorageService Instance = new();

    public bool IsEnabled => false;

    public Task<string> UploadAsync(string blobName, Stream data, string contentType, CancellationToken ct)
        => Task.FromResult(blobName);

    public Task<string> GetSasUrlAsync(string blobName, CancellationToken ct = default)
        => Task.FromResult(string.Empty);
}
