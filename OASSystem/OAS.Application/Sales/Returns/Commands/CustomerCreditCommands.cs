using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Returns;

namespace OAS.Application.Sales.Returns.Commands;

public sealed record ApplyCustomerReturnCreditCommand(Guid CustomerId, ApplyCustomerReturnCreditRequest Data)
    : ICommand<CustomerCreditSettlementDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Credit.Use];
}
public sealed record RefundCustomerReturnCreditCommand(Guid CustomerId, RefundCustomerReturnCreditRequest Data)
    : ICommand<CustomerCreditSettlementDto>, IAuthorizedRequest
{
    // Refund creates and posts a payment voucher, therefore require BOTH rights.
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Credit.Use, "accounting.payment_vouchers.create", "accounting.payment_vouchers.post"];
}
public sealed record ReverseCustomerReturnCreditCommand(Guid CustomerId, Guid Id, ReverseCustomerReturnCreditRequest Data)
    : ICommand<CustomerCreditSettlementDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Credit.Use];
}
