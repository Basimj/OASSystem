namespace OAS.Contracts.Accounting.Reports;

public sealed record SupplierDebtSnapshotDto(
    DateOnly AsOf,
    IReadOnlyList<SupplierBalanceRowDto> Suppliers,
    IReadOnlyList<SupplierAgingLineDto> Documents);
