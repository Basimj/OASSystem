using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Customers;
namespace OAS.Application.Accounting.Customers.Commands.ReserveCustomerCode;
public sealed record ReserveCustomerCodeCommand:ICommand<CustomerCodeReservationDto>,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; }=[AccountingPermissions.Customers.Create];
}
