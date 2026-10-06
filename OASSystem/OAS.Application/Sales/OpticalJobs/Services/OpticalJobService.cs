using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.OpticalJobs.Services;

public sealed class OpticalJobService(
    IRepository<OpticalJob, Guid> jobs,
    IRepository<OpticalJobLine, Guid> jobLines,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> opticalSnapshots,
    ICustomerOrderAggregateRepository orders,
    IReadRepository<SalesInvoice, Guid> invoices,
    ISequenceNumberGenerator sequences,
    TimeProvider timeProvider) : IOpticalJobService
{
    public async Task<Guid> CreateAsync(CreateOpticalJobRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetAggregateAsync(request.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.CustomerOrderId);
        if (order.Status != CustomerOrderStatus.ReadyForProduction)
            throw new ConflictException("optical_job_order_not_ready", "لا يمكن إنشاء أمر معمل قبل اكتمال توفر وحجز المواد للطلب.");

        var duplicate = new Specification<OpticalJob>().Where(x => x.CustomerOrderId == order.Id && x.IsActive);
        if (await jobs.CountAsync(duplicate, cancellationToken) > 0)
            throw new ConflictException("optical_job_duplicate_order", "يوجد أمر معمل فعال لهذا الطلب بالفعل.");

        if (request.SalesInvoiceId.HasValue)
        {
            var invoice = await invoices.GetByIdAsync(request.SalesInvoiceId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(SalesInvoice), request.SalesInvoiceId.Value);
            if (invoice.CustomerOrderId != order.Id || invoice.CustomerId != order.CustomerId)
                throw new ConflictException("optical_job_invoice_mismatch", "الفاتورة المحددة لا ترتبط بطلب العميل نفسه.");
        }

        var now = timeProvider.GetUtcNow();
        var sequence = await sequences.NextAsync($"OpticalJob-{now.Year}", cancellationToken);
        var job = OpticalJob.Create(
            Guid.NewGuid(),
            $"OJ-{now.Year:0000}-{sequence:000000}",
            order.Id,
            request.SalesInvoiceId,
            order.CustomerId,
            request.RequiredDate ?? order.RequiredDate,
            request.Notes);

        var requestedIds = request.Lines.Count == 0
            ? order.Lines.Where(x => x.IsActive && x.RequiresProduction).Select(x => x.Id).ToHashSet()
            : request.Lines.Select(x => x.CustomerOrderLineId).ToHashSet();
        var sourceLines = order.Lines.Where(x => requestedIds.Contains(x.Id) && x.IsActive).OrderBy(x => x.LineNumber).ToArray();
        if (sourceLines.Length == 0)
            throw new ConflictException("optical_job_lines_required", "يجب أن يحتوي أمر المعمل على سطر تشغيلي واحد على الأقل.");
        if (sourceLines.Any(x => !x.RequiresProduction))
            throw new ConflictException("optical_job_line_not_production", "لا يمكن إضافة سطر لا يحتاج تجهيزًا إلى أمر المعمل.");
        if (sourceLines.Length != requestedIds.Count)
            throw new ConflictException("optical_job_order_line_missing", "أحد أسطر أمر المعمل لا ينتمي إلى طلب العميل.");

        var lineNumber = 1;
        foreach (var source in sourceLines)
        {
            var snapshot = (await opticalSnapshots.ListAsync(
                new Specification<CustomerOrderLineOpticalSnapshot>().Where(x => x.CustomerOrderLineId == source.Id && x.IsActive),
                cancellationToken)).SingleOrDefault();
            var eye = snapshot?.Eye ?? source.PrescriptionEye;
            var line = OpticalJobLine.Create(
                Guid.NewGuid(),
                job.Id,
                source.Id,
                source.ProductVariantId,
                lineNumber++,
                source.LineType,
                eye,
                source.DescriptionSnapshot,
                source.Quantity,
                source.Notes);
            job.AddLine(line);
        }

        // ReadyForProduction means all required materials have already been reserved for this order.
        job.MarkMaterialsAvailable();
        await jobs.AddAsync(job, cancellationToken);
        return job.Id;
    }

    public async Task AssignAsync(Guid id, AssignOpticalJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);
        SalesConcurrency.Ensure(request.RowVersion, job.RowVersion, "أمر المعمل");
        job.AssignTechnician(request.TechnicianId);
        jobs.Update(job);
    }

    public async Task StartAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);
        SalesConcurrency.Ensure(request.RowVersion, job.RowVersion, "أمر المعمل");
        if (job.Status == OpticalJobStatus.MaterialsAvailable)
            job.MarkMaterialsIssued();
        if (job.Status != OpticalJobStatus.MaterialsIssued)
            throw new ConflictException("optical_job_materials_not_available", "لا يمكن بدء العمل قبل توفر وإصدار المواد.");

        var order = await orders.GetAggregateAsync(job.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), job.CustomerOrderId);
        job.Start(timeProvider.GetUtcNow());
        if (order.Status == CustomerOrderStatus.ReadyForProduction)
            order.StartProduction();
        jobs.Update(job);
        orders.Update(order);
    }

    public async Task MarkReadyAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);
        SalesConcurrency.Ensure(request.RowVersion, job.RowVersion, "أمر المعمل");
        if (job.Status == OpticalJobStatus.InProduction)
            job.SendToQualityControl();
        if (job.Status == OpticalJobStatus.AwaitingQC)
            job.PassQualityControl();
        if (job.Status != OpticalJobStatus.QCPassed)
            throw new ConflictException("optical_job_not_ready_for_delivery", "أمر المعمل ليس في حالة تسمح بجعله جاهزًا للتسليم.");

        job.MarkReadyForDelivery(timeProvider.GetUtcNow());
        jobs.Update(job);

        var order = await orders.GetAggregateAsync(job.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), job.CustomerOrderId);
        if (order.Status == CustomerOrderStatus.InProduction)
            order.MarkReadyForDelivery();
        orders.Update(order);
    }

    public async Task DeliverAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);
        SalesConcurrency.Ensure(request.RowVersion, job.RowVersion, "أمر المعمل");
        if (job.Status == OpticalJobStatus.Delivered)
            return;
        if (job.Status != OpticalJobStatus.ReadyForDelivery)
            throw new ConflictException("optical_job_not_ready_for_delivery", "أمر المعمل ليس جاهزًا للتسليم.");
        job.MarkDelivered();
        jobs.Update(job);
    }

    public async Task<OpticalJobDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);
        var lines = await jobLines.ListAsync(
            new Specification<OpticalJobLine>().Where(x => x.OpticalJobId == job.Id), cancellationToken);
        return MapDetails(job, lines);
    }

    public async Task<IReadOnlyList<OpticalJobWorkQueueDto>> GetWorkQueueAsync(CancellationToken cancellationToken = default)
    {
        var active = await jobs.ListAsync(
            new Specification<OpticalJob>().Where(x => x.IsActive && x.Status != OpticalJobStatus.Cancelled && x.Status != OpticalJobStatus.Delivered),
            cancellationToken);
        return active
            .OrderBy(x => x.RequiredDate ?? DateOnly.MaxValue)
            .ThenBy(x => x.JobCode)
            .Select(MapWorkQueue)
            .ToArray();
    }

    private static OpticalJobWorkQueueDto MapWorkQueue(OpticalJob x) => new(
        x.Id, x.JobCode, x.CustomerOrderId, x.SalesInvoiceId, x.CustomerId, x.RequiredDate,
        (OAS.Contracts.Sales.Enums.OpticalJobStatus)(byte)x.Status, x.AssignedTechnicianId,
        x.StartedAtUtc, x.CompletedAtUtc, x.Notes, Convert.ToBase64String(x.RowVersion));

    private static OpticalJobDetailsDto MapDetails(OpticalJob x, IReadOnlyList<OpticalJobLine> lines) => new(
        x.Id, x.JobCode, x.CustomerOrderId, x.SalesInvoiceId, x.CustomerId, x.RequiredDate,
        (OAS.Contracts.Sales.Enums.OpticalJobStatus)(byte)x.Status, x.AssignedTechnicianId,
        x.StartedAtUtc, x.CompletedAtUtc, x.Notes, x.IsActive, Convert.ToBase64String(x.RowVersion),
        x.CreatedAtUtc, x.CreatedBy, x.LastModifiedAtUtc, x.LastModifiedBy,
        lines.OrderBy(l => l.LineNumber).Select(MapLine).ToArray());

    private static OpticalJobLineDto MapLine(OpticalJobLine x) => new(
        x.Id, x.OpticalJobId, x.CustomerOrderLineId, x.ProductVariantId, x.LineNumber,
        (OAS.Contracts.Sales.Enums.SalesLineType)(byte)x.LineType,
        x.Eye.HasValue ? (OAS.Contracts.Sales.Enums.EyeSide?)(byte)x.Eye.Value : null,
        x.DescriptionSnapshot, x.Quantity, x.Notes, Convert.ToBase64String(x.RowVersion));
}
