# OASSystem

Clean Architecture foundation for the Optical System (OAS), extracted conceptually from the proven project structure while removing all hospital/medical domain code.

## Projects

- `OAS.Domain`: pure domain model. No EF Core, MediatR, ASP.NET Core, or infrastructure dependencies.
- `OAS.Contracts`: DTOs and transport-neutral contracts.
- `OAS.Application`: use cases, generic CRUD/CQRS, validation, mapping, and application abstractions.
- `OAS.Infrastructure`: EF Core 9, SQL Server, repositories, transactions, persistence interceptors.
- `OAS.API`: ASP.NET Core 9 host, controllers, security, middleware.
- `OAS.Client`: Blazor WebAssembly. It references only `OAS.Contracts` and `OAS.UiLib` from OAS projects.
- `OAS.UiLib`: reusable UI library. It contains only general-purpose presentation components; no business feature owns UI components inside the library.
- `OAS.Templates`: reserved for print/export templates.
- `OAS.Tests`: architecture and future unit/integration tests.

## UI library rules

The only shared CSS files inside `OAS.UiLib` are design tokens: `variables.css`, `colors.css`, and `sizing.css`. Every component owns its isolated `.razor.css` file. UI localization uses `IStringLocalizer<UiLibSharedResources>` injected as `L` through `_Imports.razor`. Client/business text uses its own `IStringLocalizer<SharedResources>` and resources.

## Generic CRUD

Generic CRUD is opt-in. A future simple entity is registered explicitly with:

```csharp
services.AddCrudFeature<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>();
```

The framework then supplies repository operations, generic create/update/delete commands, get-by-id/page queries, handlers, transaction/save behavior, validation pipeline, conventional search/sort/paging, and application service.

A thin controller only inherits:

```csharp
public sealed class BrandsController(ICrudApplicationService<Guid, BrandDto, CreateBrandDto, UpdateBrandDto> service)
    : CrudControllerBase<Guid, BrandDto, CreateBrandDto, UpdateBrandDto>(service);
```

A client service only inherits:

```csharp
public sealed class BrandClient(OasApiClient api)
    : CrudClientService<Guid, BrandDto, CreateBrandDto, UpdateBrandDto>(api, "api/brands");
```

Complex aggregates such as sales, purchases, invoices, inventory transfers, or posting workflows should use explicit use cases rather than generic CRUD.

Identity is implemented vertically under an `Identity` folder in every participating layer, while UiLib remains feature-agnostic. See `docs/ARCHITECTURE.md`, `docs/IDENTITY.md`, and `docs/ADDING_A_CRUD_ENTITY.md`.
