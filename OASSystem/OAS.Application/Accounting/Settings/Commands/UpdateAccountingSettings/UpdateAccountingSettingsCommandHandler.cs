using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using DomainRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;

namespace OAS.Application.Accounting.Settings.Commands.UpdateAccountingSettings;

public sealed class UpdateAccountingSettingsCommandHandler(
    IRepository<AccountingSettings, Guid> repository,
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<JournalEntry, Guid> journals,
    IReadRepository<PostingProfile, Guid> postingProfiles,
    IReadRepository<PostingProfileLine, Guid> postingProfileLines)
    : IRequestHandler<UpdateAccountingSettingsCommand>
{
    private const string SalesModule = "Sales";
    private const string SalesInvoiceDocumentType = "SalesInvoice";
    private const string SalesRevenueRole = "SalesRevenue";
    private const string TaxPayableRole = "TaxPayable";
    private const string InventoryRole = "Inventory";
    private const string CogsRole = "COGS";
    private const string PurchasingModule = "Purchasing";
    private const string PurchaseReceiptDocumentType = "PurchaseReceipt";
    private const string PurchaseInvoiceDocumentType = "PurchaseInvoice";
    private const string GrniRole = "GoodsReceivedNotInvoiced";
    private const string PurchaseTaxRole = "PurchaseTax";
    private const string PurchasePriceVarianceRole = "PurchasePriceVariance";
    private const string AccountingModule = "Accounting";
    private const string CustomerAdvanceApplicationDocumentType = "CustomerAdvanceApplication";
    private const string CustomerAdvancesRole = "CustomerAdvances";

    public async Task Handle(
        UpdateAccountingSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Request;

        var currency = await currencies.GetByIdAsync(data.BaseCurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), data.BaseCurrencyId);

        if (!currency.IsActive)
        {
            throw new ConflictException(
                "base_currency_inactive",
                "Base currency must be active.");
        }

        await ValidateAssetControlAsync(data.EmployeeParentAccountId, "employee_parent_account_invalid", cancellationToken);
        await ValidateAssetControlAsync(data.CashParentAccountId, "cash_parent_account_invalid", cancellationToken);
        await ValidateAssetControlAsync(data.BankParentAccountId, "bank_parent_account_invalid", cancellationToken);
        await ValidatePostingAsync(data.ExchangeGainAccountId, "exchange_gain_account_invalid", cancellationToken);
        await ValidatePostingAsync(data.ExchangeLossAccountId, "exchange_loss_account_invalid", cancellationToken);

        await ValidateRetainedEarningsAsync(data.RetainedEarningsAccountId, cancellationToken);

        await RejectPostingProfileRoleChangesAsync(data, cancellationToken);
        await ValidateInventoryAsync(data.InventoryAccountId, cancellationToken);
        ValidatePurchasingSettingsCompleteness(data);
        await ValidateGrniAsync(data.GrniAccountId, cancellationToken);
        await ValidatePurchaseTaxAsync(data.PurchaseTaxAccountId, cancellationToken);
        await ValidatePurchasePriceVarianceAsync(data.PurchasePriceVarianceAccountId, cancellationToken);

        var entity = await repository.GetForUpdateAsync(
            AccountingSettings.SingletonId,
            cancellationToken);

        if (entity is null)
        {
            entity = AccountingSettings.Create(
                currency.Id,
                data.EmployeeParentAccountId,
                data.CashParentAccountId,
                data.BankParentAccountId,
                data.ExchangeGainAccountId,
                data.ExchangeLossAccountId,
                (DomainRateType)(byte)data.DefaultExchangeRateType,
                data.RetainedEarningsAccountId,
                data.GrniAccountId,
                data.PurchaseTaxAccountId,
                data.PurchasePriceVarianceAccountId,
                data.InventoryAccountId);

            await repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(data.RowVersion))
                throw new ConcurrencyException("Accounting settings row version is required.");

            byte[] requestedRowVersion;
            try
            {
                requestedRowVersion = Convert.FromBase64String(data.RowVersion);
            }
            catch (FormatException ex)
            {
                throw new ConcurrencyException("Accounting settings row version is invalid.", ex);
            }

            if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
                throw new ConcurrencyException("Accounting settings were modified by another user.");

            if (entity.BaseCurrencyId != currency.Id &&
                await journals.CountAsync(
                    new Specification<JournalEntry>()
                        .Where(x =>
                            x.Status == JournalEntryStatus.Posted ||
                            x.Status == JournalEntryStatus.Reversed),
                    cancellationToken) > 0)
            {
                throw new ConflictException(
                    "base_currency_change_blocked",
                    "Base currency cannot be changed after posted journals exist.");
            }

            entity.Update(
                currency.Id,
                data.EmployeeParentAccountId,
                data.CashParentAccountId,
                data.BankParentAccountId,
                data.ExchangeGainAccountId,
                data.ExchangeLossAccountId,
                (DomainRateType)(byte)data.DefaultExchangeRateType,
                data.RetainedEarningsAccountId,
                data.GrniAccountId,
                data.PurchaseTaxAccountId,
                data.PurchasePriceVarianceAccountId,
                data.InventoryAccountId);

            repository.Update(entity);
        }

        // Posting profiles have their own explicit editor and activation workflow.
        // Never silently modify or reactivate them while updating general settings.
    }

    private async Task ValidateAssetControlAsync(
        Guid? id,
        string code,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.IsActive ||
            !account.IsControlAccount ||
            account.AccountType != AccountType.Control ||
            account.AccountClass != AccountClass.Asset ||
            account.NormalBalance != NormalBalance.Debit)
        {
            throw new ConflictException(
                code,
                "Parent account must be an active Asset/Debit control account.");
        }
    }

    private async Task ValidatePostingAsync(
        Guid? id,
        string code,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting())
        {
            throw new ConflictException(
                code,
                "Configured account must be active and posting-capable.");
        }
    }

    private async Task ValidateSalesRevenueAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting() ||
            account.AccountClass != AccountClass.Revenue ||
            account.NormalBalance != NormalBalance.Credit)
        {
            throw new ConflictException(
                "sales_revenue_account_invalid",
                "حساب إيرادات المبيعات يجب أن يكون حساب إيراد نشطًا، دائنًا، وقابلًا للترحيل.");
        }
    }

    private async Task ValidateTaxPayableAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting() ||
            account.AccountClass != AccountClass.Liability ||
            account.NormalBalance != NormalBalance.Credit)
        {
            throw new ConflictException(
                "tax_payable_account_invalid",
                "حساب الضرائب المستحقة يجب أن يكون حساب التزام نشطًا، دائنًا، وقابلًا للترحيل.");
        }
    }

    private async Task ValidateCustomerAdvancesAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting() ||
            account.AccountClass != AccountClass.Liability ||
            account.NormalBalance != NormalBalance.Credit)
        {
            throw new ConflictException(
                "customer_advances_account_invalid",
                "حساب دفعات مقدمة من العملاء يجب أن يكون حساب التزام نشطًا، دائنًا، وقابلًا للترحيل.");
        }
    }

    private async Task ValidateInventoryAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting() ||
            account.AccountClass != AccountClass.Asset ||
            account.NormalBalance != NormalBalance.Debit)
        {
            throw new ConflictException(
                "inventory_account_invalid",
                "حساب المخزون يجب أن يكون حساب أصل نشطًا، مدينًا، وقابلًا للترحيل.");
        }
    }

    private async Task ValidateCogsAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting() ||
            account.AccountClass != AccountClass.Expense ||
            account.NormalBalance != NormalBalance.Debit)
        {
            throw new ConflictException(
                "cogs_account_invalid",
                "حساب تكلفة البضاعة المباعة يجب أن يكون حساب مصروف نشطًا، مدينًا، وقابلًا للترحيل.");
        }
    }

    private static void ValidatePurchasingSettingsCompleteness(
        OAS.Contracts.Accounting.Settings.UpdateAccountingSettingsRequest data)
    {
        // Purchase receipt posting only requires Inventory + GRNI.
        // Purchase Tax and Purchase Price Variance are conditional invoice roles and
        // should not block receipt posting when they are not used yet.
        var hasReceiptAccount = data.InventoryAccountId.HasValue || data.GrniAccountId.HasValue;
        if (hasReceiptAccount && (!data.InventoryAccountId.HasValue || !data.GrniAccountId.HasValue))
        {
            throw new ConflictException(
                "purchasing_receipt_posting_accounts_incomplete",
                "لترحيل استلامات المشتريات يجب تحديد حساب المخزون وحساب بضاعة مستلمة غير مفوترة (GRNI).");
        }

        if ((data.PurchaseTaxAccountId.HasValue || data.PurchasePriceVarianceAccountId.HasValue) &&
            !data.GrniAccountId.HasValue)
        {
            throw new ConflictException(
                "purchasing_invoice_posting_accounts_incomplete",
                "قبل إعداد ضريبة المشتريات أو فرق سعر المشتريات يجب تحديد حساب بضاعة مستلمة غير مفوترة (GRNI).");
        }
    }

    private async Task ValidateGrniAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return;
        var account = await accounts.GetByIdAsync(id.Value, cancellationToken) ?? throw new NotFoundException(nameof(Account), id.Value);
        if (!account.CanReceivePosting() || account.AccountClass != AccountClass.Liability || account.NormalBalance != NormalBalance.Credit)
            throw new ConflictException("grni_account_invalid", "حساب بضاعة مستلمة غير مفوترة يجب أن يكون التزامًا نشطًا، دائنًا، وقابلًا للترحيل.");
    }

    private async Task ValidatePurchaseTaxAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return;
        var account = await accounts.GetByIdAsync(id.Value, cancellationToken) ?? throw new NotFoundException(nameof(Account), id.Value);
        if (!account.CanReceivePosting() || account.AccountClass != AccountClass.Asset || account.NormalBalance != NormalBalance.Debit)
            throw new ConflictException("purchase_tax_account_invalid", "حساب ضريبة المشتريات يجب أن يكون أصلًا نشطًا، مدينًا، وقابلًا للترحيل.");
    }

    private async Task ValidatePurchasePriceVarianceAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return;
        var account = await accounts.GetByIdAsync(id.Value, cancellationToken) ?? throw new NotFoundException(nameof(Account), id.Value);
        if (!account.CanReceivePosting() || account.AccountClass != AccountClass.Expense || account.NormalBalance != NormalBalance.Debit)
            throw new ConflictException("purchase_price_variance_account_invalid", "حساب فرق سعر المشتريات يجب أن يكون مصروفًا نشطًا، مدينًا، وقابلًا للترحيل.");
    }

    private async Task ValidateRetainedEarningsAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return;

        var account = await accounts.GetByIdAsync(id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), id.Value);

        if (!account.CanReceivePosting() ||
            account.AccountClass != AccountClass.Equity ||
            account.NormalBalance != NormalBalance.Credit)
        {
            throw new ConflictException(
                "retained_earnings_account_invalid",
                "حساب الأرباح المحتجزة يجب أن يكون حساب حقوق ملكية نشطًا، دائنًا، وقابلًا للترحيل.");
        }
    }

    // Settings read these roles from currently active posting profiles.
    // An API caller must not be allowed to mutate those roles by changing settings.
    private async Task RejectPostingProfileRoleChangesAsync(
        OAS.Contracts.Accounting.Settings.UpdateAccountingSettingsRequest data,
        CancellationToken cancellationToken)
    {
        var sales = await postingProfiles.ListAsync(new Specification<PostingProfile>()
            .Where(p => p.Module == SalesModule &&
                        p.DocumentType == SalesInvoiceDocumentType && p.IsActive), cancellationToken);
        var advances = await postingProfiles.ListAsync(new Specification<PostingProfile>()
            .Where(p => p.Module == AccountingModule &&
                        p.DocumentType == CustomerAdvanceApplicationDocumentType && p.IsActive), cancellationToken);
        if (sales.Count > 1 || advances.Count > 1)
            throw new ConflictException("posting_profile_duplicate", "يوجد أكثر من ملف ترحيل نشط لنوع المستند نفسه.");

        var salesRoles = sales.Count == 0 ? new Dictionary<string, Guid>() :
            await ReadPostingRolesAsync(sales[0].Id, cancellationToken);
        var advancesRoles = advances.Count == 0 ? new Dictionary<string, Guid>() :
            await ReadPostingRolesAsync(advances[0].Id, cancellationToken);

        static Guid? Role(Dictionary<string, Guid> values, string key) =>
            values.TryGetValue(key, out var id) ? id : null;
        // Null legacy request fields mean "not supplied" and never remove an active role.
        // Explicit IDs that differ from the active file must be edited in Posting Profiles.
        if ((data.SalesRevenueAccountId.HasValue && data.SalesRevenueAccountId != Role(salesRoles, SalesRevenueRole)) ||
            (data.TaxPayableAccountId.HasValue && data.TaxPayableAccountId != Role(salesRoles, TaxPayableRole)) ||
            (data.CogsAccountId.HasValue && data.CogsAccountId != Role(salesRoles, CogsRole)) ||
            (data.CustomerAdvancesAccountId.HasValue && data.CustomerAdvancesAccountId != Role(advancesRoles, CustomerAdvancesRole)))
        {
            throw new ConflictException("posting_roles_owned_by_profiles",
                "حسابات الترحيل تُعدل من صفحة ملفات الترحيل فقط. حدّث الصفحة بعد تعديل الملف النشط.");
        }
    }

    private async Task<Dictionary<string, Guid>> ReadPostingRolesAsync(Guid profileId, CancellationToken ct)
    {
        var lines = await postingProfileLines.ListAsync(new Specification<PostingProfileLine>()
            .Where(x => x.PostingProfileId == profileId), ct);
        if (lines.GroupBy(x => x.AccountRole).Any(g => g.Count() > 1))
            throw new ConflictException("posting_role_duplicate", "يوجد أكثر من حساب للدور المحاسبي نفسه.");
        return lines.ToDictionary(x => x.AccountRole, x => x.AccountId);
    }
}
