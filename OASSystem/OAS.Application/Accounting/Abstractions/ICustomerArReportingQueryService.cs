using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Abstractions;

/// <summary>Read-only customer AR reports; GL is authoritative for statements/reconciliation.</summary>
public interface ICustomerArReportingQueryService
{
    Task<CustomerStatementReportDto> StatementAsync(Guid customerId, DateOnly from, DateOnly to,
        int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<CustomerBalancesReportDto> BalancesAsync(DateOnly asOf, string? search = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<CustomerAgingReportDto> AgingAsync(DateOnly asOf, Guid? customerId = null,
        int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
    Task<CustomerArReconciliationDto> ReconcileAsync(DateOnly asOf,
        Guid? controlAccountId = null, CancellationToken ct = default);
    Task<CustomerArDocumentPageDto> DocumentsAsync(Guid customerId, string kind,
        DateOnly asOf, int page = 1, int size = 50, CancellationToken ct = default);
    Task<CustomerArWorkspaceDto> WorkspaceAsync(Guid customerId, DateOnly from, DateOnly to,
        int pageNumber = 1, int pageSize = 50, CancellationToken ct = default);
}
