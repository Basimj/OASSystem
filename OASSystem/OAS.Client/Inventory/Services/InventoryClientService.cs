using System.Globalization;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;
using OAS.Contracts.Inventory.Products;
using OAS.Contracts.Inventory.Stock;
using OAS.Contracts.Inventory.Transactions;
using OAS.Contracts.Inventory.Warehouses;

namespace OAS.Client.Inventory.Services;

public sealed class InventoryClientService(OasApiClient apiClient) : IInventoryClientService
{
    private const string BaseEndpoint = "api/inventory";

    public Task<InventoryCodeSuggestionDto?> GetNextCodeAsync(string kind, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<InventoryCodeSuggestionDto>($"{BaseEndpoint}/codes/next/{Uri.EscapeDataString(kind)}", cancellationToken);

    // Product categories
    public async Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<ProductCategoryDto>($"{BaseEndpoint}/product-categories", request, cancellationToken);

    public Task<ProductCategoryDto?> GetProductCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<ProductCategoryDto>($"{BaseEndpoint}/product-categories/{id:D}", cancellationToken);

    public Task<ProductCategoryDto?> CreateProductCategoryAsync(CreateProductCategoryRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateProductCategoryRequest, ProductCategoryDto>($"{BaseEndpoint}/product-categories", request, cancellationToken);

    public Task<ProductCategoryDto?> UpdateProductCategoryAsync(Guid id, UpdateProductCategoryRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateProductCategoryRequest, ProductCategoryDto>($"{BaseEndpoint}/product-categories/{id:D}", request, cancellationToken);

    // Brands
    public async Task<PagedResult<BrandDto>> GetBrandsAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<BrandDto>($"{BaseEndpoint}/brands", request, cancellationToken);

    public Task<BrandDto?> GetBrandAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<BrandDto>($"{BaseEndpoint}/brands/{id:D}", cancellationToken);

    public Task<BrandDto?> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateBrandRequest, BrandDto>($"{BaseEndpoint}/brands", request, cancellationToken);

    public Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateBrandRequest, BrandDto>($"{BaseEndpoint}/brands/{id:D}", request, cancellationToken);

    // Product types
    public async Task<PagedResult<ProductTypeDto>> GetProductTypesAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<ProductTypeDto>($"{BaseEndpoint}/product-types", request, cancellationToken);

    public Task<ProductTypeDto?> GetProductTypeAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<ProductTypeDto>($"{BaseEndpoint}/product-types/{id:D}", cancellationToken);

    public Task<ProductTypeDto?> CreateProductTypeAsync(CreateProductTypeRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateProductTypeRequest, ProductTypeDto>($"{BaseEndpoint}/product-types", request, cancellationToken);

    public Task<ProductTypeDto?> UpdateProductTypeAsync(Guid id, UpdateProductTypeRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateProductTypeRequest, ProductTypeDto>($"{BaseEndpoint}/product-types/{id:D}", request, cancellationToken);

    // Units
    public async Task<PagedResult<UnitDto>> GetUnitsAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<UnitDto>($"{BaseEndpoint}/units", request, cancellationToken);

    public Task<UnitDto?> GetUnitAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<UnitDto>($"{BaseEndpoint}/units/{id:D}", cancellationToken);

    public Task<UnitDto?> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateUnitRequest, UnitDto>($"{BaseEndpoint}/units", request, cancellationToken);

    public Task<UnitDto?> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateUnitRequest, UnitDto>($"{BaseEndpoint}/units/{id:D}", request, cancellationToken);

    // Products
    public async Task<PagedResult<ProductDto>> GetProductsAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<ProductDto>($"{BaseEndpoint}/products", request, cancellationToken);

    public Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<ProductDto>($"{BaseEndpoint}/products/{id:D}", cancellationToken);

    public Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateProductRequest, ProductDto>($"{BaseEndpoint}/products", request, cancellationToken);

    public Task<CreateStockProductResult?> CreateStockProductAsync(CreateStockProductRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateStockProductRequest, CreateStockProductResult>($"{BaseEndpoint}/products/stock-product", request, cancellationToken);

    public Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateProductRequest, ProductDto>($"{BaseEndpoint}/products/{id:D}", request, cancellationToken);

    // Product variants
    public async Task<PagedResult<ProductVariantDto>> GetProductVariantsAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<ProductVariantDto>($"{BaseEndpoint}/product-variants", request, cancellationToken);

    public Task<ProductVariantDto?> GetProductVariantAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<ProductVariantDto>($"{BaseEndpoint}/product-variants/{id:D}", cancellationToken);

    public Task<ProductVariantDto?> CreateProductVariantAsync(CreateProductVariantRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateProductVariantRequest, ProductVariantDto>($"{BaseEndpoint}/product-variants", request, cancellationToken);

    public Task<ProductVariantDto?> UpdateProductVariantAsync(Guid id, UpdateProductVariantRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateProductVariantRequest, ProductVariantDto>($"{BaseEndpoint}/product-variants/{id:D}", request, cancellationToken);

    // Frame details
    public async Task<PagedResult<FrameDetailsDto>> GetFrameDetailsAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<FrameDetailsDto>($"{BaseEndpoint}/frame-details", request, cancellationToken);

    public Task<FrameDetailsDto?> GetFrameDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<FrameDetailsDto>($"{BaseEndpoint}/frame-details/{id:D}", cancellationToken);

    public Task<FrameDetailsDto?> CreateFrameDetailsAsync(CreateFrameDetailsRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateFrameDetailsRequest, FrameDetailsDto>($"{BaseEndpoint}/frame-details", request, cancellationToken);

    public Task<FrameDetailsDto?> UpdateFrameDetailsAsync(Guid id, UpdateFrameDetailsRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateFrameDetailsRequest, FrameDetailsDto>($"{BaseEndpoint}/frame-details/{id:D}", request, cancellationToken);

    // Lens details
    public async Task<PagedResult<LensDetailsDto>> GetLensDetailsAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<LensDetailsDto>($"{BaseEndpoint}/lens-details", request, cancellationToken);

    public Task<LensDetailsDto?> GetLensDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<LensDetailsDto>($"{BaseEndpoint}/lens-details/{id:D}", cancellationToken);

    public Task<LensDetailsDto?> CreateLensDetailsAsync(CreateLensDetailsRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateLensDetailsRequest, LensDetailsDto>($"{BaseEndpoint}/lens-details", request, cancellationToken);

    public Task<LensDetailsDto?> UpdateLensDetailsAsync(Guid id, UpdateLensDetailsRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateLensDetailsRequest, LensDetailsDto>($"{BaseEndpoint}/lens-details/{id:D}", request, cancellationToken);

    // Warehouses
    public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await GetPageAsync<WarehouseDto>($"{BaseEndpoint}/warehouses", request, cancellationToken);

    public Task<WarehouseDto?> GetWarehouseAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<WarehouseDto>($"{BaseEndpoint}/warehouses/{id:D}", cancellationToken);

    public Task<WarehouseDto?> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateWarehouseRequest, WarehouseDto>($"{BaseEndpoint}/warehouses", request, cancellationToken);

    public Task<WarehouseDto?> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateWarehouseRequest, WarehouseDto>($"{BaseEndpoint}/warehouses/{id:D}", request, cancellationToken);

    // Inventory balances
    public async Task<PagedResult<InventoryBalanceDto>> GetInventoryBalancesAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = PageQuery(request);
        Add(query, "warehouseId", warehouseId);
        Add(query, "productVariantId", productVariantId);
        return await apiClient.GetAsync<PagedResult<InventoryBalanceDto>>(
            $"{BaseEndpoint}/balances?{string.Join("&", query)}", cancellationToken) ?? new();
    }

    public Task<InventoryBalanceDto?> GetInventoryBalanceAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<InventoryBalanceDto>($"{BaseEndpoint}/balances/{id:D}", cancellationToken);

    public Task<InventoryBalanceDto?> GetInventoryBalanceAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<InventoryBalanceDto>(
            $"{BaseEndpoint}/balances/by-warehouse-variant?warehouseId={warehouseId:D}&productVariantId={productVariantId:D}",
            cancellationToken);

    // Inventory transactions
    public async Task<PagedResult<InventoryTransactionDto>> GetInventoryTransactionsAsync(
        PageRequest request,
        InventoryTransactionType? transactionType = null,
        InventoryTransactionStatus? status = null,
        Guid? warehouseId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = PageQuery(request);
        AddEnum(query, "transactionType", transactionType);
        AddEnum(query, "status", status);
        Add(query, "warehouseId", warehouseId);
        Add(query, "fromDate", fromDate);
        Add(query, "toDate", toDate);

        return await apiClient.GetAsync<PagedResult<InventoryTransactionDto>>(
            $"{BaseEndpoint}/transactions?{string.Join("&", query)}", cancellationToken) ?? new();
    }

    public Task<InventoryTransactionDto?> GetInventoryTransactionAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<InventoryTransactionDto>($"{BaseEndpoint}/transactions/{id:D}", cancellationToken);

    public async Task<IReadOnlyList<InventoryTransactionLineDto>> GetInventoryTransactionLinesAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<InventoryTransactionLineDto[]>($"{BaseEndpoint}/transactions/{id:D}/lines", cancellationToken) ?? [];

    public Task<InventoryTransactionDto?> CreateInventoryTransactionAsync(
        CreateInventoryTransactionRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateInventoryTransactionRequest, InventoryTransactionDto>($"{BaseEndpoint}/transactions", request, cancellationToken);

    public Task<InventoryTransactionDto?> PostInventoryTransactionAsync(
        Guid id,
        PostInventoryTransactionRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<PostInventoryTransactionRequest, InventoryTransactionDto>($"{BaseEndpoint}/transactions/{id:D}/post", request, cancellationToken);

    // Inventory ledger
    public async Task<PagedResult<InventoryLedgerDto>> GetInventoryLedgerAsync(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        InventoryMovementType? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = PageQuery(request);
        Add(query, "warehouseId", warehouseId);
        Add(query, "productVariantId", productVariantId);
        Add(query, "transactionId", transactionId);
        AddEnum(query, "movementType", movementType);
        Add(query, "fromDate", fromDate);
        Add(query, "toDate", toDate);

        return await apiClient.GetAsync<PagedResult<InventoryLedgerDto>>(
            $"{BaseEndpoint}/ledger?{string.Join("&", query)}", cancellationToken) ?? new();
    }

    public Task<InventoryLedgerDto?> GetInventoryLedgerEntryAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<InventoryLedgerDto>($"{BaseEndpoint}/ledger/{id:D}", cancellationToken);

    // Stock counts
    public async Task<PagedResult<StockCountDto>> GetStockCountsAsync(
        PageRequest request,
        Guid? warehouseId = null,
        StockCountStatus? status = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = PageQuery(request);
        Add(query, "warehouseId", warehouseId);
        AddEnum(query, "status", status);
        Add(query, "fromDate", fromDate);
        Add(query, "toDate", toDate);

        return await apiClient.GetAsync<PagedResult<StockCountDto>>(
            $"{BaseEndpoint}/stock-counts?{string.Join("&", query)}", cancellationToken) ?? new();
    }

    public Task<StockCountDto?> GetStockCountAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}", cancellationToken);

    public async Task<IReadOnlyList<StockCountLineDto>> GetStockCountLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<StockCountLineDto[]>($"{BaseEndpoint}/stock-counts/{id:D}/lines", cancellationToken) ?? [];

    public Task<StockCountDto?> CreateStockCountAsync(CreateStockCountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts", request, cancellationToken);

    public Task<StockCountDto?> StartStockCountAsync(
        Guid id,
        StartStockCountRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<StartStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}/start", request, cancellationToken);

    public Task<StockCountDto?> RecordStockCountAsync(
        Guid id,
        Guid lineId,
        RecordStockCountRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<RecordStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}/lines/{lineId:D}/count", request, cancellationToken);

    public Task<StockCountDto?> CompleteStockCountAsync(
        Guid id,
        CompleteStockCountRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CompleteStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}/complete", request, cancellationToken);

    public Task<StockCountDto?> ApproveStockCountAsync(
        Guid id,
        ApproveStockCountRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<ApproveStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}/approve", request, cancellationToken);

    public Task<StockCountDto?> PostStockCountAsync(
        Guid id,
        PostStockCountRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<PostStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}/post", request, cancellationToken);

    public Task<StockCountDto?> CancelStockCountAsync(
        Guid id,
        CancelStockCountRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CancelStockCountRequest, StockCountDto>($"{BaseEndpoint}/stock-counts/{id:D}/cancel", request, cancellationToken);

    private async Task<PagedResult<T>> GetPageAsync<T>(string endpoint, PageRequest request, CancellationToken cancellationToken)
    {
        var query = PageQuery(request);
        return await apiClient.GetAsync<PagedResult<T>>($"{endpoint}?{string.Join("&", query)}", cancellationToken) ?? new();
    }

    private static List<string> PageQuery(PageRequest request)
    {
        var normalized = request.Normalize();
        var query = new List<string>
        {
            $"pageNumber={normalized.PageNumber}",
            $"pageSize={normalized.PageSize}",
            $"sortDirection={normalized.SortDirection}"
        };

        if (!string.IsNullOrWhiteSpace(normalized.Search))
            query.Add($"search={Uri.EscapeDataString(normalized.Search)}");
        if (!string.IsNullOrWhiteSpace(normalized.SortBy))
            query.Add($"sortBy={Uri.EscapeDataString(normalized.SortBy)}");

        return query;
    }

    private static void Add(List<string> query, string name, Guid? value)
    {
        if (value.HasValue)
            query.Add($"{name}={value.Value:D}");
    }

    private static void Add(List<string> query, string name, DateTimeOffset? value)
    {
        if (value.HasValue)
            query.Add($"{name}={Uri.EscapeDataString(value.Value.ToString("O", CultureInfo.InvariantCulture))}");
    }

    private static void Add(List<string> query, string name, DateOnly? value)
    {
        if (value.HasValue)
            query.Add($"{name}={value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
    }

    private static void AddEnum<TEnum>(List<string> query, string name, TEnum? value)
        where TEnum : struct, Enum
    {
        if (value.HasValue)
            query.Add($"{name}={Convert.ToInt32(value.Value, CultureInfo.InvariantCulture)}");
    }
}
