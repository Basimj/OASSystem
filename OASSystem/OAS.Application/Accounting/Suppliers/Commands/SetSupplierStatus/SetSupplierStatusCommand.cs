using OAS.Application.Abstractions.Messaging;using OAS.Application.Accounting.Authorization;using OAS.Contracts.Accounting.Suppliers;
namespace OAS.Application.Accounting.Suppliers.Commands.SetSupplierStatus;
public sealed record SetSupplierStatusCommand(Guid Id,SetSupplierStatusRequest Request):ICommand,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Suppliers.Disable];}
