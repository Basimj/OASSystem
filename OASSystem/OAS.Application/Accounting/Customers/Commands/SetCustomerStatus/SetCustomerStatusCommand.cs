using OAS.Application.Abstractions.Messaging; using OAS.Application.Accounting.Authorization; using OAS.Contracts.Accounting.Customers;
namespace OAS.Application.Accounting.Customers.Commands.SetCustomerStatus;
public sealed record SetCustomerStatusCommand(Guid Id,SetCustomerStatusRequest Request):ICommand,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Customers.Disable];}
