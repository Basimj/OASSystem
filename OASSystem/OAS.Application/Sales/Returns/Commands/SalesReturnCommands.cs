using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Returns;

namespace OAS.Application.Sales.Returns.Commands;

public sealed record CreateSalesReturnCommand(CreateSalesReturnRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.Create];
}

public sealed record ConfirmSalesReturnCommand(Guid Id, SalesReturnActionRequest Request) : ICommand<SalesReturnDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.Confirm];
}

public sealed record PostSalesReturnCommand(Guid Id, SalesReturnActionRequest Request) : ICommand<SalesReturnPostingResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.Post];
}

public sealed record CancelSalesReturnCommand(Guid Id, CancelSalesReturnRequest Request) : ICommand<SalesReturnDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.Cancel];
}
