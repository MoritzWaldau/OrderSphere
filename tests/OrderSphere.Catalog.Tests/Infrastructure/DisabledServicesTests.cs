namespace OrderSphere.Catalog.Tests.Infrastructure;

/// <summary>
/// The no-op blob and search services used when Azure is not configured. They must report
/// disabled and degrade silently so the catalog falls back to the database.
/// </summary>
public sealed class DisabledServicesTests
{

    [Fact]
    public void DisabledBlob_IsDisabled()
        => DisabledBlobStorageService.Instance.IsEnabled.Should().BeFalse();

    [Fact]
    public async Task DisabledBlob_Upload_EchoesBlobName()
    {
        using var data = new MemoryStream([1, 2, 3]);

        var name = await DisabledBlobStorageService.Instance
            .UploadAsync("image.png", data, "image/png", default);

        name.Should().Be("image.png");
    }

    [Fact]
    public async Task DisabledBlob_GetSasUrl_ReturnsEmpty()
        => (await DisabledBlobStorageService.Instance.GetSasUrlAsync("image.png"))
            .Should().BeEmpty();


    [Fact]
    public void DisabledSearch_IsDisabled()
        => DisabledProductSearchIndex.Instance.IsEnabled.Should().BeFalse();

    [Fact]
    public async Task DisabledSearch_Search_ReturnsEmptyPage()
    {
        var page = await DisabledProductSearchIndex.Instance.SearchAsync(
            new ProductSearchCriteria("anything", null, null, null, 0, 20), default);

        page.ProductIds.Should().BeEmpty();
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task DisabledSearch_ReindexAll_ReturnsZero()
        => (await DisabledProductSearchIndex.Instance.ReindexAllAsync(default)).Should().Be(0);

    [Fact]
    public async Task DisabledSearch_WritesAreNoOps()
    {
        var index = DisabledProductSearchIndex.Instance;

        // None of these should throw or require configuration.
        await index.SyncAsync(Guid.NewGuid(), default);
        await index.RemoveAsync(Guid.NewGuid(), default);
        await index.EnsureSeededAsync(default);
    }
}
