using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CustomerAdvances;

namespace OAS.Application.Accounting.CustomerAdvances.Commands;

public sealed record CreateCustomerAdvanceCommand(CreateCustomerAdvanceRequest Request)
    : ICommand<CustomerAdvanceDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CustomerAdvances.Create];
}

public sealed record ApplyCustomerAdvanceCommand(Guid Id, ApplyCustomerAdvanceRequest Request)
    : ICommand<CustomerAdvanceApplicationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CustomerAdvances.Apply];
}

public sealed class CreateCustomerAdvanceCommandHandler(ICustomerAdvanceService service)
    : IRequestHandler<CreateCustomerAdvanceCommand, CustomerAdvanceDto>
{
    public Task<CustomerAdvanceDto> Handle(CreateCustomerAdvanceCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Request, cancellationToken);
}

public sealed class ApplyCustomerAdvanceCommandHandler(ICustomerAdvanceService service)
    : IRequestHandler<ApplyCustomerAdvanceCommand, CustomerAdvanceApplicationDto>
{
    public Task<CustomerAdvanceApplicationDto> Handle(ApplyCustomerAdvanceCommand request, CancellationToken cancellationToken) =>
        service.ApplyAsync(request.Id, request.Request, cancellationToken);
}
