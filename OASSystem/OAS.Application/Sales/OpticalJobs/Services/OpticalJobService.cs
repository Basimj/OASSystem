using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.OpticalJobs.Services;

public sealed class OpticalJobService(
    IRepository<OpticalJob, Guid> jobs,
    IRepository<OpticalJobLine, Guid> jobLines,
    IRepository<OpticalJobStatusHistory, Guid> statusHistory,
    IRepository<OpticalQualityCheck, Guid> qualityChecks,
    IRepository<OpticalQualityCheckItem, Guid> qualityCheckItems,
    IRepository<OpticalJobBreakage, Guid> breakages,
    IRepository<OpticalJobRemake, Guid> remakes,
    IReadRepository<CustomerOrderLineOpticalSnapshot, Guid> opticalSnapshots,
    ICustomerOrderAggregateRepository orders,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<Employee, Guid> employees,
    ISequenceNumberGenerator sequences,
    IOpticalJobInventoryPort inventory,
    IOpticalJobAccountingPort accounting,
    IOpticalJobPurchasingPort purchasing,
    IOpticalJobSalesPort sales,
    IOpticalJobQueryService queryService,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IOpticalJobService
{
    private static readonly (string Code, string Name)[] DefaultQcChecklist =
    [
        ("POWER", "Power"),
        ("PD", "PD"),
        ("FITTING_HEIGHT", "Fitting Height"),
        ("FRAME", "Frame"),
        ("ALIGNMENT", "Alignment"),
        ("COSMETIC_CONDITION", "Cosmetic Condition")
    ];

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

        // Carry every explicitly requested line; when no lines are supplied, production lines are selected.
        // Grouped frames are marked as RequiresProduction by the sales policy and therefore travel with OD/OS
        // into the job as fitting context.
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
                MapLineType(source.LineType),
                eye,
                source.GroupId,
                source.DescriptionSnapshot,
                source.Quantity,
                source.RequiresProduction,
                source.Notes);
            job.AddLine(line);
        }

        await jobs.AddAsync(job, cancellationToken);
        await AddHistoryAsync(job.Id, null, OpticalJobStatus.Approved, "تم إنشاء أمر المعمل من طلب العميل.", cancellationToken);

        // ReadyForProduction means the sale-side stock requirement has already been satisfied/reserved.
        // Do not issue it again here: this is the explicit no-double-consumption boundary.
        var from = job.Status;
        job.MarkMaterialsAvailable();
        await AddHistoryAsync(job.Id, from, job.Status, "مواد الطلب الأساسية متاحة من مسار المبيعات.", cancellationToken);
        return job.Id;
    }

    public async Task AssignAsync(Guid id, AssignOpticalJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (request.TechnicianId.HasValue)
        {
            var technician = await employees.GetByIdAsync(request.TechnicianId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Employee), request.TechnicianId.Value);
            if (!technician.IsActive || !technician.IsTechnician)
                throw new ConflictException("optical_job_technician_invalid", "الموظف المحدد غير فعال أو غير معرف كفني.");
        }
        var previousTechnicianId = job.AssignedTechnicianId;
        var now = timeProvider.GetUtcNow();
        job.AssignTechnician(request.TechnicianId, now);
        jobs.Update(job);
        var action = !previousTechnicianId.HasValue && request.TechnicianId.HasValue
            ? "تعيين الفني"
            : previousTechnicianId.HasValue && request.TechnicianId.HasValue && previousTechnicianId != request.TechnicianId
                ? "إعادة تعيين الفني"
                : request.TechnicianId.HasValue ? "تأكيد تعيين الفني" : "إلغاء تعيين الفني";
        await AddHistoryAsync(job.Id, job.Status, job.Status, action, cancellationToken);
    }

    public async Task IssueMaterialsAsync(Guid id, IssueOpticalJobMaterialsRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (job.Status is not (OpticalJobStatus.MaterialsAvailable or OpticalJobStatus.InProduction))
            throw new ConflictException("optical_job_material_issue_state", "لا يمكن صرف مواد إضافية في حالة أمر المعمل الحالية.");
        var lines = request.Lines
            .Where(x => x.ProductVariantId != Guid.Empty && x.Quantity > 0m)
            .Select(x => new OpticalJobMaterialIssueLine(x.ProductVariantId, x.Quantity))
            .ToArray();
        if (lines.Length == 0)
            throw new ConflictException("optical_job_materials_required", "يجب تحديد مادة إضافية واحدة على الأقل للصرف.");

        var requestId = request.RequestId ?? Guid.NewGuid();
        await inventory.IssueMaterialsAsync(job.Id, requestId, job.JobCode, request.WarehouseId, lines,
            CurrentUserText(), timeProvider.GetUtcNow(), cancellationToken);
        if (job.Status == OpticalJobStatus.MaterialsAvailable)
        {
            var from = job.Status;
            job.MarkMaterialsIssued();
            await AddHistoryAsync(job.Id, from, job.Status, "تم صرف مواد إنتاج إضافية.", cancellationToken);
        }
        else
        {
            await AddHistoryAsync(job.Id, job.Status, job.Status, "تم صرف مواد إنتاج إضافية أثناء العمل.", cancellationToken);
        }
        jobs.Update(job);
    }

    public async Task StartAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (job.Status is not (OpticalJobStatus.MaterialsAvailable or OpticalJobStatus.MaterialsIssued))
            throw new ConflictException("optical_job_materials_not_available", "لا يمكن بدء العمل قبل توفر المواد المطلوبة.");
        if (!job.AssignedTechnicianId.HasValue)
            throw new ConflictException("optical_job_technician_required", "يجب تعيين فني قبل بدء العمل.");
        if (await HasOpenBlockingBreakageAsync(job.Id, cancellationToken) || await HasAwaitingMaterialRemakeAsync(job.Id, cancellationToken))
            throw new ConflictException("optical_job_blocked", "لا يمكن بدء العمل مع وجود كسر أو إعادة تصنيع بانتظار مواد.");

        var from = job.Status;
        job.Start(timeProvider.GetUtcNow());
        await AddHistoryAsync(job.Id, from, job.Status, "تم بدء الإنتاج.", cancellationToken);

        var order = await orders.GetAggregateAsync(job.CustomerOrderId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), job.CustomerOrderId);
        if (order.Status == CustomerOrderStatus.ReadyForProduction)
            order.StartProduction();
        jobs.Update(job);
        orders.Update(order);
    }

    public async Task SendToQualityControlAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (job.Status != OpticalJobStatus.InProduction)
            throw new ConflictException("optical_job_not_in_production", "يمكن إرسال أمر المعمل لفحص الجودة من حالة قيد الإنتاج فقط.");
        if (await HasOpenBlockingBreakageAsync(job.Id, cancellationToken))
            throw new ConflictException("optical_job_breakage_open", "لا يمكن إرسال أمر المعمل للجودة قبل إغلاق الكسور المفتوحة.");
        if (await HasOpenRemakeAsync(job.Id, cancellationToken))
            throw new ConflictException("optical_job_remake_open", "لا يمكن إرسال أمر المعمل للجودة قبل اكتمال إعادة التصنيع المفتوحة.");

        var attempts = await qualityChecks.CountAsync(new Specification<OpticalQualityCheck>().Where(x => x.OpticalJobId == job.Id), cancellationToken);
        var check = OpticalQualityCheck.Create(Guid.NewGuid(), job.Id, checked((int)attempts + 1));
        var sequence = 1;
        foreach (var template in DefaultQcChecklist)
        {
            var item = OpticalQualityCheckItem.Create(Guid.NewGuid(), check.Id, template.Code, template.Name, sequence++);
            check.AddItem(item);
        }
        await qualityChecks.AddAsync(check, cancellationToken);

        var from = job.Status;
        job.SendToQualityControl();
        await AddHistoryAsync(job.Id, from, job.Status, $"إرسال إلى QC - المحاولة {check.AttemptNumber}.", cancellationToken);
        jobs.Update(job);
    }

    public async Task CompleteQualityControlAsync(Guid id, Guid qualityCheckId, CompleteOpticalQualityCheckRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (job.Status != OpticalJobStatus.AwaitingQC)
            throw new ConflictException("optical_job_not_awaiting_qc", "أمر المعمل ليس بانتظار فحص الجودة.");

        var check = await qualityChecks.GetForUpdateAsync(qualityCheckId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalQualityCheck), qualityCheckId);
        if (check.OpticalJobId != job.Id || check.Result != OpticalQualityCheckResult.Pending)
            throw new ConflictException("optical_qc_invalid", "جلسة فحص الجودة غير صالحة لهذا الأمر.");
        SalesConcurrency.Ensure(request.QualityCheckRowVersion, check.RowVersion, "فحص الجودة");

        var items = await qualityCheckItems.ListAsync(
            new Specification<OpticalQualityCheckItem>().Where(x => x.QualityCheckId == check.Id).Tracking(), cancellationToken);
        if (items.Count == 0)
            throw new ConflictException("optical_qc_items_missing", "قائمة فحص الجودة غير موجودة.");
        var groupedIncoming = request.Items.GroupBy(x => x.CheckCode, StringComparer.OrdinalIgnoreCase).ToArray();
        if (groupedIncoming.Any(x => x.Count() != 1))
            throw new ConflictException("optical_qc_duplicate_check", "لا يمكن إرسال بند فحص الجودة نفسه أكثر من مرة.");
        var incoming = groupedIncoming.ToDictionary(x => x.Key, x => x.Single(), StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!incoming.TryGetValue(item.CheckCode, out var value))
                throw new ConflictException("optical_qc_incomplete", $"يجب تقييم بند الجودة '{item.CheckName}'.");
            if (value.Result == OAS.Contracts.Sales.Enums.OpticalQualityCheckItemResult.NotChecked)
                throw new ConflictException("optical_qc_incomplete", $"يجب تقييم بند الجودة '{item.CheckName}'.");
            item.SetResult((OpticalQualityCheckItemResult)(byte)value.Result, value.Notes);
            qualityCheckItems.Update(item);
        }

        var hasFail = items.Any(x => x.Result == OpticalQualityCheckItemResult.Fail);
        var actor = CurrentUserGuid();
        var now = timeProvider.GetUtcNow();
        var from = job.Status;
        if (!hasFail)
        {
            check.CompletePassed(actor, now, true, request.Notes);
            job.PassQualityControl();
            await CompletePassedRemakesAndBreakagesAsync(job.Id, now, cancellationToken);
            await AddHistoryAsync(job.Id, from, job.Status, $"اجتاز QC - المحاولة {check.AttemptNumber}.", cancellationToken);
        }
        else
        {
            if (!request.FailureAction.HasValue || string.IsNullOrWhiteSpace(request.Reason))
                throw new ConflictException("optical_qc_failure_action_required", "عند فشل QC يجب تحديد Rework أو Remake وسبب الفشل.");
            var action = (OpticalQcFailureAction)(byte)request.FailureAction.Value;
            check.CompleteFailed(actor, now, action, request.Reason, true);
            job.FailQualityControl(action);
            await AddHistoryAsync(job.Id, from, job.Status, $"فشل QC ({action}): {request.Reason}", cancellationToken);

            if (action == OpticalQcFailureAction.Remake)
            {
                if (!request.RemakeLineId.HasValue)
                    throw new ConflictException("optical_qc_remake_line_required", "يجب تحديد سطر العدسة المطلوب إعادة تصنيعه.");
                await CreateRemakeInternalAsync(job, request.RemakeLineId.Value, check.Id, null, null,
                    request.RemakeQuantity <= 0m ? 1m : request.RemakeQuantity, request.Reason, null, cancellationToken);
            }
        }
        qualityChecks.Update(check);
        jobs.Update(job);
    }

    // Compatibility operation retained for older clients: it marks all QC checks as Pass.
    public async Task PassQualityControlAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        var pending = (await qualityChecks.ListAsync(new Specification<OpticalQualityCheck>()
            .Where(x => x.OpticalJobId == job.Id && x.Result == OpticalQualityCheckResult.Pending).Tracking(), cancellationToken))
            .OrderByDescending(x => x.AttemptNumber).FirstOrDefault();
        if (pending is null)
            throw new ConflictException("optical_qc_required", "لا توجد جلسة QC معلقة لهذا الأمر.");
        var items = await qualityCheckItems.ListAsync(new Specification<OpticalQualityCheckItem>()
            .Where(x => x.QualityCheckId == pending.Id).Tracking(), cancellationToken);
        foreach (var item in items)
        {
            item.SetResult(OpticalQualityCheckItemResult.Pass, null);
            qualityCheckItems.Update(item);
        }
        var passAt = timeProvider.GetUtcNow();
        pending.CompletePassed(CurrentUserGuid(), passAt, items.Count > 0, "اجتياز سريع من الواجهة المتوافقة.");
        var from = job.Status;
        job.PassQualityControl();
        await CompletePassedRemakesAndBreakagesAsync(job.Id, passAt, cancellationToken);
        await AddHistoryAsync(job.Id, from, job.Status, $"اجتاز QC - المحاولة {pending.AttemptNumber}.", cancellationToken);
        qualityChecks.Update(pending);
        jobs.Update(job);
    }

    public async Task<Guid> RecordBreakageAsync(Guid id, RecordOpticalJobBreakageRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await breakages.ListAsync(new Specification<OpticalJobBreakage>()
                .Where(x => x.OpticalJobId == job.Id && x.IdempotencyKey == request.IdempotencyKey), cancellationToken);
            if (existing.Count > 0) return existing[0].Id;
        }
        if (job.Status is not (OpticalJobStatus.InProduction or OpticalJobStatus.AwaitingQC))
            throw new ConflictException("optical_breakage_state_invalid", "يمكن تسجيل الكسر أثناء الإنتاج أو فحص الجودة فقط.");
        var line = await jobLines.GetByIdAsync(request.OpticalJobLineId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJobLine), request.OpticalJobLineId);
        if (line.OpticalJobId != job.Id || line.ProductVariantId != request.ProductVariantId)
            throw new ConflictException("optical_breakage_line_mismatch", "سطر أو صنف الكسر لا ينتمي إلى أمر المعمل.");
        if (request.Quantity <= 0m || request.Quantity > line.Quantity)
            throw new ConflictException("optical_breakage_quantity_invalid", "كمية الكسر غير صالحة.");

        var actor = CurrentUserGuid();
        var now = timeProvider.GetUtcNow();
        var breakage = OpticalJobBreakage.Create(Guid.NewGuid(), job.Id, line.Id, request.ProductVariantId,
            request.Eye.HasValue ? (EyeSide?)(byte)request.Eye.Value : line.Eye,
            request.Quantity, request.ReasonCode, request.ReasonText, request.TechnicianId ?? job.AssignedTechnicianId,
            request.RequiresReplacement, actor, now, request.IdempotencyKey);
        await breakages.AddAsync(breakage, cancellationToken);

        // A replacement is an additional physical unit consumed because the first unit broke. We only
        // scrap it immediately when stock exists. When it does not, the breakage waits for Purchasing;
        // this avoids a negative balance and avoids consuming the already-posted sale line twice.
        if (!request.RequiresReplacement)
        {
            // The sold unit was already consumed by Sales posting. Closing a non-replacement
            // breakage must not decrease inventory a second time.
            breakage.Close(now);
        }
        else
        {
            if (request.WarehouseId is not Guid warehouseId || warehouseId == Guid.Empty)
                throw new ConflictException("optical_breakage_warehouse_required", "يجب تحديد المخزن عند طلب بديل للكسر.");

            var available = await inventory.GetAvailableQuantityAsync(warehouseId, request.ProductVariantId, cancellationToken);
            if (available >= request.Quantity)
            {
                // Create the remake while the replacement is still visible as available; then
                // consume that one additional unit exactly once against the breakage source.
                await CreateRemakeInternalAsync(job, line.Id, null, breakage.Id, request.ProductVariantId,
                    request.Quantity, request.ReasonText ?? request.ReasonCode, warehouseId, cancellationToken);
                var scrap = await inventory.ScrapAsync(breakage.Id, job.JobCode, warehouseId, request.ProductVariantId,
                    request.Quantity, CurrentUserText(), now, cancellationToken);
                var journalId = await accounting.PostBreakageAsync(breakage, warehouseId, scrap.TotalCostBase, actor, now, cancellationToken);
                breakage.LinkScrap(scrap.InventoryTransactionId, journalId);
                breakage.MarkReplacementAvailable();
            }
            else
            {
                breakage.MarkAwaitingReplacement();
                await CreateRemakeInternalAsync(job, line.Id, null, breakage.Id, request.ProductVariantId,
                    request.Quantity, request.ReasonText ?? request.ReasonCode, warehouseId, cancellationToken);
            }
        }

        breakages.Update(breakage);
        await AddHistoryAsync(job.Id, job.Status, job.Status, $"تسجيل كسر: {request.ReasonCode}.", cancellationToken, request.IdempotencyKey);
        return breakage.Id;
    }

    public async Task<Guid> CreateRemakeAsync(Guid id, CreateOpticalJobRemakeRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        var remake = await CreateRemakeInternalAsync(job, request.OpticalJobLineId, request.SourceQualityCheckId,
            request.SourceBreakageId, request.ProductVariantId, request.Quantity, request.Reason, request.WarehouseId, cancellationToken);
        return remake.Id;
    }

    public async Task StartRemakeAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        var remake = await remakes.GetForUpdateAsync(remakeId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJobRemake), remakeId);
        if (remake.OpticalJobId != job.Id) throw new ConflictException("optical_remake_job_mismatch", "إعادة التصنيع لا تخص أمر المعمل.");
        SalesConcurrency.Ensure(request.RemakeRowVersion, remake.RowVersion, "إعادة التصنيع");
        if (job.Status != OpticalJobStatus.InProduction)
            throw new ConflictException("optical_remake_job_state", "يجب أن يكون أمر المعمل في الإنتاج لبدء إعادة التصنيع.");
        remake.Start(timeProvider.GetUtcNow());
        remakes.Update(remake);
        await AddHistoryAsync(job.Id, job.Status, job.Status, $"بدء إعادة تصنيع {remake.Id:D}.", cancellationToken);
    }

    public async Task SendRemakeToQualityControlAsync(Guid id, Guid remakeId, OpticalJobRemakeActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        var remake = await remakes.GetForUpdateAsync(remakeId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJobRemake), remakeId);
        if (remake.OpticalJobId != job.Id) throw new ConflictException("optical_remake_job_mismatch", "إعادة التصنيع لا تخص أمر المعمل.");
        SalesConcurrency.Ensure(request.RemakeRowVersion, remake.RowVersion, "إعادة التصنيع");
        if (job.Status != OpticalJobStatus.InProduction)
            throw new ConflictException("optical_remake_job_state", "يجب أن يكون أمر المعمل في الإنتاج قبل إرسال إعادة التصنيع إلى QC.");

        remake.SendToQc();
        // Remake QC becomes the next job QC attempt. Closing the remake happens when that QC passes.
        remakes.Update(remake);
        var from = job.Status;
        job.SendToQualityControl();
        var attempts = await qualityChecks.CountAsync(new Specification<OpticalQualityCheck>().Where(x => x.OpticalJobId == job.Id), cancellationToken);
        var check = OpticalQualityCheck.Create(Guid.NewGuid(), job.Id, checked((int)attempts + 1));
        var seq = 1;
        foreach (var template in DefaultQcChecklist)
            check.AddItem(OpticalQualityCheckItem.Create(Guid.NewGuid(), check.Id, template.Code, template.Name, seq++));
        await qualityChecks.AddAsync(check, cancellationToken);
        await AddHistoryAsync(job.Id, from, job.Status, $"إعادة التصنيع أرسلت إلى QC - المحاولة {check.AttemptNumber}.", cancellationToken);
        jobs.Update(job);
    }

    public async Task MarkReadyAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (job.Status != OpticalJobStatus.QCPassed)
            throw new ConflictException("optical_job_qc_required", "يجب تسجيل اجتياز فحص الجودة أولًا قبل جعل أمر المعمل جاهزًا للتسليم.");
        if (await HasOpenBlockingBreakageAsync(job.Id, cancellationToken))
            throw new ConflictException("optical_job_breakage_open", "لا يمكن جعل الأمر جاهزًا مع وجود كسر مفتوح.");
        if (await HasOpenRemakeAsync(job.Id, cancellationToken))
            throw new ConflictException("optical_job_remake_open", "لا يمكن جعل الأمر جاهزًا مع وجود إعادة تصنيع مفتوحة.");

        var latest = (await qualityChecks.ListAsync(new Specification<OpticalQualityCheck>().Where(x => x.OpticalJobId == job.Id), cancellationToken))
            .OrderByDescending(x => x.AttemptNumber).FirstOrDefault();
        if (latest?.Result != OpticalQualityCheckResult.Passed)
            throw new ConflictException("optical_job_latest_qc_not_passed", "آخر محاولة QC لم تجتز بنجاح.");


        var from = job.Status;
        job.MarkReadyForDelivery(timeProvider.GetUtcNow());
        await AddHistoryAsync(job.Id, from, job.Status, "اجتاز آخر QC ولا توجد عوائق مفتوحة.", cancellationToken);
        jobs.Update(job);
        await sales.MarkReadyForDeliveryAsync(job, cancellationToken);
    }

    public async Task DeliverAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (job.Status == OpticalJobStatus.Delivered) return;
        if (job.Status != OpticalJobStatus.ReadyForDelivery)
            throw new ConflictException("optical_job_not_ready_for_delivery", "أمر المعمل ليس جاهزًا للتسليم.");
        await sales.EnsureDeliveryEligibleAsync(job, cancellationToken);
        var from = job.Status;
        job.MarkDelivered(timeProvider.GetUtcNow());
        await AddHistoryAsync(job.Id, from, job.Status, "تم التسليم النهائي.", cancellationToken);
        jobs.Update(job);
        await sales.MarkDeliveredAsync(job, cancellationToken);
    }

    public async Task CancelAsync(Guid id, OpticalJobActionRequest request, CancellationToken cancellationToken = default)
    {
        var job = await GetForUpdateAsync(id, request.RowVersion, cancellationToken);
        if (await HasOpenBlockingBreakageAsync(job.Id, cancellationToken) || await HasOpenRemakeAsync(job.Id, cancellationToken))
            throw new ConflictException("optical_job_cancel_blocked", "يجب إغلاق الكسور وإعادات التصنيع قبل إلغاء أمر المعمل.");
        var from = job.Status;
        job.Cancel();
        await AddHistoryAsync(job.Id, from, job.Status, "تم إلغاء أمر المعمل.", cancellationToken);
        jobs.Update(job);
    }

    public Task<OpticalJobDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        queryService.GetDetailsAsync(id, cancellationToken);

    public async Task<IReadOnlyList<OpticalJobWorkQueueDto>> GetWorkQueueAsync(CancellationToken cancellationToken = default)
    {
        var page = await queryService.GetWorkQueueAsync(new OpticalJobWorkQueueRequest
        {
            PageNumber = 1,
            PageSize = PageRequest.MaximumPageSize,
            SortBy = "RequiredDate"
        }, cancellationToken);
        return page.Items;
    }

    private async Task<OpticalJobRemake> CreateRemakeInternalAsync(OpticalJob job, Guid lineId, Guid? sourceQcId,
        Guid? sourceBreakageId, Guid? productVariantId, decimal quantity, string reason, Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var line = await jobLines.GetByIdAsync(lineId, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJobLine), lineId);
        if (line.OpticalJobId != job.Id)
            throw new ConflictException("optical_remake_line_mismatch", "سطر إعادة التصنيع لا ينتمي إلى أمر المعمل.");
        if (quantity <= 0m || quantity > line.Quantity)
            throw new ConflictException("optical_remake_quantity_invalid", "كمية إعادة التصنيع غير صالحة.");
        var variantId = productVariantId ?? line.ProductVariantId;
        var remake = OpticalJobRemake.Create(Guid.NewGuid(), job.Id, sourceQcId, sourceBreakageId, line.Id,
            variantId, quantity, reason, CurrentUserGuid(), timeProvider.GetUtcNow());

        if (variantId.HasValue && warehouseId.HasValue)
        {
            var available = await inventory.GetAvailableQuantityAsync(warehouseId.Value, variantId.Value, cancellationToken);
            if (available >= quantity)
                remake.MarkReady();
            else
            {
                var purchaseLineId = await purchasing.CreateReplacementDemandAsync(job, line, warehouseId.Value,
                    variantId.Value, quantity, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), reason, cancellationToken);
                remake.MarkAwaitingMaterials(purchaseLineId);
            }
        }
        await remakes.AddAsync(remake, cancellationToken);
        await AddHistoryAsync(job.Id, job.Status, job.Status, $"إنشاء إعادة تصنيع: {reason}", cancellationToken);
        return remake;
    }

    private async Task CompletePassedRemakesAndBreakagesAsync(Guid jobId, DateTimeOffset now, CancellationToken ct)
    {
        var remakeRows = await remakes.ListAsync(new Specification<OpticalJobRemake>()
            .Where(x => x.OpticalJobId == jobId && x.Status == OpticalRemakeStatus.AwaitingQC).Tracking(), ct);
        foreach (var remake in remakeRows)
        {
            remake.Complete(now);
            remakes.Update(remake);
            if (!remake.SourceBreakageId.HasValue) continue;
            var breakage = await breakages.GetForUpdateAsync(remake.SourceBreakageId.Value, ct);
            if (breakage is not null && breakage.Status is not (OpticalBreakageStatus.Closed or OpticalBreakageStatus.Cancelled))
            {
                breakage.Close(now);
                breakages.Update(breakage);
            }
        }
    }

    private async Task<OpticalJob> GetForUpdateAsync(Guid id, string rowVersion, CancellationToken cancellationToken)
    {
        var job = await jobs.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);
        SalesConcurrency.Ensure(rowVersion, job.RowVersion, "أمر المعمل");
        return job;
    }

    private async Task AddHistoryAsync(Guid jobId, OpticalJobStatus? from, OpticalJobStatus to, string? reason,
        CancellationToken cancellationToken, string? correlationId = null)
    {
        var correlation = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId;
        await statusHistory.AddAsync(OpticalJobStatusHistory.Create(Guid.NewGuid(), jobId, from, to, reason,
            CurrentUserGuid(), timeProvider.GetUtcNow(), correlation), cancellationToken);
    }

    private async Task<bool> HasOpenBlockingBreakageAsync(Guid jobId, CancellationToken ct) =>
        await breakages.CountAsync(new Specification<OpticalJobBreakage>().Where(x => x.OpticalJobId == jobId &&
            x.Status != OpticalBreakageStatus.Closed && x.Status != OpticalBreakageStatus.Cancelled), ct) > 0;

    private async Task<bool> HasOpenRemakeAsync(Guid jobId, CancellationToken ct) =>
        await remakes.CountAsync(new Specification<OpticalJobRemake>().Where(x => x.OpticalJobId == jobId &&
            x.Status != OpticalRemakeStatus.Completed && x.Status != OpticalRemakeStatus.Cancelled), ct) > 0;

    private async Task<bool> HasAwaitingMaterialRemakeAsync(Guid jobId, CancellationToken ct) =>
        await remakes.CountAsync(new Specification<OpticalJobRemake>().Where(x => x.OpticalJobId == jobId &&
            x.Status == OpticalRemakeStatus.AwaitingMaterials), ct) > 0;

    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) && id != Guid.Empty
        ? id
        : new Guid("00000000-0000-0000-0000-000000000001");

    private string CurrentUserText() => string.IsNullOrWhiteSpace(currentUser.UserId) ? "system" : currentUser.UserId!;

    private static OpticalJobLineType MapLineType(SalesLineType type) => type switch
    {
        SalesLineType.Frame => OpticalJobLineType.Frame,
        SalesLineType.Lens => OpticalJobLineType.Lens,
        SalesLineType.Service => OpticalJobLineType.Service,
        SalesLineType.Accessory => OpticalJobLineType.Accessory,
        _ => OpticalJobLineType.Other
    };
}
