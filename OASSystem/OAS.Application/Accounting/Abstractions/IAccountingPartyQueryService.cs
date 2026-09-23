using OAS.Contracts.Accounting.Customers;
using OAS.Contracts.Accounting.Suppliers;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.Abstractions;

public interface IAccountingPartyQueryService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(PageRequest request,string? filter=null,CancellationToken cancellationToken=default);
    Task<CustomerDto?> GetCustomerAsync(Guid id,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CustomerLookupDto>> LookupCustomersAsync(string? search,int take,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CustomerAccountParentDto>> GetCustomerParentsAsync(string? search,int take,CancellationToken cancellationToken=default);
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(PageRequest request,string? filter=null,CancellationToken cancellationToken=default);
    Task<SupplierDto?> GetSupplierAsync(Guid id,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<SupplierLookupDto>> LookupSuppliersAsync(string? search,int take,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<SupplierAccountParentDto>> GetSupplierParentsAsync(string? search,int take,CancellationToken cancellationToken=default);
}
