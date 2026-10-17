using OAS.Domain.Sales.Commissions;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Abstractions;

public sealed record CommissionSourceLine(
    string SourceDocumentType,
    Guid SourceDocumentId,
    Guid SourceLineId,
    Guid SalesInvoiceId,
    Guid OriginalSalesInvoiceLineId,
    Guid? SalesReturnId,
    DateOnly SourceDate,
    DateOnly OriginalSalesDate,
    decimal BaseNetAmount,
    decimal BaseGrossAmount,
    decimal BaseFinalAmount,
    Guid? ProductVariantId,
    Guid? ProductCategoryId,
    SalesLineType SaleType,
    bool IsReversal)
{
    public decimal AmountFor(CommissionBasisKind basisKind) => basisKind switch
    {
        CommissionBasisKind.NetAfterDiscountExcludingTax => BaseNetAmount,
        CommissionBasisKind.GrossBeforeDiscount => BaseGrossAmount,
        CommissionBasisKind.FinalIncludingTax => BaseFinalAmount,
        _ => throw new ArgumentOutOfRangeException(nameof(basisKind), basisKind, null)
    };
}

public interface ICommissionSourceQueryService
{
    Task<IReadOnlyList<CommissionSourceLine>> GetSourcesAsync(Guid employeeId, DateOnly fromDate, DateOnly toDate,
        CancellationToken cancellationToken = default);
}
