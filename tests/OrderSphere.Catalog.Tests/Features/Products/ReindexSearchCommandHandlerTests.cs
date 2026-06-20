using OrderSphere.Catalog.Application.Features.Products.Admin.ReindexSearch;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class ReindexSearchCommandHandlerTests
{
    [Fact]
    public async Task Handle_SearchDisabled_ReturnsSearchUnavailable()
    {
        var search = Substitute.For<IProductSearchIndex>();
        search.IsEnabled.Returns(false);

        var result = await new ReindexSearchCommandHandler(search).Handle(new(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.SearchUnavailable);
        await search.DidNotReceive().ReindexAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SearchEnabled_ReturnsIndexedCount()
    {
        var search = Substitute.For<IProductSearchIndex>();
        search.IsEnabled.Returns(true);
        search.ReindexAllAsync(Arg.Any<CancellationToken>()).Returns(42);

        var result = await new ReindexSearchCommandHandler(search).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }
}
