namespace OAS.Application.Sales.Abstractions;

public sealed record CommissionSourceLine(
    string SourceDocumentType, Guid SourceDocumentId, Guid SourceLineId, Guid SalesInvoiceId,
    Guid OriginalSalesInvoiceLineId, Guid? SalesReturnId, DateOnly SourceDate, DateOnly OriginalSalesDate,
    decimal BaseNetAmount, bool IsReversal);

public interface ICommissionSourceQueryService
{
    Task<IReadOnlyList<CommissionSourceLine>> GetSourcesAsync(Guid employeeId, DateOnly fromDate, DateOnly toDate,
        CancellationToken cancellationToken = default);
}
