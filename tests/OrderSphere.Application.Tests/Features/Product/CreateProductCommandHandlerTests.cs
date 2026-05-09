using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Features.Product.Admin.CreateProduct;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Errors;

namespace OrderSphere.Application.Tests.Features.Product;

public class CreateProductCommandHandlerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public async Task Handle_WithNonPositivePrice_ReturnsInvalidPriceError(decimal price)
    {
        // Arrange
        var dbContext = Substitute.For<IDbContext>();
        var handler = new CreateProductCommandHandler(dbContext, NullLogger<CreateProductCommandHandler>.Instance);
        var input = new AdminProductInput("Name", "Desc", price, 10, Guid.NewGuid(), "SKU");

        // Act
        var result = await handler.Handle(new CreateProductCommand(input), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InvalidPrice);
        await dbContext.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }
}
