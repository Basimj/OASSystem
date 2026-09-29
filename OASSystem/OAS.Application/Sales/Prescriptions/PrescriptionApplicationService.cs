using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Services;
using OAS.Application.Sales.Prescriptions.Commands;
using OAS.Application.Sales.Prescriptions.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions;

public sealed class PrescriptionApplicationService(
    ISender sender,
    ICrudMapper<Prescription, Guid, PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest> mapper)
    : CrudApplicationService<Prescription, Guid, PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest>(sender, mapper)
{
    public override Task<PagedResult<PrescriptionDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new GetPrescriptionsQuery(request), cancellationToken);

    public override Task<PrescriptionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetPrescriptionByIdQuery(id), cancellationToken);

    public override async Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new CreatePrescriptionCommand(request), cancellationToken);
        return await sender.Send(new GetPrescriptionByIdQuery(entity.Id), cancellationToken);
    }

    public override async Task<PrescriptionDto> UpdateAsync(Guid id, UpdatePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new UpdatePrescriptionCommand(id, request), cancellationToken);
        return await sender.Send(new GetPrescriptionByIdQuery(entity.Id), cancellationToken);
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromException(new ConflictException("sales_prescription_delete_forbidden", "لا يتم حذف الوصفة حذفًا نهائيًا؛ استخدم تغيير الحالة."));

    public Task<SalesCodeReservationDto> ReserveCodeAsync(CancellationToken cancellationToken = default) =>
        sender.Send(new ReservePrescriptionCodeCommand(), cancellationToken);

    public Task<PrescriptionDto> CreateRevisionAsync(Guid prescriptionId, CreatePrescriptionRevisionRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new CreatePrescriptionRevisionCommand(prescriptionId, request), cancellationToken);

    public Task<PrescriptionDto> SetStatusAsync(Guid prescriptionId, SetPrescriptionStatusRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new SetPrescriptionStatusCommand(prescriptionId, request), cancellationToken);
}
