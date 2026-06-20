namespace OrderSphere.Catalog.Application.Abstractions;

public interface IBlobStorageService
{
    bool IsEnabled { get; }

    /// <summary>Uploads a stream to blob storage under the given name and returns the blob name.</summary>
    Task<string> UploadAsync(string blobName, Stream data, string contentType, CancellationToken ct);

    /// <summary>Generates a short-lived (1h) read-only SAS URL via User Delegation Key.</summary>
    Task<string> GetSasUrlAsync(string blobName, CancellationToken ct = default);
}
