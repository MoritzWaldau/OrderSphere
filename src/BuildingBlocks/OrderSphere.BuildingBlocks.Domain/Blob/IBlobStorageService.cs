namespace OrderSphere.BuildingBlocks.Blob;

public interface IBlobStorageService
{
    bool IsEnabled { get; }

    Task<string> UploadAsync(string blobName, Stream data, string contentType, CancellationToken ct);

    Task<string> GetSasUrlAsync(string blobName, CancellationToken ct = default);
}
