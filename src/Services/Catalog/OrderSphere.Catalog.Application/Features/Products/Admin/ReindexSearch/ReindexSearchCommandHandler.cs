namespace OrderSphere.Catalog.Application.Features.Products.Admin.ReindexSearch;

public sealed class ReindexSearchCommandHandler(IProductSearchIndex searchIndex)
    : ICommandHandler<ReindexSearchCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ReindexSearchCommand request, CancellationToken ct)
    {
        if (!searchIndex.IsEnabled)
            return Result<int>.Failure(ProductErrors.SearchUnavailable);

        var indexed = await searchIndex.ReindexAllAsync(ct);
        return Result<int>.Success(indexed);
    }
}
