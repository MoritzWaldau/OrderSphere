using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace OrderSphere.Catalog.Infrastructure.Blob;

/// <summary>
/// Singleton holder for the Azure Blob Storage service client. Expensive to construct and
/// thread-safe, so built once and shared. Caches the User Delegation Key (valid 23h,
/// refreshed when fewer than 10 minutes remain) to avoid a round-trip on every SAS URL
/// generation. Disabled when Blob:Endpoint / ConnectionStrings:images is not configured.
/// </summary>
public sealed class BlobStorageClients : IDisposable
{
    private const int SasExpiryHours = 1;
    private const int KeyValidityHours = 23;
    private const int KeyRefreshBufferMinutes = 10;

    public bool IsEnabled { get; }
    public string ContainerName { get; }
    public BlobContainerClient? Container { get; }

    private readonly BlobServiceClient? _serviceClient;
    private UserDelegationKey? _delegationKey;
    private DateTimeOffset _keyExpiresAt;
    private readonly SemaphoreSlim _keyLock = new(1, 1);

    public BlobStorageClients(IConfiguration configuration)
    {
        // appsettings ships Blob:Endpoint as "" (not null); coalesce on whitespace so
        // the Aspire-injected connection string wins over the empty local default.
        var rawEndpoint = configuration["Blob:Endpoint"];
        if (string.IsNullOrWhiteSpace(rawEndpoint))
            rawEndpoint = configuration.GetConnectionString("images");
        var endpoint = NormalizeEndpoint(rawEndpoint);

        ContainerName = configuration["Blob:ContainerName"] ?? "product-images";

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            IsEnabled = false;
            return;
        }

        var credential = new DefaultAzureCredential();
        _serviceClient = new BlobServiceClient(new Uri(endpoint), credential);
        Container = _serviceClient.GetBlobContainerClient(ContainerName);
        IsEnabled = true;
    }

    /// <summary>
    /// Generates a read-only SAS URL valid for <see cref="SasExpiryHours"/> hour(s)
    /// using a cached User Delegation Key. Thread-safe.
    /// </summary>
    public async Task<string> GetSasUrlAsync(string blobName, CancellationToken ct = default)
    {
        var key = await GetOrRefreshKeyAsync(ct);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(SasExpiryHours),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasParams = sasBuilder.ToSasQueryParameters(key, _serviceClient!.AccountName);
        var uriBuilder = new BlobUriBuilder(_serviceClient.Uri)
        {
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Sas = sasParams,
        };
        return uriBuilder.ToUri().ToString();
    }

    private async Task<UserDelegationKey> GetOrRefreshKeyAsync(CancellationToken ct)
    {
        if (_delegationKey is not null
            && DateTimeOffset.UtcNow < _keyExpiresAt.AddMinutes(-KeyRefreshBufferMinutes))
            return _delegationKey;

        await _keyLock.WaitAsync(ct);
        try
        {
            if (_delegationKey is not null
                && DateTimeOffset.UtcNow < _keyExpiresAt.AddMinutes(-KeyRefreshBufferMinutes))
                return _delegationKey;

            var expiresOn = DateTimeOffset.UtcNow.AddHours(KeyValidityHours);
            var response = await _serviceClient!.GetUserDelegationKeyAsync(
                DateTimeOffset.UtcNow, expiresOn, ct);
            _delegationKey = response.Value;
            _keyExpiresAt = expiresOn;
            return _delegationKey;
        }
        finally
        {
            _keyLock.Release();
        }
    }

    // Aspire injects the blob service endpoint as a bare URL for RBAC-only accounts.
    // Handle both bare URL and the BlobEndpoint= key-value form as a safety net.
    private static string? NormalizeEndpoint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('='))
            return value;

        foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("BlobEndpoint=", StringComparison.OrdinalIgnoreCase))
                return part["BlobEndpoint=".Length..];
        }

        return value;
    }

    public void Dispose() => _keyLock.Dispose();
}
