# Graph Report - src/Services/Catalog  (2026-05-22)

## Corpus Check
- Corpus is ~4,217 words - fits in a single context window. You may not need a graph.

## Summary
- 300 nodes · 295 edges · 54 communities (33 shown, 21 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 14 edges (avg confidence: 0.9)
- Token cost: 14,600 input · 5,700 output

## Community Hubs (Navigation)
- [[_COMMUNITY_CQRS Command Handlers|CQRS Command Handlers]]
- [[_COMMUNITY_API Cross-Cutting Concerns|API Cross-Cutting Concerns]]
- [[_COMMUNITY_Category Domain & DTOs|Category Domain & DTOs]]
- [[_COMMUNITY_Product Infrastructure & DTOs|Product Infrastructure & DTOs]]
- [[_COMMUNITY_Domain Entities|Domain Entities]]
- [[_COMMUNITY_Caching & Authorization|Caching & Authorization]]
- [[_COMMUNITY_gRPC Service|gRPC Service]]
- [[_COMMUNITY_API Configuration|API Configuration]]
- [[_COMMUNITY_FluentValidation|FluentValidation]]
- [[_COMMUNITY_Admin Product Endpoints|Admin Product Endpoints]]
- [[_COMMUNITY_Launch Settings|Launch Settings]]
- [[_COMMUNITY_Public Product Endpoints|Public Product Endpoints]]
- [[_COMMUNITY_Admin Category Endpoints|Admin Category Endpoints]]
- [[_COMMUNITY_EF Entity Configurations|EF Entity Configurations]]
- [[_COMMUNITY_Internal Product Endpoints|Internal Product Endpoints]]
- [[_COMMUNITY_Admin Category CQRS|Admin Category CQRS]]
- [[_COMMUNITY_Domain Errors|Domain Errors]]
- [[_COMMUNITY_DbContext|DbContext]]
- [[_COMMUNITY_Swagger Options|Swagger Options]]
- [[_COMMUNITY_Swagger Extensions|Swagger Extensions]]
- [[_COMMUNITY_Validation Exception Handler|Validation Exception Handler]]
- [[_COMMUNITY_EF Design-Time Factory|EF Design-Time Factory]]
- [[_COMMUNITY_Public Category Endpoints|Public Category Endpoints]]
- [[_COMMUNITY_ICatalogDbContext Interface|ICatalogDbContext Interface]]
- [[_COMMUNITY_Product Admin DTOs|Product Admin DTOs]]
- [[_COMMUNITY_API Versioning|API Versioning]]
- [[_COMMUNITY_Authentication Placeholder|Authentication Placeholder]]
- [[_COMMUNITY_Endpoint Mapping|Endpoint Mapping]]
- [[_COMMUNITY_Application DI|Application DI]]
- [[_COMMUNITY_Infrastructure DI|Infrastructure DI]]
- [[_COMMUNITY_ProductDto|ProductDto]]
- [[_COMMUNITY_App Settings|App Settings]]
- [[_COMMUNITY_AdminCategoryInput|AdminCategoryInput]]
- [[_COMMUNITY_AdminProductInput|AdminProductInput]]
- [[_COMMUNITY_CategoryErrors Node|CategoryErrors Node]]

## God Nodes (most connected - your core abstractions)
1. `Catalog API Program (Entry Point)` - 11 edges
2. `CatalogGrpcService` - 9 edges
3. `EndpointMappingExtensions` - 9 edges
4. `Product Entity` - 9 edges
5. `Product` - 8 edges
6. `Admin ProductEndpoints` - 8 edges
7. `ICatalogDbContext` - 8 edges
8. `ProductEndpoints` - 7 edges
9. `ProductEndpoints` - 7 edges
10. `ICatalogDbContext` - 7 edges

## Surprising Connections (you probably didn't know these)
- `ValidationExceptionHandler` --semantically_similar_to--> `ICatalogDbContext`  [AMBIGUOUS] [semantically similar]
  src/Services/Catalog/OrderSphere.Catalog.Api/Exceptions/ValidationExceptionHandler.cs → src/Services/Catalog/OrderSphere.Catalog.Application/Abstractions/ICatalogDbContext.cs
- `GetCategoriesQueryHandler` --shares_data_with--> `PagedResult`  [EXTRACTED]
  src/Services/Catalog/OrderSphere.Catalog.Application/Features/Categories/Public/GetCategories/GetCategoriesQueryHandler.cs → src/Services/Catalog/OrderSphere.Catalog.Application
- `AuthorizationExtensions` --references--> `string`  [EXTRACTED]
  OrderSphere.Catalog.Api/Configuration/AuthorizationExtensions.cs → OrderSphere.Catalog.Application/Caching/CatalogCache.cs
- `RateLimitingExtensions` --references--> `string`  [EXTRACTED]
  OrderSphere.Catalog.Api/Configuration/RateLimitingExtensions.cs → OrderSphere.Catalog.Application/Caching/CatalogCache.cs
- `Internal ProductEndpoints` --semantically_similar_to--> `Admin ProductEndpoints`  [INFERRED] [semantically similar]
  src/Services/Catalog/OrderSphere.Catalog.Api/Endpoints/Internal/ProductEndpoints.cs → src/Services/Catalog/OrderSphere.Catalog.Api/Endpoints/Admin/ProductEndpoints.cs

## Communities (54 total, 21 thin omitted)

### Community 0 - "CQRS Command Handlers"
Cohesion: 0.05
Nodes (13): CreateCategoryCommandHandler, CreateProductCommandHandler, DeleteCategoryCommandHandler, DeleteProductCommandHandler, GetAllCategoriesAdminQueryHandler, GetAllProductsAdminQueryHandler, GetCategoriesQueryHandler, GetProductByIdAdminQueryHandler (+5 more)

### Community 1 - "API Cross-Cutting Concerns"
Cohesion: 0.08
Nodes (34): LoggingBehavior, ValidationBehavior, Admin ProductEndpoints, AuthenticationExtensions, AuthorizationExtensions, ConfigureSwaggerOptions, EndpointMappingExtensions, CatalogGrpcService (+26 more)

### Community 2 - "Category Domain & DTOs"
Cohesion: 0.12
Nodes (25): AdminCategoryDto, ICatalogDbContext, Category (Domain Entity), CategoryDto, CategoryErrors, CreateCategoryCommand, CreateCategoryCommandHandler, CreateCategoryCommandValidator (+17 more)

### Community 3 - "Product Infrastructure & DTOs"
Cohesion: 0.15
Nodes (23): Admin Product DTO, Auditable Entity (BuildingBlocks), Catalog Cache, Catalog Db Context, Catalog Infrastructure Dependency Injection, Category Entity, Category EF Configuration, Design Time Catalog Db Context Factory (+15 more)

### Community 4 - "Domain Entities"
Cohesion: 0.15
Nodes (3): AuditableEntity, Category, Product

### Community 5 - "Caching & Authorization"
Cohesion: 0.20
Nodes (4): CatalogCache, AuthorizationExtensions, RateLimitingExtensions, string

### Community 7 - "API Configuration"
Cohesion: 0.20
Nodes (9): AllowedHosts, Keycloak, Audience, Authority, Logging, LogLevel, Default, Microsoft.AspNetCore (+1 more)

### Community 8 - "FluentValidation"
Cohesion: 0.22
Nodes (5): AbstractValidator, CreateCategoryCommandValidator, CreateProductCommandValidator, UpdateCategoryCommandValidator, UpdateProductCommandValidator

### Community 10 - "Launch Settings"
Cohesion: 0.25
Nodes (7): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, environmentVariables, launchBrowser, profiles, OrderSphere.Catalog.Api

### Community 13 - "EF Entity Configurations"
Cohesion: 0.29
Nodes (3): CategoryConfiguration, ProductConfiguration, IEntityTypeConfiguration

### Community 15 - "Admin Category CQRS"
Cohesion: 0.33
Nodes (6): Admin CategoryEndpoints, AdminCategoryInput DTO, CreateCategoryCommand, DeleteCategoryCommand, GetAllCategoriesAdminQuery, UpdateCategoryCommand

### Community 16 - "Domain Errors"
Cohesion: 0.40
Nodes (3): Error, CategoryErrors, ProductErrors

### Community 17 - "DbContext"
Cohesion: 0.40
Nodes (3): DbContext, ICatalogDbContext, CatalogDbContext

### Community 24 - "Product Admin DTOs"
Cohesion: 0.67
Nodes (3): AdminProductDto, GetAllProductsAdminQuery, ProductDto

## Ambiguous Edges - Review These
- `ValidationExceptionHandler` → `ICatalogDbContext`  [AMBIGUOUS]
  src/Services/Catalog/OrderSphere.Catalog.Api/Exceptions/ValidationExceptionHandler.cs · relation: semantically_similar_to

## Knowledge Gaps
- **48 isolated node(s):** `Default`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `AllowedHosts`, `Authority` (+43 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `ValidationExceptionHandler` and `ICatalogDbContext`?**
  _Edge tagged AMBIGUOUS (relation: semantically_similar_to) - confidence is low._
- **Why does `EndpointMappingExtensions` connect `API Cross-Cutting Concerns` to `Admin Category CQRS`?**
  _High betweenness centrality (0.009) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `Product Entity` (e.g. with `Get All Products Admin Query Handler` and `Get Product By Id Admin Query Handler`) actually correct?**
  _`Product Entity` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Default`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore` to the rest of the system?**
  _48 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `CQRS Command Handlers` be split into smaller, more focused modules?**
  _Cohesion score 0.05405405405405406 - nodes in this community are weakly interconnected._
- **Should `API Cross-Cutting Concerns` be split into smaller, more focused modules?**
  _Cohesion score 0.08377896613190731 - nodes in this community are weakly interconnected._
- **Should `Category Domain & DTOs` be split into smaller, more focused modules?**
  _Cohesion score 0.12 - nodes in this community are weakly interconnected._