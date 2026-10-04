using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Returns;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesReturnQueryService
{
    Task<SalesReturnDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<SalesReturnDto>> GetPageAsync(
        PageRequest request,
        Guid? salesInvoiceId = null,
        Guid? customerId = null,
        SalesReturnStatus? status = null,
        CancellationToken cancellationToken = default);
}
