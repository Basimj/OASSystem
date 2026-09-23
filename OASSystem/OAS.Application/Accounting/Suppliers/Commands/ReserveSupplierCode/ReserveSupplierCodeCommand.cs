using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Suppliers;
namespace OAS.Application.Accounting.Suppliers.Commands.ReserveSupplierCode;
public sealed record ReserveSupplierCodeCommand:ICommand<SupplierCodeReservationDto>,IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; }=[AccountingPermissions.Suppliers.Create];
}
