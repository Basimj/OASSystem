using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Posting;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting.Queries;

/// <summary>Checks the same profile/period/account data as the posting service, without mutating it.</summary>
public sealed record GetPurchasingPostingReadinessQuery(
    DateOnly PostingDate,
    Guid? SupplierId = null,
    bool RequirePurchaseTax = false,
    bool RequirePriceVariance = false,
    bool RequireForeignCurrency = false)
    : IQuery<PurchasingPostingReadinessDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [AccountingPermissions.PostingProfiles.View, AccountingPermissions.Settings.View,
     AccountingPermissions.Suppliers.View];
}

public sealed class GetPurchasingPostingReadinessQueryHandler(
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<FiscalPeriod, Guid> periods,
    IReadRepository<PostingProfile, Guid> profiles,
    IReadRepository<PostingProfileLine, Guid> profileLines,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<Supplier, Guid> suppliers)
    : IRequestHandler<GetPurchasingPostingReadinessQuery, PurchasingPostingReadinessDto>
{
    public async Task<PurchasingPostingReadinessDto> Handle(
        GetPurchasingPostingReadinessQuery request, CancellationToken ct)
    {
        var issues = new List<PurchasingPostingReadinessIssueDto>();
        void Block(string code, string message, string? doc = null, string? role = null) =>
            issues.Add(new(code, "Block", message, doc, role));
        void Warn(string code, string message, string? doc = null, string? role = null) =>
            issues.Add(new(code, "Warning", message, doc, role));

        var periodMatches = await periods.ListAsync(new Specification<FiscalPeriod>()
            .Where(x => x.StartDate <= request.PostingDate && x.EndDate >= request.PostingDate)
            .ApplyPaging(0, 2), ct);
        if (periodMatches.Count == 0)
            Block("fiscal_period_not_found", "لا توجد فترة مالية تغطي تاريخ الترحيل.");
        else if (periodMatches.Count > 1)
            Block("fiscal_period_overlap", "توجد فترتان ماليتان أو أكثر تغطيان التاريخ.");
        else if (!periodMatches[0].CanPostAccounting())
            Block("fiscal_period_closed", "الفترة المالية مغلقة أو الترحيل المحاسبي موقوف.");

        var config = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct);
        string? baseCurrencyCode = null;
        if (config is null)
            Block("accounting_settings_required", "احفظ إعدادات المحاسبة والعملة الأساسية أولًا.");
        else
        {
            var baseCurrency = await currencies.GetByIdAsync(config.BaseCurrencyId, ct);
            if (baseCurrency is null || !baseCurrency.IsActive)
                Block("base_currency_invalid", "العملة الأساسية غير موجودة أو غير فعالة.");
            else
                baseCurrencyCode = baseCurrency.Code;
        }

        async Task CheckAccountAsync(Guid? id, string code, string label, bool required, string? document = null,
            string? role = null)
        {
            if (!id.HasValue || id.Value == Guid.Empty)
            {
                if (required) Block(code + "_missing", $"لم يتم تحديد حساب {label}.", document, role);
                else Warn(code + "_missing", $"حساب {label} غير مضبوط (يصبح مطلوبًا إذا استُخدم في المستند).", document, role);
                return;
            }
            var account = await accounts.GetByIdAsync(id.Value, ct);
            if (account is null || !account.CanReceivePosting() || account.AccountType == AccountType.Subledger)
            {
                Block(code + "_invalid", $"حساب {label} غير صالح للترحيل الآلي.", document, role);
                return;
            }
            if (role is not null)
            {
                var roleProblem = PurchasingPostingAccountRolePolicy.Validate(role, account);
                if (roleProblem is not null)
                    Block(roleProblem.Value.Code, roleProblem.Value.Message, document, role);
            }
        }

        if (config is not null)
        {
            await CheckAccountAsync(config.InventoryAccountId, "inventory_account", "المخزون", true, role: "Inventory");
            await CheckAccountAsync(config.GrniAccountId, "grni_account", "GRNI", true, role: "GoodsReceivedNotInvoiced");
            await CheckAccountAsync(config.PurchaseTaxAccountId, "purchase_tax_account", "ضريبة المشتريات", request.RequirePurchaseTax);
            await CheckAccountAsync(config.PurchasePriceVarianceAccountId, "ppv_account", "فروق سعر الشراء", request.RequirePriceVariance);
            await CheckAccountAsync(config.ExchangeGainAccountId, "exchange_gain_account", "أرباح فروق العملة", request.RequireForeignCurrency);
            await CheckAccountAsync(config.ExchangeLossAccountId, "exchange_loss_account", "خسائر فروق العملة", request.RequireForeignCurrency);
        }

        // Return transactions deliberately use the receipt/invoice roles; a
        // third PurchaseReturn profile is not required by the existing adapter.
        foreach (var documentType in new[] { "PurchaseReceipt", "PurchaseInvoice" })
        {
            var matches = await profiles.ListAsync(new Specification<PostingProfile>()
                .Where(x => x.Module == "Purchasing" && x.DocumentType == documentType && x.IsActive)
                .ApplyPaging(0, 2), ct);
            if (matches.Count != 1)
            {
                Block(matches.Count == 0 ? "purchasing_profile_missing" : "purchasing_profile_duplicate",
                    $"يجب وجود ملف ترحيل فعال واحد للمستند {documentType}، والتهيئة لا تتم أثناء الترحيل.", documentType);
                continue;
            }

            var configured = await profileLines.ListAsync(new Specification<PostingProfileLine>()
                .Where(x => x.PostingProfileId == matches[0].Id), ct);
            var expected = documentType == "PurchaseReceipt"
                ? new[] { (Role: "Inventory", Mandatory: true), (Role: "GoodsReceivedNotInvoiced", Mandatory: true) }
                : new[] { (Role: "GoodsReceivedNotInvoiced", Mandatory: true),
                          (Role: "PurchaseTax", Mandatory: request.RequirePurchaseTax),
                          (Role: "PurchasePriceVariance", Mandatory: request.RequirePriceVariance) };
            foreach (var expectedRole in expected)
            {
                var found = configured.Where(x => string.Equals(x.AccountRole, expectedRole.Role,
                    StringComparison.OrdinalIgnoreCase)).ToList();
                if (found.Count > 1)
                {
                    Block("purchasing_role_duplicate", $"الدور {expectedRole.Role} مُعرّف أكثر من مرة.", documentType, expectedRole.Role);
                    continue;
                }
                if (found.Count == 0)
                {
                    if (expectedRole.Mandatory)
                        Block("purchasing_role_missing", $"الدور {expectedRole.Role} غير مضبوط.", documentType, expectedRole.Role);
                    else
                        Warn("purchasing_role_optional", $"الدور {expectedRole.Role} غير مضبوط؛ سيُطلب عند وجود ضريبة أو فرق سعر.", documentType, expectedRole.Role);
                    continue;
                }
                await CheckAccountAsync(found[0].AccountId, "profile_role_" + expectedRole.Role,
                    expectedRole.Role, true, documentType, expectedRole.Role);
                var configuredSettingsAccount = expectedRole.Role switch
                {
                    "Inventory" => config?.InventoryAccountId,
                    "GoodsReceivedNotInvoiced" => config?.GrniAccountId,
                    "PurchaseTax" => config?.PurchaseTaxAccountId,
                    "PurchasePriceVariance" => config?.PurchasePriceVarianceAccountId,
                    _ => null
                };
                if (configuredSettingsAccount.HasValue && configuredSettingsAccount.Value != found[0].AccountId)
                    Warn("purchasing_role_settings_drift",
                        $"حساب الدور {expectedRole.Role} في ملف الترحيل يختلف عن الحساب المحدد في إعدادات المحاسبة. راجع الاختيار المقصود.",
                        documentType, expectedRole.Role);
            }
        }

        if (request.SupplierId.HasValue)
        {
            var supplier = await suppliers.GetByIdAsync(request.SupplierId.Value, ct);
            if (supplier is null || !supplier.IsActive)
                Block("supplier_not_active", "المورد المحدد غير موجود أو غير فعال.");
            else
            {
                var account = supplier.AccountId == Guid.Empty
                    ? null : await accounts.GetByIdAsync(supplier.AccountId, ct);
                if (account is null)
                    Block("supplier_account_missing", "لا يوجد حساب ذمم مرتبط بهذا المورد.");
                else
                {
                    var parent = account.ParentAccountId.HasValue
                        ? await accounts.GetByIdAsync(account.ParentAccountId.Value, ct) : null;
                    var defect = SupplierSubledgerPostingPolicy.Validate(account, parent);
                    if (defect is not null)
                        Block(defect.Value.Code, defect.Value.Message);
                }
            }
        }
        else
            Warn("supplier_not_selected", "النتيجة عامة. اختر موردًا من مركز المورد للتحقق من حساب ذمته أيضًا.");

        return new(request.PostingDate, request.SupplierId, baseCurrencyCode,
            issues.All(x => x.Severity != "Block"), issues);
    }
}
