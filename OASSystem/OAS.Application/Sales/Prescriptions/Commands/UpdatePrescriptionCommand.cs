using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Commands;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed record UpdatePrescriptionCommand(Guid Id, UpdatePrescriptionRequest Data)
    : UpdateEntityCommand<Prescription, Guid, UpdatePrescriptionRequest>(Id, Data), IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.Edit];
}
