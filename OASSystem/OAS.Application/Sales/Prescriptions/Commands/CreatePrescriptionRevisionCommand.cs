using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed record CreatePrescriptionRevisionCommand(Guid PrescriptionId, CreatePrescriptionRevisionRequest Request)
    : ICommand<PrescriptionDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.Edit];
}
