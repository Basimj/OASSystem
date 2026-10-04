using OAS.Contracts.Accounting.CustomerAdvances;

namespace OAS.Application.Accounting.Abstractions;

public interface ICustomerAdvanceService
{
    Task<CustomerAdvanceDto> CreateAsync(
        CreateCustomerAdvanceRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAdvanceApplicationDto> ApplyAsync(
        Guid customerAdvanceId,
        ApplyCustomerAdvanceRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAdvanceDto> GetAsync(
        Guid customerAdvanceId,
        CancellationToken cancellationToken = default);
}
