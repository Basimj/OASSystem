using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed class CreatePrescriptionCommandHandler(
    IPrescriptionAggregateRepository repository,
    IReadRepository<Customer, Guid> customers,
    ISequenceNumberGenerator sequences) : IRequestHandler<CreatePrescriptionCommand, Prescription>
{
    public async Task<Prescription> Handle(CreatePrescriptionCommand request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.Data.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.Data.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        var code = request.Data.PrescriptionCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            code = PrescriptionCodeFormatter.Format(await sequences.NextAsync("PrescriptionCodeSequence", ct));

        if (await repository.CountAsync(
                new Specification<Prescription>().Where(x => x.PrescriptionCode == code), ct) > 0)
            throw new ConflictException(SalesErrorCodes.DuplicatePrescriptionCode, "كود الوصفة مستخدم مسبقًا.");

        var entity = Prescription.Create(
            Guid.NewGuid(), code, request.Data.CustomerId, request.Data.PrescriptionDate,
            request.Data.PrescribedBy, request.Data.ClinicName, request.Data.Notes);

        await repository.AddAsync(entity, ct);
        return entity;
    }
}
