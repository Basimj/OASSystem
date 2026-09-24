using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Inventory;
using OAS.Domain.Entities.Inventory;
using InventoryUnit = OAS.Domain.Entities.Inventory.Unit;

namespace OAS.Application.Inventory.Services;

public sealed class InventoryCodeGenerator(
    ISequenceNumberGenerator sequenceNumberGenerator,
    IReadRepository<Product, Guid> productRepository,
    IReadRepository<Brand, Guid> brandRepository,
    IReadRepository<ProductCategory, Guid> categoryRepository,
    IReadRepository<ProductType, Guid> productTypeRepository,
    IReadRepository<InventoryUnit, Guid> unitRepository,
    IReadRepository<Warehouse, Guid> warehouseRepository,
    IReadRepository<ProductVariant, Guid> variantRepository) : IInventoryCodeGenerator
{
    public async Task<string> NextAsync(string kind, CancellationToken cancellationToken = default)
    {
        var normalized = kind.Trim().ToLowerInvariant();
        var (prefix, sequenceName) = normalized switch
        {
            InventoryCodeKinds.Product => ("P", "InventoryCode_Product"),
            InventoryCodeKinds.Brand => ("B", "InventoryCode_Brand"),
            InventoryCodeKinds.ProductCategory => ("C", "InventoryCode_ProductCategory"),
            InventoryCodeKinds.ProductType => ("T", "InventoryCode_ProductType"),
            InventoryCodeKinds.Unit => ("U", "InventoryCode_Unit"),
            InventoryCodeKinds.Warehouse => ("W", "InventoryCode_Warehouse"),
            InventoryCodeKinds.ProductVariant => ("V", "InventoryCode_ProductVariant"),
            _ => throw InvalidKind()
        };

        // Sequences reserve numbers atomically. The existence check also makes this safe
        // for databases that already contain manually-entered codes in the new format.
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var number = await sequenceNumberGenerator.NextAsync(sequenceName, cancellationToken);
            var code = $"{prefix}-{number:000000}";
            if (!await ExistsAsync(normalized, code, cancellationToken))
                return code;
        }

        throw new ConflictException("inventory_code_generation_exhausted", "تعذر توليد كود فريد تلقائيًا. حاول مرة أخرى.");
    }

    private Task<bool> ExistsAsync(string kind, string code, CancellationToken cancellationToken) => kind switch
    {
        InventoryCodeKinds.Product => Exists(productRepository, new Specification<Product>().Where(x => x.ProductCode == code), cancellationToken),
        InventoryCodeKinds.Brand => Exists(brandRepository, new Specification<Brand>().Where(x => x.Code == code), cancellationToken),
        InventoryCodeKinds.ProductCategory => Exists(categoryRepository, new Specification<ProductCategory>().Where(x => x.Code == code), cancellationToken),
        InventoryCodeKinds.ProductType => Exists(productTypeRepository, new Specification<ProductType>().Where(x => x.Code == code), cancellationToken),
        InventoryCodeKinds.Unit => Exists(unitRepository, new Specification<InventoryUnit>().Where(x => x.Code == code), cancellationToken),
        InventoryCodeKinds.Warehouse => Exists(warehouseRepository, new Specification<Warehouse>().Where(x => x.Code == code), cancellationToken),
        InventoryCodeKinds.ProductVariant => Exists(variantRepository, new Specification<ProductVariant>().Where(x => x.SKU == code), cancellationToken),
        _ => throw InvalidKind()
    };

    private static async Task<bool> Exists<TEntity>(
        IReadRepository<TEntity, Guid> repository,
        Specification<TEntity> specification,
        CancellationToken cancellationToken)
        where TEntity : OAS.Domain.Common.Entities.Entity<Guid> =>
        await repository.CountAsync(specification, cancellationToken) > 0;

    private static RequestValidationException InvalidKind() =>
        new(new Dictionary<string, string[]>
        {
            ["kind"] = ["inventory_code_kind_invalid: نوع كود المخزون غير صالح."]
        });
}
