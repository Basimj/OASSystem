using OAS.Application.Abstractions.Messaging;using OAS.Application.Accounting.Authorization;using OAS.Contracts.Accounting.Customers;
namespace OAS.Application.Accounting.Customers.Queries.GetCustomerById;public sealed record GetCustomerByIdQuery(Guid Id):IQuery<CustomerDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Customers.View];}
