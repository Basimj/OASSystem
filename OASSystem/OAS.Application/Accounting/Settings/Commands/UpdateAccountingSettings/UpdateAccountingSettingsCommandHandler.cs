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
    IRepository<PostingProfile, Guid> postingProfiles,
    IRepository<PostingProfileLine, Guid> postingProfileLines)
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

        ValidateSalesInvoiceSettingsCompleteness(data);
        await ValidateSalesRevenueAsync(data.SalesRevenueAccountId, cancellationToken);
        await ValidateTaxPayableAsync(data.TaxPayableAccountId, cancellationToken);
        await ValidateCustomerAdvancesAsync(data.CustomerAdvancesAccountId, cancellationToken);
        await ValidateInventoryAsync(data.InventoryAccountId, cancellationToken);
        await ValidateCogsAsync(data.CogsAccountId, cancellationToken);
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

        await UpsertSalesInvoiceRolesAsync(
            data.SalesRevenueAccountId,
            data.TaxPayableAccountId,
            data.InventoryAccountId,
            data.CogsAccountId,
            cancellationToken);

        await UpsertCustomerAdvanceProfileAsync(
            data.CustomerAdvancesAccountId,
            cancellationToken);

        await UpsertPurchasingProfilesAsync(
            data.InventoryAccountId,
            data.GrniAccountId,
            data.PurchaseTaxAccountId,
            data.PurchasePriceVarianceAccountId,
            cancellationToken);
    }

    private static void ValidateSalesInvoiceSettingsCompleteness(
        OAS.Contracts.Accounting.Settings.UpdateAccountingSettingsRequest data)
    {
        var hasSalesSpecificAccount =
            data.SalesRevenueAccountId.HasValue ||
            data.TaxPayableAccountId.HasValue ||
            data.CogsAccountId.HasValue;

        var configured = new[]
        {
            data.SalesRevenueAccountId,
            data.TaxPayableAccountId,
            data.InventoryAccountId,
            data.CogsAccountId
        };

        if (hasSalesSpecificAccount && configured.Any(x => !x.HasValue))
        {
            throw new ConflictException(
                "sales_invoice_posting_accounts_incomplete",
                "عند إعداد ترحيل فواتير المبيعات يجب تحديد حساب إيرادات المبيعات والضرائب المستحقة والمخزون وتكلفة البضاعة المباعة جميعًا.");
        }
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

    private async Task UpsertSalesInvoiceRolesAsync(
        Guid? salesRevenueAccountId,
        Guid? taxPayableAccountId,
        Guid? inventoryAccountId,
        Guid? cogsAccountId,
        CancellationToken cancellationToken)
    {
        var profiles = await postingProfiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x =>
                    x.Module == SalesModule &&
                    x.DocumentType == SalesInvoiceDocumentType &&
                    x.IsActive)
                .Tracking(),
            cancellationToken);

        if (profiles.Count > 1)
        {
            throw new ConflictException(
                "sales_invoice_posting_profile_duplicate",
                "يوجد أكثر من ملف ترحيل فعال لفاتورة المبيعات. يجب إبقاء ملف واحد فعال فقط.");
        }

        var profile = profiles.SingleOrDefault();
        var hasAnyConfiguredAccount =
            salesRevenueAccountId.HasValue ||
            taxPayableAccountId.HasValue ||
            cogsAccountId.HasValue;

        if (profile is null)
        {
            if (hasAnyConfiguredAccount)
            {
                throw new ConflictException(
                    "sales_invoice_posting_profile_missing",
                    "لا يوجد ملف ترحيل فعال لفاتورة المبيعات. فعّل ملف Sales / SalesInvoice أولاً ثم احفظ حسابات ترحيل المبيعات.");
            }

            return;
        }

        await UpsertRoleAsync(profile.Id, SalesRevenueRole, salesRevenueAccountId, cancellationToken);
        await UpsertRoleAsync(profile.Id, TaxPayableRole, taxPayableAccountId, cancellationToken);
        await UpsertRoleAsync(profile.Id, InventoryRole, inventoryAccountId, cancellationToken);
        await UpsertRoleAsync(profile.Id, CogsRole, cogsAccountId, cancellationToken);
    }

    private async Task UpsertCustomerAdvanceProfileAsync(
        Guid? customerAdvancesAccountId,
        CancellationToken cancellationToken)
    {
        var allProfiles = await postingProfiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x =>
                    x.Module == AccountingModule &&
                    x.DocumentType == CustomerAdvanceApplicationDocumentType)
                .Tracking(),
            cancellationToken);

        var activeProfiles = allProfiles.Where(x => x.IsActive).ToList();
        if (activeProfiles.Count > 1)
        {
            throw new ConflictException(
                "customer_advance_posting_profile_duplicate",
                "يوجد أكثر من ملف ترحيل فعال لعربون العميل. يجب إبقاء ملف واحد فقط.");
        }

        var profile = activeProfiles.SingleOrDefault() ?? allProfiles.FirstOrDefault();

        if (!customerAdvancesAccountId.HasValue)
        {
            if (profile is null)
                return;

            await UpsertRoleAsync(profile.Id, CustomerAdvancesRole, null, cancellationToken);
            if (profile.IsActive)
            {
                profile.SetActive(false);
                postingProfiles.Update(profile);
            }
            return;
        }

        if (profile is null)
        {
            profile = PostingProfile.Create(
                Guid.NewGuid(),
                "ACCOUNTING-CUSTOMER-ADVANCE",
                "ترحيل دفعات مقدمة من العملاء",
                AccountingModule,
                CustomerAdvanceApplicationDocumentType,
                true);
            await postingProfiles.AddAsync(profile, cancellationToken);
        }
        else if (!profile.IsActive)
        {
            profile.SetActive(true);
            postingProfiles.Update(profile);
        }

        await UpsertRoleAsync(
            profile.Id,
            CustomerAdvancesRole,
            customerAdvancesAccountId,
            cancellationToken);
    }

    private async Task UpsertPurchasingProfilesAsync(
        Guid? inventoryAccountId,
        Guid? grniAccountId,
        Guid? purchaseTaxAccountId,
        Guid? purchasePriceVarianceAccountId,
        CancellationToken cancellationToken)
    {
        // Receipt posting is independent from invoice-only roles.
        if (inventoryAccountId.HasValue && grniAccountId.HasValue)
        {
            var receipt = await EnsurePurchasingProfileAsync(
                "PURCHASE-RECEIPT",
                "ترحيل استلام المشتريات",
                PurchaseReceiptDocumentType,
                cancellationToken);

            await UpsertRoleAsync(receipt.Id, InventoryRole, inventoryAccountId, cancellationToken);
            await UpsertRoleAsync(receipt.Id, GrniRole, grniAccountId, cancellationToken);
        }

        // Invoice posting always needs GRNI. Tax/PPV roles are conditional and are
        // only required by the posting service when the corresponding amount is non-zero.
        if (grniAccountId.HasValue)
        {
            var invoice = await EnsurePurchasingProfileAsync(
                "PURCHASE-INVOICE",
                "ترحيل فاتورة المورد",
                PurchaseInvoiceDocumentType,
                cancellationToken);

            await UpsertRoleAsync(invoice.Id, GrniRole, grniAccountId, cancellationToken);
            await UpsertRoleAsync(invoice.Id, PurchaseTaxRole, purchaseTaxAccountId, cancellationToken);
            await UpsertRoleAsync(invoice.Id, PurchasePriceVarianceRole, purchasePriceVarianceAccountId, cancellationToken);
        }
    }

    private async Task<PostingProfile> EnsurePurchasingProfileAsync(
        string code, string name, string documentType, CancellationToken cancellationToken)
    {
        var profiles = await postingProfiles.ListAsync(
            new Specification<PostingProfile>().Where(x => x.Module == PurchasingModule && x.DocumentType == documentType).Tracking(),
            cancellationToken);

        var active = profiles.Where(x => x.IsActive).ToList();
        if (active.Count > 1)
            throw new ConflictException("purchasing_posting_profile_duplicate", $"يوجد أكثر من ملف ترحيل فعال للمستند {documentType}.");

        var profile = active.SingleOrDefault() ?? profiles.FirstOrDefault();
        if (profile is null)
        {
            profile = PostingProfile.Create(Guid.NewGuid(), code, name, PurchasingModule, documentType, true);
            await postingProfiles.AddAsync(profile, cancellationToken);
        }
        else if (!profile.IsActive)
        {
            profile.SetActive(true);
            postingProfiles.Update(profile);
        }

        return profile;
    }

    private async Task UpsertRoleAsync(
        Guid postingProfileId,
        string role,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        var existingLines = await postingProfileLines.ListAsync(
            new Specification<PostingProfileLine>()
                .Where(x =>
                    x.PostingProfileId == postingProfileId &&
                    x.AccountRole == role)
                .Tracking(),
            cancellationToken);

        if (!accountId.HasValue)
        {
            if (existingLines.Count > 0)
                postingProfileLines.DeleteRange(existingLines);

            return;
        }

        var line = existingLines.FirstOrDefault();
        if (line is null)
        {
            line = PostingProfileLine.Create(
                Guid.NewGuid(),
                postingProfileId,
                role,
                accountId.Value,
                true);

            await postingProfileLines.AddAsync(line, cancellationToken);
            return;
        }

        line.Update(role, accountId.Value, true);
        postingProfileLines.Update(line);

        if (existingLines.Count > 1)
            postingProfileLines.DeleteRange(existingLines.Skip(1));
    }
}
