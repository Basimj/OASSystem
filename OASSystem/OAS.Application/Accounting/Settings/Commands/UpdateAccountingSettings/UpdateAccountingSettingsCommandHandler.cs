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

        ValidateSalesInvoiceSettingsCompleteness(data);
        await ValidateSalesRevenueAsync(data.SalesRevenueAccountId, cancellationToken);
        await ValidateTaxPayableAsync(data.TaxPayableAccountId, cancellationToken);
        await ValidateInventoryAsync(data.InventoryAccountId, cancellationToken);
        await ValidateCogsAsync(data.CogsAccountId, cancellationToken);

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
                (DomainRateType)(byte)data.DefaultExchangeRateType);

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
                (DomainRateType)(byte)data.DefaultExchangeRateType);

            repository.Update(entity);
        }

        await UpsertSalesInvoiceRolesAsync(
            data.SalesRevenueAccountId,
            data.TaxPayableAccountId,
            data.InventoryAccountId,
            data.CogsAccountId,
            cancellationToken);
    }

    private static void ValidateSalesInvoiceSettingsCompleteness(
        OAS.Contracts.Accounting.Settings.UpdateAccountingSettingsRequest data)
    {
        var configured = new[]
        {
            data.SalesRevenueAccountId,
            data.TaxPayableAccountId,
            data.InventoryAccountId,
            data.CogsAccountId
        };

        if (configured.Any(x => x.HasValue) && configured.Any(x => !x.HasValue))
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
            inventoryAccountId.HasValue ||
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
