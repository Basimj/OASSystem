using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Common;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed record ReservePrescriptionCodeCommand : ICommand<SalesCodeReservationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.Create];
}
