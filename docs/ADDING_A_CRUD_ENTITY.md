# Adding a simple CRUD entity

Use generic CRUD only for simple/master-data entities. Business aggregates with posting, inventory, accounting, or workflow side effects must use explicit application use cases.

1. Add the entity under `OAS.Domain/Features/<Feature>/...` and derive from `Entity<TKey>`, `AuditableEntity<TKey>`, or `SoftDeletableEntity<TKey>`.
2. Add read/create/update DTOs in `OAS.Contracts/Features/<Feature>/...`.
3. Add FluentValidation validators for create/update DTOs when needed.
4. Add an EF `IEntityTypeConfiguration<TEntity>` in Infrastructure.
5. Use the convention mapper by default; register a feature-specific `ICrudMapper<TEntity,TKey,TReadDto,TCreateDto,TUpdateDto>` only when custom mapping is required.
6. Register one line with `AddCrudFeature<TEntity,TKey,TReadDto,TCreateDto,TUpdateDto>()`.
7. Add a thin API controller inheriting `CrudControllerBase<TKey,...>`.
8. Add a thin client service inheriting `CrudClientService<TKey,...>`.
9. Add feature-specific commands/queries/repository methods only when the generic behavior is insufficient.

No per-entity Add/Delete/Update repository implementation or five duplicated CRUD handlers are required.
