using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.SupplierAllocations;
using OAS.Contracts.Sales.Enums;

namespace OAS.Client.Accounting.Components;

public partial class PaymentAllocationEditor
{
    public sealed class FormModel
    {
        public PaymentSourceType PaymentSourceType { get; set; } = PaymentSourceType.ReceiptVoucher;
        public Guid? InitialVoucherId { get; set; }
        public decimal DraftAllocatedAmount { get; set; }
        public bool IsReversed { get; set; }
        public string? RowVersion { get; set; }
        public string? ReversalReason { get; set; }
        public Guid? ReceiptVoucherLineId { get; set; }
        public Guid? PaymentVoucherLineId { get; set; }
        public Guid? SourceLineId
        {
            get => PaymentSourceType == PaymentSourceType.ReceiptVoucher ? ReceiptVoucherLineId : PaymentVoucherLineId;
            set
            {
                if (PaymentSourceType == PaymentSourceType.ReceiptVoucher) { ReceiptVoucherLineId = value; PaymentVoucherLineId = null; }
                else { PaymentVoucherLineId = value; ReceiptVoucherLineId = null; }
            }
        }
        public AllocationTargetDocumentType TargetDocumentType { get; set; } = AllocationTargetDocumentType.SalesInvoice;
        public Guid? TargetDocumentId { get; set; }
        public string TargetDocumentIdText { get => TargetDocumentId?.ToString("D") ?? string.Empty; set => TargetDocumentId = Guid.TryParse(value, out var id) ? id : null; }
        public Guid? CurrencyId { get; set; }
        public string? CurrencyCodeSnapshot { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? BaseAllocatedAmount { get; set; }
        public DateTimeOffset AllocatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    [Parameter] public FormModel Model { get; set; } = new();
    [Parameter] public bool IsEditable { get; set; }
    [Parameter] public bool IsNew { get; set; }
    [Parameter] public Guid? AllocationId { get; set; }
    [Parameter] public EventCallback OnAllocationsChanged { get; set; }
    [Parameter] public EventCallback OnModelChanged { get; set; }
    [Parameter] public EventCallback<Guid> OnOpenAllocation { get; set; }
    [Inject] private IUiDialogService Dialog { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private bool IsReceipt => Model.PaymentSourceType == PaymentSourceType.ReceiptVoucher;
    private bool _busy;
    private bool _loadingInvoices;
    private string? _invoiceLookupValue;
    private Guid? _voucherId;
    private Guid? _loadedVoucherId;
    private string _voucherNumber = "";
    private string? _partyName;
    private DateOnly _voucherDate;
    private Guid? _selectedLineId;
    private List<SourceLine> _sourceLines = [];
    private List<InvoiceRow> _invoiceRows = [];
    private List<PaymentAllocationDto> _selectedAllocations = [];
    private bool _showHistory;
    private string _reverseDateText = DateTime.Today.ToString("yyyy-MM-dd");
    private string _reverseReason = "";

    private sealed class SourceLine
    {
        public Guid Id { get; init; }
        public Guid? PartyId { get; init; }
        public string? PartyName { get; init; }
        public Guid? CurrencyId { get; init; }
        public string Currency { get; init; } = "";
        public string Method { get; init; } = "";
        public decimal ExchangeRate { get; init; }
        public decimal Amount { get; init; }
        public decimal Applied { get; set; }
        public decimal Available => Math.Max(0m, Amount - Applied);
    }
    private sealed class InvoiceRow
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = "";
        public DateOnly Date { get; init; }
        public Guid CurrencyId { get; init; }
        public string Currency { get; init; } = "";
        public decimal Rate { get; init; }
        public decimal Total { get; init; }
        public decimal Paid { get; init; }
        public decimal Outstanding { get; init; }
        public decimal MaxSourceAmount { get; init; }
        public decimal Amount { get; set; }
        public bool Saved { get; set; }
        public string? RequestKey { get; set; }
    }

    private SourceLine? SelectedLine => _sourceLines.FirstOrDefault(x => x.Id == _selectedLineId);
    private IEnumerable<InvoiceRow> FilteredInvoices => _invoiceRows;
    private decimal PendingTotal => _invoiceRows.Where(x => !x.Saved).Sum(x => x.Amount);
    private decimal ProgressPercent => SelectedLine is { Available: > 0m } line
        ? Math.Min(100m, Math.Max(0m, 100m * PendingTotal / line.Available)) : 0m;
    private static string Amount(decimal value) => value.ToString("N2");

    protected override async Task OnParametersSetAsync()
    {
        if (!IsNew || !Model.InitialVoucherId.HasValue || Model.InitialVoucherId == _loadedVoucherId) return;
        _loadedVoucherId = Model.InitialVoucherId;
        await LoadVoucherAsync(Model.InitialVoucherId.Value);
    }

    private UiLookupItem? VoucherLookupItem => _voucherId is Guid id
        ? new UiLookupItem(id.ToString("D"), _voucherNumber, _voucherDate.ToString("yyyy/MM/dd")) : null;
    private UiLookupItem? SourceLineLookupItem => SelectedLine is { } line
        ? ToLineLookup(line) : null;
    private UiLookupItem? InvoiceLookupItem => Guid.TryParse(_invoiceLookupValue, out var id)
        ? _invoiceRows.Where(x => x.Id == id).Select(ToInvoiceLookup).FirstOrDefault() : null;

    private static UiLookupItem ToLineLookup(SourceLine line) => new(
        line.Id.ToString("D"), $"{line.Method} · {Amount(line.Amount)} {line.Currency}",
        $"{line.PartyName ?? "طرف غير مسمى"} · متاح {Amount(line.Available)} {line.Currency}",
        Disabled: line.PartyId is null || line.CurrencyId is null || line.ExchangeRate <= 0m || line.Available <= 0m);

    private static UiLookupItem ToInvoiceLookup(InvoiceRow row) => new(
        row.Id.ToString("D"), row.Code,
        $"{row.Currency} · متبقي {Amount(row.Outstanding)} · {row.Date:yyyy/MM/dd}",
        Disabled: row.Saved || row.MaxSourceAmount <= 0m);

    private async Task<IReadOnlyList<UiLookupItem>> SearchVoucherOptionsAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            // Filter posted vouchers in the database BEFORE paging.
            // A capped list avoids loading all historical vouchers on focus.
            var request = new PageRequest { PageNumber = 1, PageSize = 200, Search = query, Status = "posted" };
            if (IsReceipt)
            {
                var page = await Accounting.GetReceiptVouchersPageAsync(request, cancellationToken);
                return page.Items.Where(x => x.Status == ReceiptVoucherStatus.Posted)
                    .Select(x => new UiLookupItem(x.Id.ToString("D"), x.VoucherNumber,
                        $"{x.VoucherDate:yyyy/MM/dd} · {x.Description}"))
                    .ToArray();
            }
            else
            {
                var page = await Accounting.GetPaymentVouchersPageAsync(request, cancellationToken);
                return page.Items.Where(x => x.Status == PaymentVoucherStatus.Posted)
                    .Select(x => new UiLookupItem(x.Id.ToString("D"), x.VoucherNumber,
                        $"{x.VoucherDate:yyyy/MM/dd} · {x.Description}"))
                    .ToArray();
            }
        }
        catch (OperationCanceledException) { return []; }
        catch (Exception ex)
        {
            await ShowNoticeAsync(UiOperationStatus.Failure, "البحث عن السند", $"تعذر تحميل السندات: {ex.Message}");
            return [];
        }
    }

