using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using System.Globalization;

namespace OAS.Application.Inventory.Balances.Workspace;

public interface IInventoryBalanceWorkspaceReader
{
    Task<InventoryBalanceWorkspaceDto> ReadAsync(PageRequest request, InventoryBalanceFilters filters, CancellationToken ct);
    Task<IReadOnlyList<InventoryBalanceWorkspaceRow>> ExportRowsAsync(PageRequest request, InventoryBalanceFilters filters, CancellationToken ct);
    Task<(string? Code, string? Name)> CurrencyAsync(CancellationToken ct);
}
public sealed record GetBalanceWorkspaceQuery(PageRequest Request, InventoryBalanceFilters Filters)
    : IQuery<InventoryBalanceWorkspaceDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [InventoryPermissions.Balances.View];
}
public sealed record ExportBalanceWorkspaceQuery(PageRequest Request, InventoryBalanceFilters Filters)
    : IQuery<byte[]>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [InventoryPermissions.Balances.View];
}
public sealed class GetBalanceWorkspaceHandler(IInventoryBalanceWorkspaceReader reader)
    : IRequestHandler<GetBalanceWorkspaceQuery, InventoryBalanceWorkspaceDto>
{
    public Task<InventoryBalanceWorkspaceDto> Handle(GetBalanceWorkspaceQuery request, CancellationToken ct) =>
        reader.ReadAsync(request.Request, request.Filters, ct);
}
public sealed class ExportBalanceWorkspaceHandler(IInventoryBalanceWorkspaceReader reader, ISpreadsheetWorkbook workbook)
    : IRequestHandler<ExportBalanceWorkspaceQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportBalanceWorkspaceQuery request, CancellationToken ct)
    {
        var rows = await reader.ExportRowsAsync(request.Request, request.Filters, ct);
        var currency = await reader.CurrencyAsync(ct);
        var label = currency.Code is null ? "غير محددة" : $"{currency.Name} ({currency.Code})";
        string[] names = ["المنتج", "رمز المنتج", "الصنف", "SKU", "الباركود", "المخزن", "حالة الصنف", "الوحدة", "الفعلي", "المحجوز", "المتاح", "قيد الطلب", "متوسط التكلفة", "القيمة", "العملة الأساسية", "آخر حركة UTC"];
        string F(decimal value) => value.ToString(CultureInfo.InvariantCulture);
        var columns = names.Select((n,i) => new SpreadsheetColumn(n, DataType: i >= 8 && i <= 13 ? "decimal" : "text")).ToArray();
        var data = rows.Select(r => {
            string[] values = [r.ProductName, r.ProductCode, r.VariantName ?? "", r.SKU, r.Barcode ?? "", r.WarehouseName, r.IsActive ? "نشط" : "غير نشط", r.UnitName ?? "غير محددة", F(r.OnHand), F(r.Reserved), F(r.Available), F(r.OnOrder), F(r.AverageCost), F(r.Value), label, r.LastMovement?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""];
            return (IReadOnlyDictionary<string,string>)names.Select((n,i) => (n, value: values[i])).ToDictionary(x => x.n, x => x.value);
        }).ToArray();
        var totals = rows.GroupBy(r => new { r.UnitId, r.UnitName }).Select(g => (IReadOnlyDictionary<string,string>)new Dictionary<string,string> { ["الوحدة"] = g.Key.UnitName ?? "غير محددة", ["الفعلي"] = F(g.Sum(r=>r.OnHand)), ["المحجوز"] = F(g.Sum(r=>r.Reserved)), ["المتاح"] = F(g.Sum(r=>r.Available)), ["قيد الطلب"] = F(g.Sum(r=>r.OnOrder)) }).ToArray();
        var summary = new Dictionary<string,string> { ["عدد الأصناف"] = rows.Select(r=>r.VariantId).Distinct().Count().ToString(CultureInfo.InvariantCulture), ["عدد الأرصدة"] = rows.Count.ToString(CultureInfo.InvariantCulture), ["القيمة"] = F(rows.Sum(r=>r.Value)), ["العملة الأساسية"] = label };
        return workbook.Write([
            new SpreadsheetTable(new SpreadsheetSheet("أرصدة المخزون", columns), data),
            new SpreadsheetTable(new SpreadsheetSheet("إجماليات الكميات", new[] { "الوحدة", "الفعلي", "المحجوز", "المتاح", "قيد الطلب" }.Select((n,i)=>new SpreadsheetColumn(n, DataType: i==0 ? "text" : "decimal")).ToArray()), totals),
            new SpreadsheetTable(new SpreadsheetSheet("الملخص", new[] { "عدد الأصناف", "عدد الأرصدة", "القيمة", "العملة الأساسية" }.Select((n,i)=>new SpreadsheetColumn(n, DataType: i==3 ? "text" : "decimal")).ToArray()), [summary])
        ]);
    }
}
