namespace OrderSphere.Catalog.Application.Features.Products.Admin.ReindexSearch;

/// <summary>Rebuilds the Azure AI Search index from the catalog database. Admin-only.</summary>
public sealed record ReindexSearchCommand : ICommand<Result<int>>;
