using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Suppliers;
namespace OAS.Application.Accounting.Suppliers.Commands.CreateSupplier;
public sealed record CreateSupplierCommand(CreateSupplierRequest Request):ICommand<Guid>,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; }=[AccountingPermissions.Suppliers.Create];
}
