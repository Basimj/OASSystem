using MediatR;
using OAS.Application.Sales.Returns.Services;
using OAS.Contracts.Sales.Returns;
namespace OAS.Application.Sales.Returns.Queries;
public sealed class GetCustomerCreditBalanceQueryHandler(CustomerCreditLedgerService ledger)
    : IRequestHandler<GetCustomerCreditBalanceQuery,CustomerCreditBalanceDto>
{
    public Task<CustomerCreditBalanceDto> Handle(GetCustomerCreditBalanceQuery request,CancellationToken ct)
        => ledger.GetAsync(request.CustomerId,ct);
}
