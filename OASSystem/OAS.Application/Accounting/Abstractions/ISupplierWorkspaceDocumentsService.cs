using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Abstractions;

public interface ISupplierWorkspaceDocumentsService
{
    Task<SupplierWorkspaceDocumentsDto> GetAsync(
        SupplierWorkspaceDocumentsFilter filter,
        CancellationToken cancellationToken = default);
}
