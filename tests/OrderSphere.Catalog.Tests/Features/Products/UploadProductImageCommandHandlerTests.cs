using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class UploadProductImageCommandHandlerTests
{
    private static UploadProductImageCommand Command(ProductId id, string fileName = "photo.png")
        => new(id, Stream.Null, "image/png", fileName);

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var blob = Substitute.For<IBlobStorageService>();
        var search = Substitute.For<IProductSearchIndex>();

        var result = await new UploadProductImageCommandHandler(ctx, blob, search)
            .Handle(Command(ProductId.New()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
        await blob.DidNotReceive()
            .UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingProduct_UploadsBlob_SetsBlobName_Syncs()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Shoes");
        var product = new Product("Air", "d", Money.Of(10m), 5, category.Id, "SKU-1");
        ctx.Categories.Add(category);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var blob = Substitute.For<IBlobStorageService>();
        var search = Substitute.For<IProductSearchIndex>();

        var result = await new UploadProductImageCommandHandler(ctx, blob, search)
            .Handle(Command(product.Id, "photo.png"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith($"products/{product.Id.Value}/").And.EndWith(".png");
        await blob.Received(1).UploadAsync(result.Value, Stream.Null, "image/png", Arg.Any<CancellationToken>());
        await search.Received(1).SyncAsync(product.Id.Value, Arg.Any<CancellationToken>());
        var stored = await ctx.Products.SingleAsync(p => p.Id == product.Id);
        stored.ImageBlobName.Should().Be(result.Value);
    }

    [Theory]
    [InlineData("photo.jpeg", ".jpg")]
    [InlineData("photo.gif", ".gif")]
    [InlineData("photo.bmp", ".jpg")] // unknown extension falls back to .jpg
    public async Task Handle_MapsExtensionFromFileName(string fileName, string expectedExtension)
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Shoes");
        var product = new Product("Air", "d", Money.Of(10m), 5, category.Id, "SKU-1");
        ctx.Categories.Add(category);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var result = await new UploadProductImageCommandHandler(
                ctx, Substitute.For<IBlobStorageService>(), Substitute.For<IProductSearchIndex>())
            .Handle(Command(product.Id, fileName), default);

        result.Value.Should().EndWith(expectedExtension);
    }
}
