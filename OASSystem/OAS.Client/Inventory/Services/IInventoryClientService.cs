using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;
using OAS.Contracts.Inventory.Products;
using OAS.Contracts.Inventory.Stock;
using OAS.Contracts.Inventory.Transactions;
using OAS.Contracts.Inventory.Warehouses;

namespace OAS.Client.Inventory.Services;

public interface IInventoryClientService
{
    Task<InventoryCodeSuggestionDto?> GetNextCodeAsync(string kind, CancellationToken cancellationToken = default);

    // Product categories
    Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ProductCategoryDto?> GetProductCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductCategoryDto?> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ProductCategoryDto?> UpdateProductCategoryAsync(Guid id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default);

    // Brands
    Task<PagedResult<BrandDto>> GetBrandsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BrandDto?> GetBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto?> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken = default);

    // Product types
    Task<PagedResult<ProductTypeDto>> GetProductTypesAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ProductTypeDto?> GetProductTypeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductTypeDto?> CreateProductTypeAsync(CreateProductTypeRequest request, CancellationToken cancellationToken = default);
    Task<ProductTypeDto?> UpdateProductTypeAsync(Guid id, UpdateProductTypeRequest request, CancellationToken cancellationToken = default);

    // Units
    Task<PagedResult<UnitDto>> GetUnitsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<UnitDto?> GetUnitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitDto?> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitDto?> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default);

    // Products
    Task<PagedResult<ProductDto>> GetProductsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<CreateStockProductResult?> CreateStockProductAsync(CreateStockProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    // Product variants
    Task<PagedResult<ProductVariantDto>> GetProductVariantsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantDto?> GetProductVariantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductVariantDto?> CreateProductVariantAsync(CreateProductVariantRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantDto?> UpdateProductVariantAsync(Guid id, UpdateProductVariantRequest request, CancellationToken cancellationToken = default);

    // Frame details
    Task<PagedResult<FrameDetailsDto>> GetFrameDetailsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<FrameDetailsDto?> GetFrameDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FrameDetailsDto?> CreateFrameDetailsAsync(CreateFrameDetailsRequest request, CancellationToken cancellationToken = default);
    Task<FrameDetailsDto?> UpdateFrameDetailsAsync(Guid id, UpdateFrameDetailsRequest request, CancellationToken cancellationToken = default);

    // Lens details
    Task<PagedResult<LensDetailsDto>> GetLensDetailsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<LensDetailsDto?> GetLensDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LensDetailsDto?> CreateLensDetailsAsync(CreateLensDetailsRequest request, CancellationToken cancellationToken = default);
    Task<LensDetailsDto?> UpdateLensDetailsAsync(Guid id, UpdateLensDetailsRequest request, CancellationToken cancellationToken = default);

    // Warehouses
    Task<PagedResult<WarehouseDto>> GetWarehousesAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetWarehouseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);

    // Inventory balances - read only
    Task<PagedResult<InventoryBalanceDto>> GetInventoryBalancesAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default);
    Task<InventoryBalanceDto?> GetInventoryBalanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryBalanceDto?> GetInventoryBalanceAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default);

    // Inventory transactions
    Task<PagedResult<InventoryTransactionDto>> GetInventoryTransactionsAsync(
        PageRequest request,
        InventoryTransactionType? transactionType = null,
        InventoryTransactionStatus? status = null,
        Guid? warehouseId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
    Task<InventoryTransactionDto?> GetInventoryTransactionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTransactionLineDto>> GetInventoryTransactionLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryTransactionDto?> CreateInventoryTransactionAsync(CreateInventoryTransactionRequest request, CancellationToken cancellationToken = default);
    Task<InventoryTransactionDto?> PostInventoryTransactionAsync(Guid id, PostInventoryTransactionRequest request, CancellationToken cancellationToken = default);

    // Inventory ledger - read only
    Task<PagedResult<InventoryLedgerDto>> GetInventoryLedgerAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        InventoryMovementType? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
    Task<InventoryLedgerDto?> GetInventoryLedgerEntryAsync(Guid id, CancellationToken cancellationToken = default);

    // Stock counts
    Task<PagedResult<StockCountDto>> GetStockCountsAsync(
        PageRequest request,
        Guid? warehouseId = null,
        StockCountStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
    Task<StockCountDto?> GetStockCountAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockCountLineDto>> GetStockCountLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockCountDto?> CreateStockCountAsync(CreateStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto?> StartStockCountAsync(Guid id, StartStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto?> RecordStockCountAsync(Guid id, Guid lineId, RecordStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto?> CompleteStockCountAsync(Guid id, CompleteStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto?> ApproveStockCountAsync(Guid id, ApproveStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto?> PostStockCountAsync(Guid id, PostStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto?> CancelStockCountAsync(Guid id, CancelStockCountRequest request, CancellationToken cancellationToken = default);
}
