using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting;

public sealed class ProfileAccountingPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriods,
    IRepository<PostingProfile, Guid> profiles,
    IRepository<PostingProfileLine, Guid> profileLines,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    ISequenceNumberGenerator sequences) : IProfileAccountingPostingService
{
    public async Task ValidatePostingPeriodAsync(DateOnly postingDate, CancellationToken cancellationToken = default)
        => _ = await ResolvePeriodAsync(postingDate, cancellationToken);

    public async Task ValidateSupplierSubledgerAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => _ = await ResolveSupplierAccountAsync(supplierId, cancellationToken);

    public async Task<Guid> PostAsync(ProfileAccountingPostingRequest request, Guid postedBy, DateTime postedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceDocumentId == Guid.Empty) throw new ArgumentException("Source document id is required.", nameof(request));
        if (request.Lines.Count == 0) throw new ConflictException("accounting_posting_lines_required", "لا توجد أسطر محاسبية للترحيل.");

        var existingSpec = new Specification<JournalEntry>()
            .Where(x => x.SourceModule == request.Module && x.SourceDocumentType == request.SourceDocumentType && x.SourceDocumentId == request.SourceDocumentId)
            .ApplyPaging(0, 2);
        var existing = await journals.ListAsync(existingSpec, cancellationToken);
        if (existing.Count > 0) return existing[0].Id;

        var period = await ResolvePeriodAsync(request.PostingDate, cancellationToken);
        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة وتحديد العملة الأساسية أولًا.");
        var baseCurrency = await currencies.GetByIdAsync(accountingSettings.BaseCurrencyId, cancellationToken)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية المحددة في إعدادات المحاسبة غير موجودة.");

        Currency? transactionCurrency = null;
        if (request.TransactionCurrencyId.HasValue)
        {
            transactionCurrency = await currencies.GetByIdAsync(request.TransactionCurrencyId.Value, cancellationToken)
                ?? throw new ConflictException("accounting_transaction_currency_missing", "عملة المستند غير موجودة.");
            if (!transactionCurrency.IsActive) throw new ConflictException("accounting_transaction_currency_inactive", "عملة المستند غير فعالة.");
            if (request.ExchangeRate <= 0m) throw new ConflictException("accounting_exchange_rate_invalid", "سعر الصرف يجب أن يكون أكبر من صفر.");
        }

        var sequence = await sequences.NextAsync($"JournalEntry-{request.PostingDate.Year}", cancellationToken);
        var journal = JournalEntry.Create(Guid.NewGuid(), $"JV-{request.PostingDate.Year:0000}-{sequence:000000}", JournalType.Automatic,
            request.PostingDate, request.DocumentDate, period.Id, request.Description, request.Module,
            request.SourceDocumentType, request.SourceDocumentId, JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces);

        var profileCache = new Dictionary<string, IReadOnlyList<PostingProfileLine>>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 1;
        foreach (var source in request.Lines)
        {
            if (source.DebitBase < 0m || source.CreditBase < 0m || (source.DebitBase > 0m && source.CreditBase > 0m))
                throw new ConflictException("accounting_posting_line_invalid", "سطر الترحيل المحاسبي غير صالح.");
            if (source.DebitBase == 0m && source.CreditBase == 0m) continue;

            var accountId = source.UseSupplierSubledger
                ? (await ResolveSupplierAccountAsync(source.SupplierId ?? throw new ConflictException("accounting_supplier_required", "المورد مطلوب لحل حساب المورد."), cancellationToken)).Account.Id
                : await ResolveRoleAccountAsync(request.Module, source.ProfileDocumentType, source.AccountRole, profileCache, cancellationToken);

            var currency = source.UseTransactionCurrency
                ? transactionCurrency ?? throw new ConflictException("accounting_transaction_currency_required", "عملة المستند مطلوبة لهذا السطر.")
                : baseCurrency;
            var rate = source.UseTransactionCurrency ? request.ExchangeRate : 1m;
            var txDebit = source.UseTransactionCurrency && source.DebitBase > 0m ? Math.Round(source.DebitBase / rate, currency.DecimalPlaces) : source.DebitBase;
            var txCredit = source.UseTransactionCurrency && source.CreditBase > 0m ? Math.Round(source.CreditBase / rate, currency.DecimalPlaces) : source.CreditBase;

            journal.AddLine(JournalEntryLine.CreateMultiCurrency(
                Guid.NewGuid(), journal.Id, lineNumber++, accountId,
                source.DebitBase, source.CreditBase,
                currency.Id, currency.Code, currency.DecimalPlaces,
                txDebit, txCredit, rate, request.PostingDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
                source.Description, null, source.SupplierId, null, null, null,
                source.ProductVariantId, source.WarehouseId, source.SourceLineId));
        }

        if (!journal.IsBalanced())
            throw new ConflictException("accounting_journal_unbalanced", "تعذر الترحيل لأن القيد المحاسبي غير متوازن.");

        journal.SetPendingApproval();
        journal.Approve(postedBy, postedAtUtc);
        journal.Post(postedBy, postedAtUtc);
        await journals.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private async Task<FiscalPeriod> ResolvePeriodAsync(DateOnly postingDate, CancellationToken ct)
    {
        var spec = new Specification<FiscalPeriod>()
            .Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate)
            .ApplyPaging(0, 2);
        var periods = await fiscalPeriods.ListAsync(spec, ct);
        if (periods.Count == 0) throw new ConflictException("fiscal_period_not_found", "لا توجد فترة مالية تغطي تاريخ الترحيل.");
        if (periods.Count > 1) throw new ConflictException("fiscal_period_overlap", "يوجد أكثر من فترة مالية تغطي تاريخ الترحيل.");
        if (!periods[0].CanPostAccounting()) throw new ConflictException("fiscal_period_closed", "الفترة المالية مغلقة.");
        return periods[0];
    }

    private async Task<Guid> ResolveRoleAccountAsync(string module, string documentType, string? role,
        Dictionary<string, IReadOnlyList<PostingProfileLine>> cache, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(role)) throw new ConflictException("accounting_posting_role_required", "دور الحساب مطلوب لسطر الترحيل.");
        var key = $"{module}|{documentType}";
        if (!cache.TryGetValue(key, out var configured))
        {
            var profileSpec = new Specification<PostingProfile>()
                .Where(x => x.IsActive && x.Module == module && x.DocumentType == documentType)
                .ApplyPaging(0, 2);
            var matches = await profiles.ListAsync(profileSpec, ct);

            if (matches.Count == 0 && string.Equals(module, "Purchasing", StringComparison.OrdinalIgnoreCase))
            {
                await TryProvisionPurchasingProfileAsync(documentType, ct);
                matches = await profiles.ListAsync(profileSpec, ct);
            }
            else if (matches.Count == 0 && string.Equals(module, "Optical", StringComparison.OrdinalIgnoreCase))
            {
                await TryProvisionOpticalProfileAsync(documentType, ct);
                matches = await profiles.ListAsync(profileSpec, ct);
            }

            if (matches.Count != 1)
            {
                var message = matches.Count == 0 && string.Equals(module, "Purchasing", StringComparison.OrdinalIgnoreCase)
                    ? $"إعدادات ترحيل المشتريات غير مكتملة للمستند '{documentType}'. افتح إعدادات المحاسبة وحدد حسابات ترحيل المشتريات المطلوبة ثم احفظ الإعدادات."
                    : matches.Count == 0 && string.Equals(module, "Optical", StringComparison.OrdinalIgnoreCase)
                        ? "إعداد ترحيل تلف/كسر المعمل غير مكتمل. يجب إعداد حساب المخزون وتكلفة المبيعات في إعدادات المحاسبة."
                        : $"يجب وجود Posting Profile فعال واحد للموديول '{module}' والمستند '{documentType}'.";

                throw new ConflictException("accounting_posting_profile_invalid", message);
            }

            var lineSpec = new Specification<PostingProfileLine>().Where(x => x.PostingProfileId == matches[0].Id);
            configured = await profileLines.ListAsync(lineSpec, ct);
            cache[key] = configured;
        }

        var roleMatches = configured.Where(x => string.Equals(x.AccountRole, role, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (roleMatches.Length != 1)
            throw new ConflictException("accounting_posting_role_invalid", $"يجب تعريف حساب واحد فقط للدور '{role}'.");
        var account = await EnsurePostingAccountAsync(roleMatches[0].AccountId, ct);
        return account.Id;
    }


    private async Task TryProvisionOpticalProfileAsync(string documentType, CancellationToken ct)
    {
        if (!string.Equals(documentType, "OpticalBreakage", StringComparison.OrdinalIgnoreCase)) return;

        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct);
        if (accountingSettings?.InventoryAccountId is not Guid inventoryAccountId) return;

        // The current settings model has no dedicated production-damage account. Reuse the
        // configured COGS posting account as the default damage expense account, but store it
        // under a distinct PostingProfile role so it can be replaced later without code changes.
        var damageAccountId = await ResolveSalesRoleAccountIdAsync("COGS", ct);
        if (!damageAccountId.HasValue) return;

        var all = await profiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x => x.Module == "Optical" && x.DocumentType == "OpticalBreakage")
                .Tracking(), ct);
        var active = all.Where(x => x.IsActive).ToList();
        if (active.Count > 1)
            throw new ConflictException("optical_posting_profile_duplicate", "يوجد أكثر من ملف ترحيل فعال لكسر المعمل.");

        var profile = active.SingleOrDefault() ?? all.FirstOrDefault();
        if (profile is null)
        {
            profile = PostingProfile.Create(Guid.NewGuid(), "OPTICAL-BREAKAGE", "ترحيل كسر وتلف المعمل", "Optical", "OpticalBreakage", true);
            await profiles.AddAsync(profile, ct);
        }
        else if (!profile.IsActive)
        {
            profile.SetActive(true);
            profiles.Update(profile);
        }

        await UpsertProfileRoleAsync(profile.Id, "ProductionDamageExpense", damageAccountId.Value, ct);
        await UpsertProfileRoleAsync(profile.Id, "Inventory", inventoryAccountId, ct);
    }

    private async Task<Guid?> ResolveSalesRoleAccountIdAsync(string role, CancellationToken ct)
    {
        var salesProfiles = await profiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x => x.IsActive && x.Module == "Sales" && x.DocumentType == "SalesInvoice")
                .ApplyPaging(0, 2), ct);
        if (salesProfiles.Count != 1) return null;
        var rows = await profileLines.ListAsync(
            new Specification<PostingProfileLine>()
                .Where(x => x.PostingProfileId == salesProfiles[0].Id && x.AccountRole == role)
                .ApplyPaging(0, 2), ct);
        return rows.Count == 1 ? rows[0].AccountId : null;
    }

    private async Task TryProvisionPurchasingProfileAsync(string documentType, CancellationToken ct)
    {
        // First recover an existing inactive profile when it already contains the
        // minimum roles required for the document. This supports older databases
        // that contain configured profiles which were simply disabled.
        var existingProfiles = await profiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x => x.Module == "Purchasing" && x.DocumentType == documentType)
                .Tracking(),
            ct);

        if (existingProfiles.Count == 1 && !existingProfiles[0].IsActive)
        {
            var existingLines = await profileLines.ListAsync(
                new Specification<PostingProfileLine>()
                    .Where(x => x.PostingProfileId == existingProfiles[0].Id),
                ct);

            var hasGrni = existingLines.Count(x => x.AccountRole == "GoodsReceivedNotInvoiced") == 1;
            var hasInventory = existingLines.Count(x => x.AccountRole == "Inventory") == 1;
            var canActivate = string.Equals(documentType, "PurchaseReceipt", StringComparison.OrdinalIgnoreCase)
                ? hasGrni && hasInventory
                : hasGrni;

            if (canActivate)
            {
                existingProfiles[0].SetActive(true);
                profiles.Update(existingProfiles[0]);
                return;
            }
        }

        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct);
        if (accountingSettings is null || !accountingSettings.GrniAccountId.HasValue)
            return;

        if (string.Equals(documentType, "PurchaseReceipt", StringComparison.OrdinalIgnoreCase))
        {
            var inventoryAccountId = accountingSettings.InventoryAccountId
                ?? await ResolveExistingInventoryAccountAsync(ct);
            if (!inventoryAccountId.HasValue)
                return;

            var profile = await EnsurePurchasingProfileAsync(
                "PURCHASE-RECEIPT",
                "ترحيل استلام المشتريات",
                "PurchaseReceipt",
                ct);

            await UpsertProfileRoleAsync(profile.Id, "Inventory", inventoryAccountId.Value, ct);
            await UpsertProfileRoleAsync(profile.Id, "GoodsReceivedNotInvoiced", accountingSettings.GrniAccountId.Value, ct);
            return;
        }

        if (string.Equals(documentType, "PurchaseInvoice", StringComparison.OrdinalIgnoreCase))
        {
            var profile = await EnsurePurchasingProfileAsync(
                "PURCHASE-INVOICE",
                "ترحيل فاتورة المورد",
                "PurchaseInvoice",
                ct);

            await UpsertProfileRoleAsync(profile.Id, "GoodsReceivedNotInvoiced", accountingSettings.GrniAccountId.Value, ct);

            if (accountingSettings.PurchaseTaxAccountId.HasValue)
                await UpsertProfileRoleAsync(profile.Id, "PurchaseTax", accountingSettings.PurchaseTaxAccountId.Value, ct);

            if (accountingSettings.PurchasePriceVarianceAccountId.HasValue)
                await UpsertProfileRoleAsync(profile.Id, "PurchasePriceVariance", accountingSettings.PurchasePriceVarianceAccountId.Value, ct);
        }
    }

    private async Task<Guid?> ResolveExistingInventoryAccountAsync(CancellationToken ct)
    {
        var salesProfiles = await profiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x => x.IsActive && x.Module == "Sales" && x.DocumentType == "SalesInvoice")
                .ApplyPaging(0, 2),
            ct);

        if (salesProfiles.Count != 1)
            return null;

        var inventoryLines = await profileLines.ListAsync(
            new Specification<PostingProfileLine>()
                .Where(x => x.PostingProfileId == salesProfiles[0].Id && x.AccountRole == "Inventory")
                .ApplyPaging(0, 2),
            ct);

        return inventoryLines.Count == 1 ? inventoryLines[0].AccountId : null;
    }

    private async Task<PostingProfile> EnsurePurchasingProfileAsync(
        string code,
        string name,
        string documentType,
        CancellationToken ct)
    {
        var all = await profiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x => x.Module == "Purchasing" && x.DocumentType == documentType)
                .Tracking(),
            ct);

        var active = all.Where(x => x.IsActive).ToList();
        if (active.Count > 1)
            throw new ConflictException("purchasing_posting_profile_duplicate", $"يوجد أكثر من ملف ترحيل فعال للمستند {documentType}.");

        var profile = active.SingleOrDefault() ?? all.FirstOrDefault();
        if (profile is null)
        {
            profile = PostingProfile.Create(Guid.NewGuid(), code, name, "Purchasing", documentType, true);
            await profiles.AddAsync(profile, ct);
        }
        else if (!profile.IsActive)
        {
            profile.SetActive(true);
            profiles.Update(profile);
        }

        return profile;
    }

    private async Task UpsertProfileRoleAsync(Guid profileId, string role, Guid accountId, CancellationToken ct)
    {
        var existing = await profileLines.ListAsync(
            new Specification<PostingProfileLine>()
                .Where(x => x.PostingProfileId == profileId && x.AccountRole == role)
                .Tracking(),
            ct);

        var line = existing.FirstOrDefault();
        if (line is null)
        {
            await profileLines.AddAsync(
                PostingProfileLine.Create(Guid.NewGuid(), profileId, role, accountId, true),
                ct);
        }
        else
        {
            line.Update(role, accountId, true);
            profileLines.Update(line);
        }

        if (existing.Count > 1)
            profileLines.DeleteRange(existing.Skip(1));
    }

    private async Task<(Supplier Supplier, Account Account)> ResolveSupplierAccountAsync(Guid supplierId, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(supplierId, ct) ?? throw new NotFoundException(nameof(Supplier), supplierId);
        if (!supplier.IsActive) throw new ConflictException("accounting_supplier_inactive", "المورد غير فعال.");
        if (supplier.AccountId == Guid.Empty) throw new ConflictException("accounting_supplier_account_missing", "لا يوجد حساب محاسبي صالح للمورد.");
        var account = await EnsurePostingAccountAsync(supplier.AccountId, ct);
        if (account.AccountType != AccountType.Subledger)
            throw new ConflictException("accounting_supplier_account_not_subledger", "حساب المورد يجب أن يكون حساب Subledger قابلًا للترحيل.");
        return (supplier, account);
    }

    private async Task<Account> EnsurePostingAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(accountId, ct) ?? throw new NotFoundException(nameof(Account), accountId);
        if (!account.CanReceivePosting()) throw new ConflictException("accounting_account_not_postable", $"الحساب '{account.Code}' غير صالح للترحيل.");
        return account;
    }
}
