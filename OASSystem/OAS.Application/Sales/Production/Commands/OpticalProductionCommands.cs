using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Production;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using DomainProductionStatus = OAS.Domain.Sales.Production.OpticalProductionStatus;

namespace OAS.Application.Sales.Production.Commands;

public sealed record CreateOpticalProductionJobCommand(CreateOpticalProductionJobRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit];
}

public sealed record ReleaseOpticalProductionJobCommand(Guid Id, OpticalProductionActionRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit];
}

public sealed record StartOpticalProductionJobCommand(Guid Id, OpticalProductionActionRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit];
}

public sealed record IssueOpticalProductionMaterialsCommand(Guid Id, OpticalProductionActionRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post];
}

public sealed record SubmitOpticalProductionQcCommand(Guid Id, SubmitOpticalProductionQcRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit];
}

public sealed record CreateOpticalProductionRemakeCommand(Guid Id, CreateOpticalProductionRemakeRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit];
}

public sealed record CompleteOpticalProductionJobCommand(Guid Id, OpticalProductionActionRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit];
}

public sealed record CancelOpticalProductionJobCommand(Guid Id, OpticalProductionActionRequest Data)
    : ICommand<OpticalProductionJobDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Cancel];
}

public sealed class CreateOpticalProductionJobCommandHandler(
    IOpticalProductionJobRepository jobs,
    IReadRepository<SalesInvoiceLine, Guid> invoiceLines,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<Warehouse, Guid> warehouses,
    IReadRepository<ProductVariant, Guid> variants,
    ISequenceNumberGenerator sequences,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOpticalProductionJobCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(CreateOpticalProductionJobCommand request, CancellationToken ct)
    {
        var data = request.Data;
        if (data.Materials is null || data.Materials.Count == 0)
            throw new ConflictException("production_materials_required", "يجب إضافة مادة إنتاج واحدة على الأقل.");

        var line = await invoiceLines.GetByIdAsync(data.SalesInvoiceLineId, ct)
            ?? throw new NotFoundException(nameof(SalesInvoiceLine), data.SalesInvoiceLineId);
        if (!line.RequiresProduction)
            throw new ConflictException("production_not_required", "سطر الفاتورة لا يتطلب إنتاجًا بصريًا.");

        var invoice = await invoices.GetByIdAsync(line.SalesInvoiceId, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), line.SalesInvoiceId);
        if (invoice.Status is not (SalesInvoiceStatus.Confirmed or SalesInvoiceStatus.Posted))
            throw new ConflictException("production_invoice_state", "يجب تأكيد فاتورة المبيعات قبل إنشاء أمر الإنتاج.");

        if (await jobs.CountAsync(
                new Specification<OpticalProductionJob>().Where(x => x.SalesInvoiceLineId == line.Id && x.IsActive), ct) > 0)
            throw new ConflictException("production_job_exists", "يوجد أمر إنتاج نشط لهذا السطر مسبقًا.");

        var warehouse = await warehouses.GetByIdAsync(data.WarehouseId, ct)
            ?? throw new NotFoundException(nameof(Warehouse), data.WarehouseId);
        if (!warehouse.IsActive)
            throw new ConflictException("production_warehouse_inactive", "المخزن غير فعال.");

        var sequence = await sequences.NextAsync("OpticalProductionJobCodeSequence", ct);
        var job = OpticalProductionJob.Create(
            Guid.NewGuid(),
            $"OPJ-{data.JobDate.Year:D4}-{sequence:D6}",
            invoice.Id,
            line.Id,
            invoice.CustomerId,
            data.WarehouseId,
            data.JobDate,
            data.TargetDate,
            data.Notes);

        var seen = new HashSet<Guid>();
        foreach (var material in data.Materials)
        {
            if (!seen.Add(material.ProductVariantId))
                throw new ConflictException("production_material_duplicate", "لا يمكن تكرار نفس مادة الإنتاج.");
            var variant = await variants.GetByIdAsync(material.ProductVariantId, ct)
                ?? throw new NotFoundException(nameof(ProductVariant), material.ProductVariantId);
            if (!variant.IsActive)
                throw new ConflictException("production_variant_inactive", "أحد أصناف مواد الإنتاج غير فعال.");

            job.AddMaterial(OpticalProductionMaterial.Create(
                Guid.NewGuid(), job.Id, variant.Id, material.Quantity, material.Notes));
        }

        await jobs.AddAsync(job, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetAsync(job.Id, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), job.Id);
    }
}

internal static class OpticalProductionCommandSupport
{
    public static void EnsureVersion(OpticalProductionJob job, string value)
    {
        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(value);
        }
        catch
        {
            throw new ConflictException("production_rowversion_invalid", "قيمة RowVersion غير صالحة.");
        }

        if (!job.RowVersion.SequenceEqual(expected))
            throw new ConflictException("production_concurrency", "تم تعديل أمر الإنتاج بواسطة مستخدم آخر.");
    }

    public static async Task<OpticalProductionJobDto> SaveAndGet(
        OpticalProductionJob job,
        IUnitOfWork unitOfWork,
        IOpticalProductionQueryService queries,
        CancellationToken ct)
    {
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetAsync(job.Id, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), job.Id);
    }
}

