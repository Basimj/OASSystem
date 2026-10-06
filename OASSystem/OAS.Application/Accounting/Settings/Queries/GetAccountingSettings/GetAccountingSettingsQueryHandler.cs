using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Settings;
using OAS.Domain.Accounting.Entities;
using ContractRateType = OAS.Contracts.Accounting.Enums.ExchangeRateType;

namespace OAS.Application.Accounting.Settings.Queries.GetAccountingSettings;

public sealed class GetAccountingSettingsQueryHandler(
    IReadRepository<AccountingSettings, Guid> repository,
    IReadRepository<Currency, Guid> currencies,
    IReadRepository<PostingProfile, Guid> postingProfiles,
    IReadRepository<PostingProfileLine, Guid> postingProfileLines)
    : IRequestHandler<GetAccountingSettingsQuery, AccountingSettingsDto?>
{
    private const string SalesModule = "Sales";
    private const string SalesInvoiceDocumentType = "SalesInvoice";
    private const string SalesRevenueRole = "SalesRevenue";
    private const string TaxPayableRole = "TaxPayable";
    private const string InventoryRole = "Inventory";
    private const string CogsRole = "COGS";

    public async Task<AccountingSettingsDto?> Handle(
        GetAccountingSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(
            AccountingSettings.SingletonId,
            cancellationToken);

        if (entity is null)
            return null;

        var currency = await currencies.GetByIdAsync(
            entity.BaseCurrencyId,
            cancellationToken);

        var roles = await ResolveSalesInvoiceRoleAccountsAsync(cancellationToken);

        return new AccountingSettingsDto(
            entity.Id,
            entity.BaseCurrencyId,
            currency?.Code ?? string.Empty,
            entity.EmployeeParentAccountId,
            entity.CashParentAccountId,
            entity.BankParentAccountId,
            entity.ExchangeGainAccountId,
            entity.ExchangeLossAccountId,
            roles.SalesRevenueAccountId,
            roles.TaxPayableAccountId,
            entity.InventoryAccountId ?? roles.InventoryAccountId,
            roles.CogsAccountId,
            entity.GrniAccountId,
            entity.PurchaseTaxAccountId,
            entity.PurchasePriceVarianceAccountId,
            (ContractRateType)(byte)entity.DefaultExchangeRateType,
            Convert.ToBase64String(entity.RowVersion),
            entity.RetainedEarningsAccountId);
    }

    private async Task<SalesInvoiceRoleAccounts> ResolveSalesInvoiceRoleAccountsAsync(
        CancellationToken cancellationToken)
    {
        var profiles = await postingProfiles.ListAsync(
            new Specification<PostingProfile>()
                .Where(x =>
                    x.Module == SalesModule &&
                    x.DocumentType == SalesInvoiceDocumentType &&
                    x.IsActive),
            cancellationToken);

        if (profiles.Count > 1)
        {
            throw new ConflictException(
                "sales_invoice_posting_profile_duplicate",
                "يوجد أكثر من ملف ترحيل فعال لفاتورة المبيعات.");
        }

        var profile = profiles.SingleOrDefault();
        if (profile is null)
            return SalesInvoiceRoleAccounts.Empty;

        var lines = await postingProfileLines.ListAsync(
            new Specification<PostingProfileLine>()
                .Where(x =>
                    x.PostingProfileId == profile.Id &&
                    (x.AccountRole == SalesRevenueRole ||
                     x.AccountRole == TaxPayableRole ||
                     x.AccountRole == InventoryRole ||
                     x.AccountRole == CogsRole)),
            cancellationToken);

        foreach (var group in lines.GroupBy(x => x.AccountRole))
        {
            if (group.Count() > 1)
            {
                throw new ConflictException(
                    "sales_invoice_posting_role_duplicate",
                    $"يوجد أكثر من حساب معرف للدور '{group.Key}' في ملف ترحيل فاتورة المبيعات.");
            }
        }

        Guid? Find(string role) =>
            lines.FirstOrDefault(x => x.AccountRole == role)?.AccountId;

        return new SalesInvoiceRoleAccounts(
            Find(SalesRevenueRole),
            Find(TaxPayableRole),
            Find(InventoryRole),
            Find(CogsRole));
    }

    private sealed record SalesInvoiceRoleAccounts(
        Guid? SalesRevenueAccountId,
        Guid? TaxPayableAccountId,
        Guid? InventoryAccountId,
        Guid? CogsAccountId)
    {
        public static readonly SalesInvoiceRoleAccounts Empty =
            new(null, null, null, null);
    }
}