    private async Task OnVoucherLookupChangedAsync(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            if (id != _voucherId) await LoadVoucherAsync(id);
            return;
        }
        if (!_voucherId.HasValue) return;
        if (PendingTotal > 0m)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "تخصيصات غير محفوظة", "احفظ مبالغ التخصيص أو ألغِها قبل تغيير السند.");
            return;
        }
        _voucherId = null;
        _selectedLineId = null;
        _sourceLines.Clear();
        _invoiceRows.Clear();
        _selectedAllocations.Clear();
        _invoiceLookupValue = null;
        await NotifyDraftChangedAsync();
    }

    private Task<IReadOnlyList<UiLookupItem>> SearchSourceLinesAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = _sourceLines.Select(ToLineLookup);
        if (!string.IsNullOrWhiteSpace(query))
            options = options.Where(x => x.PrimaryText.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (x.SecondaryText?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
        return Task.FromResult<IReadOnlyList<UiLookupItem>>(options.ToArray());
    }

    private async Task OnSourceLineLookupChangedAsync(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            if (id != _selectedLineId)
            {
                var line = _sourceLines.FirstOrDefault(x => x.Id == id);
                if (line is not null) await SelectLineAsync(line);
            }
            return;
        }
        if (!_selectedLineId.HasValue) return;
        if (PendingTotal > 0m)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "تخصيصات غير محفوظة", "احفظ مبالغ التخصيص أو ألغِها قبل تغيير سطر السند.");
            return;
        }
        _selectedLineId = null;
        _invoiceLookupValue = null;
        _invoiceRows.Clear();
        _selectedAllocations.Clear();
        await NotifyDraftChangedAsync();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchInvoiceOptionsAsync(string query, CancellationToken cancellationToken)
    {
        if (SelectedLine?.PartyId is null) return [];
        if (IsReceipt && query.Trim().Length >= 2 && !_invoiceRows.Any(x => x.Code.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)))
            await SearchAdditionalCustomerInvoicesAsync(query, cancellationToken);
        return _invoiceRows.Where(x => string.IsNullOrWhiteSpace(query)
                || x.Code.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(ToInvoiceLookup).ToArray();
    }

    private async Task OnInvoiceLookupChangedAsync(string? value)
    {
        _invoiceLookupValue = value;
        if (!Guid.TryParse(value, out var id)) return;
        var invoice = _invoiceRows.FirstOrDefault(x => x.Id == id);
        if (invoice is null || invoice.Saved) return;
        if (invoice.Amount <= 0m)
        {
            if (SelectedLine?.Available <= PendingTotal)
            {
                await ShowNoticeAsync(UiOperationStatus.Information, "المبلغ المتاح", "تم توزيع المبلغ المتاح بالكامل. خفّض مبلغ فاتورة أخرى أو اختر سطر سداد مختلفًا.");
                return;
            }
            await ToggleInvoice(invoice, true);
        }
    }

    // Use the same global snackbar feedback as Chart of Accounts and the other
    // accounting screens. Confirmation of consequential actions remains modal.
    private Task ShowNoticeAsync(UiOperationStatus status, string title, string message)
    {
        var text = string.IsNullOrWhiteSpace(title) ? message : $"{title}: {message}";
        switch (status)
        {
            case UiOperationStatus.Success:
                Snackbar.Success(text, durationMilliseconds: 6000);
                break;
            case UiOperationStatus.Failure:
                Snackbar.Error(text, durationMilliseconds: 12000);
                break;
            case UiOperationStatus.Warning:
                // Partial saves and uncertain server results need explicit dismissal.
                if (title is "حفظ جزئي" or "حفظ غير مؤكد")
                    Snackbar.Show(text, AlertTone.Warning, autoClose: false);
                else
                    Snackbar.Warning(text, durationMilliseconds: 9000);
                break;
            default:
                Snackbar.Info(text, durationMilliseconds: 6500);
                break;
        }
        return Task.CompletedTask;
    }

    private async Task LoadVoucherAsync(Guid id)
    {
        if (PendingTotal > 0m)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "تخصيصات غير محفوظة", "احفظ مبالغ التخصيص الحالية أو ألغِها قبل فتح سند آخر.");
            return;
        }
        _busy = true;
        _voucherId = null;
        _selectedLineId = null;
        _sourceLines.Clear();
        _invoiceRows.Clear();
        _invoiceLookupValue = null;
        try
        {
            if (IsReceipt)
            {
                var voucher = await Accounting.GetReceiptVoucherByIdAsync(id);
                if (voucher is null || voucher.Status != ReceiptVoucherStatus.Posted)
                {
                    await ShowNoticeAsync(UiOperationStatus.Warning, "تعذر تحديد سند القبض", "السند غير موجود أو لم يتم ترحيله.");
                    return;
                }
                _voucherId = voucher.Id;
                _voucherNumber = voucher.VoucherNumber;
                _voucherDate = voucher.VoucherDate;
                _sourceLines = voucher.Lines.Select(x => new SourceLine
                {
                    Id = x.Id, PartyId = x.CustomerId, PartyName = x.PartyNameSnapshot,
                    CurrencyId = x.CurrencyId, Currency = x.CurrencyCodeSnapshot ?? "—",
                    Method = MethodLabel(x.PaymentMethod), Amount = x.Amount, ExchangeRate = x.ExchangeRate ?? 0m
                }).ToList();
            }
            else
            {
                var voucher = await Accounting.GetPaymentVoucherByIdAsync(id);
                if (voucher is null || voucher.Status != PaymentVoucherStatus.Posted)
                {
                    await ShowNoticeAsync(UiOperationStatus.Warning, "تعذر تحديد سند الصرف", "السند غير موجود أو لم يتم ترحيله.");
                    return;
                }
                _voucherId = voucher.Id;
                _voucherNumber = voucher.VoucherNumber;
                _voucherDate = voucher.VoucherDate;
                _sourceLines = voucher.Lines.Select(x => new SourceLine
                {
                    Id = x.Id, PartyId = x.SupplierId, PartyName = x.PartyNameSnapshot,
                    CurrencyId = x.CurrencyId, Currency = x.CurrencyCodeSnapshot ?? "—",
                    Method = MethodLabel(x.PaymentMethod), Amount = x.Amount, ExchangeRate = x.ExchangeRate ?? 0m
                }).ToList();
            }
            _partyName = _sourceLines.Select(x => x.PartyName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (!_sourceLines.Any(x => x.PartyId.HasValue))
                await ShowNoticeAsync(UiOperationStatus.Warning, "الطرف غير محدد", "لا يحتوي هذا السند على سطر مرتبط بعميل أو مورد مناسب لتخصيص الفواتير.");
            else
            {
                var eligible = _sourceLines.Where(x => x.PartyId.HasValue && x.CurrencyId.HasValue && x.ExchangeRate > 0m).ToList();
                if (eligible.Count == 1) await SelectLineAsync(eligible[0]);
            }
        }
        catch (Exception ex) { await ShowNoticeAsync(UiOperationStatus.Failure, "فتح السند", $"تعذر فتح السند: {ex.Message}"); }
        finally { _busy = false; }
    }

    private static string MethodLabel(PaymentMethod? method) => method?.ToString() switch
    {
        "Cash" => "نقدًا", "BankTransfer" => "تحويل بنكي", "Cheque" => "شيك", "Check" => "شيك",
        _ => method?.ToString() ?? "غير محدد"
    };

    private async Task SelectLineAsync(SourceLine line)
    {
        if (_selectedLineId != line.Id && PendingTotal > 0m)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "تغييرات غير محفوظة", "احفظ مبالغ التخصيص الحالية أو ألغِها قبل تغيير سطر السند.");
            return;
        }
        _selectedLineId = line.Id;
        _invoiceRows.Clear();
        _invoiceLookupValue = null;
        _selectedAllocations.Clear();
        _showHistory = false;
        _loadingInvoices = true;
        try
        {
            // The allocation query indexes PaymentSourceId by the actual voucher LINE id.
            decimal applied = 0m;
            for (var pageNo = 1; pageNo <= 20; pageNo++)
            {
                var page = await Accounting.GetPaymentAllocationsPageAsync(new PageRequest
                {
                    PageNumber = pageNo, PageSize = 200, Search = line.Id.ToString("D")
                });
                _selectedAllocations.AddRange(page.Items.Where(x => x.PaymentSourceId == line.Id));
                applied += page.Items.Where(x => !x.IsReversed && x.PaymentSourceId == line.Id).Sum(x => x.AllocatedAmount);
                if (!page.HasNextPage) break;
                if (pageNo == 20) throw new InvalidOperationException("عدد التخصيصات كبير جدًا لإظهار الرصيد الموثوق؛ يرجى تضييق النطاق.");
            }
            line.Applied = applied;
            if (line.PartyId is null) { await ShowNoticeAsync(UiOperationStatus.Warning, "الطرف غير محدد", "يجب أن يكون سطر السند مرتبطًا بعميل في القبض أو مورد في الصرف."); return; }
            if (line.Available <= 0m) { await ShowNoticeAsync(UiOperationStatus.Information, "المبلغ المتاح", "لا يوجد مبلغ غير مخصص في هذا السطر."); return; }
            if (line.ExchangeRate <= 0m || !line.CurrencyId.HasValue) { await ShowNoticeAsync(UiOperationStatus.Warning, "بيانات العملة", "عملة السطر أو سعر صرفه غير صالح."); return; }

            if (IsReceipt)
                await LoadCustomerInvoicesAsync(line);
            else
                await LoadSupplierInvoicesAsync(line);
        }
        catch (Exception ex) { await ShowNoticeAsync(UiOperationStatus.Failure, "تحميل الأرصدة", $"تعذر تحميل أرصدة التخصيص: {ex.Message}"); }
        finally { _loadingInvoices = false; }
    }

    private async Task LoadCustomerInvoicesAsync(SourceLine source)
    {
        // The sales API filters by customer BEFORE pagination.
        // Still warn when the safety cap is reached rather than hiding later pages.
        const int maxPages = 10;
        for (var pageNo = 1; pageNo <= maxPages; pageNo++)
        {
            var page = await Sales.GetCustomerInvoicesPageAsync(source.PartyId!.Value,
                new PageRequest { PageNumber = pageNo, PageSize = 100 });
            foreach (var invoice in page.Items.Where(x => x.CustomerId == source.PartyId && x.Status == SalesInvoiceStatus.Posted))
            {
                var remaining = invoice.PaymentSummary.OutstandingAmount;
                if (remaining <= 0m) continue;
                AddInvoice(source, invoice.Id, invoice.InvoiceCode, invoice.InvoiceDate,
                    invoice.CurrencyId, invoice.CurrencyCodeSnapshot, invoice.ExchangeRate,
                    invoice.TotalAmount, invoice.PaymentSummary.PaidAmount + invoice.PaymentSummary.ReturnedAmount, remaining);
            }
            if (!page.HasNextPage) return;
            if (pageNo == maxPages)
                await ShowNoticeAsync(UiOperationStatus.Warning, "فواتير إضافية", "توجد صفحات فواتير إضافية لم تُحمّل. ابحث عن الفاتورة المطلوبة برقمها في القائمة.");
        }
    }

    private async Task SearchAdditionalCustomerInvoicesAsync(string searchText, CancellationToken cancellationToken)
    {
        var source = SelectedLine;
        if (!IsReceipt || source?.PartyId is null || string.IsNullOrWhiteSpace(searchText)) return;
        try
        {
            var page = await Sales.GetCustomerInvoicesPageAsync(source.PartyId.Value, new PageRequest
            {
                Search = searchText.Trim(), PageNumber = 1, PageSize = 200
            }, cancellationToken);
            foreach (var invoice in page.Items.Where(x =>
                x.CustomerId == source.PartyId && x.Status == SalesInvoiceStatus.Posted &&
                !_invoiceRows.Any(r => r.Id == x.Id)))
            {
                var remaining = invoice.PaymentSummary.OutstandingAmount;
                if (remaining <= 0m) continue;
                AddInvoice(source, invoice.Id, invoice.InvoiceCode, invoice.InvoiceDate,
                    invoice.CurrencyId, invoice.CurrencyCodeSnapshot, invoice.ExchangeRate,
                    invoice.TotalAmount, invoice.PaymentSummary.PaidAmount + invoice.PaymentSummary.ReturnedAmount, remaining);
            }
            if (page.HasNextPage)
                await ShowNoticeAsync(UiOperationStatus.Warning, "نتائج البحث", "عدد النتائج كبير. ابحث برقم الفاتورة بصورة أدق.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await ShowNoticeAsync(UiOperationStatus.Failure, "البحث عن فاتورة", $"تعذر البحث عن الفواتير: {ex.Message}");
        }
    }

    private async Task LoadSupplierInvoicesAsync(SourceLine source)
    {
        var center = await Purchasing.GetSupplierAllocationCenterAsync(source.PartyId!.Value);
        if (center is null) throw new InvalidOperationException("تعذر تحميل فواتير المورد المفتوحة.");
        foreach (var invoice in center.OpenInvoices.Where(x => x.OutstandingAmount > 0m))
        {
            AddInvoice(source, invoice.PurchaseInvoiceId, invoice.PurchaseInvoiceCode,
                invoice.InvoiceDate, invoice.CurrencyId, invoice.CurrencyCode, invoice.ExchangeRate,
                invoice.TotalAmount, invoice.AppliedPaymentAmount + invoice.AppliedCreditAmount, invoice.OutstandingAmount);
        }
    }

    private void AddInvoice(SourceLine source, Guid id, string code, DateOnly date, Guid currencyId,
        string? currency, decimal targetRate, decimal total, decimal paid, decimal outstanding)
    {
        var max = source.CurrencyId == currencyId
            ? outstanding
            : source.ExchangeRate > 0m && targetRate > 0m
                ? Math.Floor(outstanding * targetRate / source.ExchangeRate * 10000m) / 10000m
                : 0m;
        _invoiceRows.Add(new InvoiceRow
        {
            Id = id, Code = code, Date = date, CurrencyId = currencyId, Currency = currency ?? "—",
            Rate = targetRate, Total = total, Paid = paid, Outstanding = outstanding,
            MaxSourceAmount = Math.Max(0m, max)
        });
    }

    private async Task ToggleInvoice(InvoiceRow invoice, bool enabled)
    {
        if (invoice.Saved) return;
        if (invoice.RequestKey is not null)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "طلب حفظ معلق", "لا تزال نتيجة الحفظ السابقة غير مؤكدة؛ أعد المحاولة بنفس المبلغ لتجنب التكرار.");
            return;
        }
        invoice.RequestKey = null;
        invoice.Amount = enabled
            ? Math.Max(0m, Math.Min(invoice.MaxSourceAmount, (SelectedLine?.Available ?? 0m) - PendingTotal))
            : 0m;
        await NotifyDraftChangedAsync();
    }

    private async Task ChangeInvoiceAmount(InvoiceRow invoice, string? value)
    {
        if (invoice.Saved) return;
        if (invoice.RequestKey is not null)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "طلب حفظ معلق", "أعد محاولة حفظ هذه الفاتورة بالمبلغ السابق قبل تعديله، حتى لا يتكرر التخصيص.");
            return;
        }
        invoice.RequestKey = null;
        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var amount)) amount = 0m;
        invoice.Amount = Math.Max(0m, amount);
        if (invoice.Amount > invoice.MaxSourceAmount)
            await ShowNoticeAsync(UiOperationStatus.Warning, "تجاوز المبلغ المتاح", "مبلغ التخصيص أكبر من رصيد الفاتورة المتاح بعد تحويل العملة.");
        await NotifyDraftChangedAsync();
    }

    private async Task NotifyDraftChangedAsync()
    {
        Model.DraftAllocatedAmount = PendingTotal;
        await OnModelChanged.InvokeAsync();
    }

    private async Task SaveAllocationsAsync()
    {
        var source = SelectedLine;
        if (source is null || !source.CurrencyId.HasValue || PendingTotal <= 0m || PendingTotal > source.Available ||
            _invoiceRows.Any(x => !x.Saved && x.Amount > x.MaxSourceAmount)) return;
        var count = _invoiceRows.Count(x => !x.Saved && x.Amount > 0m);
        var confirmed = await Dialog.ConfirmAsync(
            "تأكيد تخصيص الدفعة",
            $"سيتم إنشاء {count} تخصيص مستقل من السند {_voucherNumber} بإجمالي {Amount(PendingTotal)} {source.Currency}. هل تريد المتابعة؟",
            AlertTone.Warning, "حفظ التخصيصات", "رجوع");
        if (!confirmed) return;
        _busy = true;
        var successCount = 0;
        string? failureMessage = null;
        try
        {
            foreach (var invoice in _invoiceRows.Where(x => !x.Saved && x.Amount > 0m))
            {
                invoice.RequestKey ??= $"OAS-ALLOC:{Guid.NewGuid():N}";
                try
                {
                    var created = await Accounting.CreatePaymentAllocationAsync(new CreatePaymentAllocationRequest(
                        IsReceipt ? source.Id : null,
                        IsReceipt ? null : source.Id,
                        IsReceipt ? AllocationTargetDocumentType.SalesInvoice : AllocationTargetDocumentType.PurchaseInvoice,
                        invoice.Id, invoice.Amount, null, invoice.RequestKey));
                    if (created is null) throw new InvalidOperationException("لم يُعد الخادم نتيجة التخصيص.");
                    invoice.Saved = true;
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureMessage = $"نجح حفظ {successCount} تخصيص، لكن فشل تخصيص الفاتورة {invoice.Code}: {ex.Message}. احتفظت بالعمليات غير المكتملة لإعادة المحاولة.";
                    break;
                }
            }
            if (successCount > 0)
            {
                await NotifyDraftChangedAsync();
                await OnAllocationsChanged.InvokeAsync();
            }
        }
        finally { _busy = false; }
        if (failureMessage is not null)
            await ShowNoticeAsync(UiOperationStatus.Warning, "حفظ جزئي", failureMessage);
        else if (successCount > 0)
            await ShowNoticeAsync(UiOperationStatus.Success, "تم حفظ التخصيصات", $"حُفظ {successCount} تخصيص بنجاح. يمكن تحديث الأرصدة للتحقق من المتاح النهائي.");
    }

    private async Task ClearDraftAmountsAsync()
    {
        if (PendingTotal <= 0m) return;
        if (_invoiceRows.Any(x => !x.Saved && x.RequestKey is not null && x.Amount > 0m))
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "حفظ غير مؤكد", "توجد فاتورة لديها محاولة حفظ لم تتضح نتيجتها. يجب إعادة المحاولة بالمبلغ نفسه قبل إلغائه.");
            return;
        }
        var confirmed = await Dialog.ConfirmAsync("إلغاء التحديد", "هل تريد مسح مبالغ التخصيص غير المحفوظة؟", AlertTone.Warning, "مسح المبالغ", "رجوع");
        if (!confirmed) return;
        foreach (var row in _invoiceRows.Where(x => !x.Saved)) row.Amount = 0m;
        _invoiceLookupValue = null;
        await NotifyDraftChangedAsync();
    }

    private async Task ReloadSelectedAsync()
    {
        if (PendingTotal > 0m)
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "تغييرات غير محفوظة", "احفظ التخصيصات المحددة أو ألغِها قبل تحديث الأرصدة.");
            return;
        }
        if (SelectedLine is { } line) await SelectLineAsync(line);
    }

    private Task OpenAllocationAsync(Guid id) => OnOpenAllocation.InvokeAsync(id);

    // Bind the native HTML date input explicitly. Using @bind with a string
    // causes Razor to generate a DateTime/TimeOnly binder on some .NET 9 builds.
    private void HandleReverseDateChange(ChangeEventArgs args)
    {
        _reverseDateText = args.Value?.ToString() ?? string.Empty;
    }

    private async Task ReverseAsync()
    {
        if (!AllocationId.HasValue || Model.IsReversed || string.IsNullOrWhiteSpace(Model.RowVersion) ||
            string.IsNullOrWhiteSpace(_reverseReason)) return;
        if (!DateOnly.TryParseExact(_reverseDateText, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
        {
            await ShowNoticeAsync(UiOperationStatus.Warning, "تاريخ غير صالح", "تحقق من تاريخ العكس قبل المتابعة.");
            return;
        }
        var confirmed = await Dialog.ConfirmAsync(
            "تأكيد عكس التخصيص", "سيتم عكس الأثر المحاسبي للتخصيص مع حفظ سبب العكس. هل تريد المتابعة؟",
            AlertTone.Warning, "عكس التخصيص", "رجوع");
        if (!confirmed) return;
        _busy = true;
        try
        {
            var result = await Accounting.ReversePaymentAllocationAsync(AllocationId.Value,
                new ReversePaymentAllocationRequest(date, _reverseReason.Trim(), Model.RowVersion));
            if (result is null) throw new InvalidOperationException("لم يؤكد الخادم نجاح العكس.");
            Model.IsReversed = true;
            Model.ReversalReason = _reverseReason.Trim();
            await ShowNoticeAsync(UiOperationStatus.Success, "نجاح العكس", "تم عكس تخصيص الدفعة بنجاح.");
            await OnAllocationsChanged.InvokeAsync();
        }
        catch (Exception ex) { await ShowNoticeAsync(UiOperationStatus.Failure, "فشل العكس", $"تعذر عكس التخصيص: {ex.Message}"); }
        finally { _busy = false; }
    }
}