public sealed class ReleaseOpticalProductionJobCommandHandler(
    IOpticalProductionJobRepository jobs,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ReleaseOpticalProductionJobCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(ReleaseOpticalProductionJobCommand request, CancellationToken ct)
    {
        var job = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(job, request.Data.RowVersion);
        job.Release(timeProvider.GetUtcNow());
        return await OpticalProductionCommandSupport.SaveAndGet(job, unitOfWork, queries, ct);
    }
}

public sealed class StartOpticalProductionJobCommandHandler(
    IOpticalProductionJobRepository jobs,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<StartOpticalProductionJobCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(StartOpticalProductionJobCommand request, CancellationToken ct)
    {
        var job = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(job, request.Data.RowVersion);
        job.Start(timeProvider.GetUtcNow());
        return await OpticalProductionCommandSupport.SaveAndGet(job, unitOfWork, queries, ct);
    }
}

public sealed class IssueOpticalProductionMaterialsCommandHandler(
    IOpticalProductionJobRepository jobs,
    IOpticalProductionInventoryService inventory,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ICurrentUser currentUser)
    : IRequestHandler<IssueOpticalProductionMaterialsCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(IssueOpticalProductionMaterialsCommand request, CancellationToken ct)
    {
        var job = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(job, request.Data.RowVersion);
        await inventory.IssueMaterialsAsync(job, currentUser.UserId ?? "system", timeProvider.GetUtcNow(), ct);
        return await OpticalProductionCommandSupport.SaveAndGet(job, unitOfWork, queries, ct);
    }
}

public sealed class SubmitOpticalProductionQcCommandHandler(
    IOpticalProductionJobRepository jobs,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<SubmitOpticalProductionQcCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(SubmitOpticalProductionQcCommand request, CancellationToken ct)
    {
        var job = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(job, request.Data.RowVersion);
        job.SubmitQualityControl(
            timeProvider.GetUtcNow(),
            request.Data.Passed,
            request.Data.Notes,
            request.Data.IsBreakage);
        return await OpticalProductionCommandSupport.SaveAndGet(job, unitOfWork, queries, ct);
    }
}

public sealed class CreateOpticalProductionRemakeCommandHandler(
    IOpticalProductionJobRepository jobs,
    ISequenceNumberGenerator sequences,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CreateOpticalProductionRemakeCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(CreateOpticalProductionRemakeCommand request, CancellationToken ct)
    {
        var failed = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(failed, request.Data.RowVersion);

        if (failed.Status != DomainProductionStatus.QualityControlFailed)
            throw new ConflictException("production_remake_requires_failed_qc", "يمكن إنشاء إعادة تصنيع فقط لأمر إنتاج فشل في فحص الجودة.");

        if (await jobs.CountAsync(
                new Specification<OpticalProductionJob>().Where(x => x.SalesInvoiceLineId == failed.SalesInvoiceLineId && x.IsActive), ct) > 0)
            throw new ConflictException("production_remake_active_exists", "يوجد أمر إنتاج نشط لهذا السطر بالفعل.");

        var now = timeProvider.GetUtcNow();
        var jobDate = DateOnly.FromDateTime(now.UtcDateTime);
        var targetDate = request.Data.TargetDate;
        if (targetDate.HasValue && targetDate.Value < jobDate)
            throw new ConflictException("production_remake_target_invalid", "تاريخ الاستهداف لإعادة التصنيع لا يمكن أن يسبق تاريخ الإنشاء.");

        var sequence = await sequences.NextAsync("OpticalProductionJobCodeSequence", ct);
        var remake = OpticalProductionJob.Create(
            Guid.NewGuid(),
            $"OPJ-{jobDate.Year:D4}-{sequence:D6}",
            failed.SalesInvoiceId,
            failed.SalesInvoiceLineId,
            failed.CustomerId,
            failed.WarehouseId,
            jobDate,
            targetDate,
            request.Data.Notes ?? $"إعادة تصنيع من {failed.JobCode}",
            failed.Id,
            failed.RemakeNumber + 1);

        foreach (var material in failed.Materials)
        {
            remake.AddMaterial(OpticalProductionMaterial.Create(
                Guid.NewGuid(),
                remake.Id,
                material.ProductVariantId,
                material.Quantity,
                material.Notes));
        }

        await jobs.AddAsync(remake, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetAsync(remake.Id, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), remake.Id);
    }
}

public sealed class CompleteOpticalProductionJobCommandHandler(
    IOpticalProductionJobRepository jobs,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteOpticalProductionJobCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(CompleteOpticalProductionJobCommand request, CancellationToken ct)
    {
        var job = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(job, request.Data.RowVersion);
        job.Complete(timeProvider.GetUtcNow());
        return await OpticalProductionCommandSupport.SaveAndGet(job, unitOfWork, queries, ct);
    }
}

public sealed class CancelOpticalProductionJobCommandHandler(
    IOpticalProductionJobRepository jobs,
    IOpticalProductionQueryService queries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelOpticalProductionJobCommand, OpticalProductionJobDto>
{
    public async Task<OpticalProductionJobDto> Handle(CancelOpticalProductionJobCommand request, CancellationToken ct)
    {
        var job = await jobs.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(OpticalProductionJob), request.Id);
        OpticalProductionCommandSupport.EnsureVersion(job, request.Data.RowVersion);
        job.Cancel();
        return await OpticalProductionCommandSupport.SaveAndGet(job, unitOfWork, queries, ct);
    }
}
