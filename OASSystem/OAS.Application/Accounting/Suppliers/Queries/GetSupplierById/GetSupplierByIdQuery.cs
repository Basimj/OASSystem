using OAS.Application.Abstractions.Messaging;using OAS.Application.Accounting.Authorization;using OAS.Contracts.Accounting.Suppliers;
namespace OAS.Application.Accounting.Suppliers.Queries.GetSupplierById;public sealed record GetSupplierByIdQuery(Guid Id):IQuery<SupplierDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Suppliers.View];}
