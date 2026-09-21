using System.Globalization;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Enums.Inventory;

namespace OAS.Client.Inventory.Services;

public sealed class InventorySpreadsheetClient(OasApiClient api)
{
    public Task<byte[]> ExportAsync(
        string section,
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        Guid? transactionId = null,
        InventoryTransactionType? transactionType = null,
        InventoryTransactionStatus? transactionStatus = null,
        InventoryMovementType? movementType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        StockCountStatus? stockCountStatus = null,
        CancellationToken cancellationToken = default)
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
        Add(query, "warehouseId", warehouseId);
        Add(query, "productVariantId", productVariantId);
        Add(query, "transactionId", transactionId);
        AddEnum(query, "transactionType", transactionType);
        AddEnum(query, "transactionStatus", transactionStatus);
        AddEnum(query, "movementType", movementType);
        Add(query, "fromDate", fromDate);
        Add(query, "toDate", toDate);
        AddEnum(query, "stockCountStatus", stockCountStatus);

        return api.GetFileAsync(
            $"api/inventory/spreadsheets/{Uri.EscapeDataString(section)}/export?{string.Join("&", query)}",
            cancellationToken);
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

    private static void AddEnum<TEnum>(List<string> query, string name, TEnum? value)
        where TEnum : struct, Enum
    {
        if (value.HasValue)
            query.Add($"{name}={Convert.ToInt32(value.Value, CultureInfo.InvariantCulture)}");
    }
}
