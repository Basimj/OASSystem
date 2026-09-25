using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Customers;
using OAS.Contracts.Accounting.Suppliers;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

using ContractContactMethod = OAS.Contracts.Accounting.Enums.ContactMethod;
using ContractGender = OAS.Contracts.Accounting.Enums.Gender;
using ContractPartyEntityType = OAS.Contracts.Accounting.Enums.PartyEntityType;
using ContractSupplierScope = OAS.Contracts.Accounting.Enums.SupplierScope;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class AccountingPartyQueryService(OasDbContext db) : IAccountingPartyQueryService
{
    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(
        PageRequest request,
        string? filter = null,
        CancellationToken ct = default)
    {
        var p = request.Normalize();

        IQueryable<Customer> query = db.Set<Customer>().AsNoTracking();
        query = ApplyCustomerFilter(query, filter);

        var search = p.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var accounts = db.Set<Account>().AsNoTracking();

            query = query.Where(x =>
                x.CustomerCode.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)) ||
                (x.TradeName != null && x.TradeName.Contains(search)) ||
                (x.NationalId != null && x.NationalId.Contains(search)) ||
                (x.TaxNumber != null && x.TaxNumber.Contains(search)) ||
                (x.CommercialRegistrationNo != null && x.CommercialRegistrationNo.Contains(search)) ||
                (x.ContactInfo.Phone != null && x.ContactInfo.Phone.Contains(search)) ||
                (x.ContactInfo.Mobile != null && x.ContactInfo.Mobile.Contains(search)) ||
                (x.ContactInfo.WhatsAppNumber != null && x.ContactInfo.WhatsAppNumber.Contains(search)) ||
                (x.ContactInfo.Email != null && x.ContactInfo.Email.Contains(search)) ||
                (x.ContactInfo.Address.City != null && x.ContactInfo.Address.City.Contains(search)) ||
                accounts.Any(a => a.Id == x.AccountId && a.Code.Contains(search)));
        }

        var total = await query.LongCountAsync(ct);

        query = OrderCustomers(query, p.SortBy, p.SortDirection);

        var customers = await query
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync(ct);

        var items = await MapCustomersAsync(customers, ct);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            PageNumber = p.PageNumber,
            PageSize = p.PageSize,
            TotalCount = total
        };
    }

    public async Task<CustomerDto?> GetCustomerAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var customer = await db.Set<Customer>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (customer is null)
        {
            return null;
        }

        var account = await db.Set<Account>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customer.AccountId, ct);

        if (account is null)
        {
            throw new InvalidOperationException(
                $"Customer '{customer.Id}' references missing account '{customer.AccountId}'.");
        }

        Account? parent = null;
        if (account.ParentAccountId.HasValue)
        {
            parent = await db.Set<Account>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == account.ParentAccountId.Value, ct);
        }

        return MapCustomer(customer, account, parent);
    }

    public async Task<IReadOnlyList<CustomerLookupDto>> LookupCustomersAsync(
        string? search,
        int take,
        CancellationToken ct = default)
    {
        var customers = db.Set<Customer>()
            .AsNoTracking()
            .Where(x => x.IsActive);

        var accounts = db.Set<Account>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();

            customers = customers.Where(x =>
                x.CustomerCode.Contains(s) ||
                x.NameAr.Contains(s) ||
                accounts.Any(a => a.Id == x.AccountId && a.Code.Contains(s)));
        }

        var rows = await (
            from c in customers
            join a in accounts on c.AccountId equals a.Id
            orderby c.CustomerCode
            select new
            {
                c.Id,
                c.CustomerCode,
                AccountCode = a.Code,
                c.NameAr,
                c.IsActive
            })
            .Take(take)
            .ToListAsync(ct);

        return rows
            .Select(x => new CustomerLookupDto(
                x.Id,
                x.CustomerCode,
                x.AccountCode,
                x.NameAr,
                x.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<CustomerAccountParentDto>> GetCustomerParentsAsync(
        string? search,
        int take,
        CancellationToken ct = default)
    {
        var query = db.Set<Account>()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.AccountClass == AccountClass.Asset &&
                x.AccountType == AccountType.Control &&
                x.IsControlAccount &&
                x.NormalBalance == NormalBalance.Debit);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                x.Code.Contains(s) ||
                x.NameAr.Contains(s) ||
                (x.NameEn != null && x.NameEn.Contains(s)));
        }

        return await query
            .OrderBy(x => x.Code)
            .Take(take)
            .Select(x => new CustomerAccountParentDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.Level))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(
        PageRequest request,
        string? filter = null,
        CancellationToken ct = default)
    {
        var p = request.Normalize();

        IQueryable<Supplier> query = db.Set<Supplier>().AsNoTracking();
        query = ApplySupplierFilter(query, filter);

        var search = p.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var accounts = db.Set<Account>().AsNoTracking();

            query = query.Where(x =>
                x.SupplierCode.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)) ||
                (x.TradeName != null && x.TradeName.Contains(search)) ||
                (x.NationalId != null && x.NationalId.Contains(search)) ||
                (x.TaxNumber != null && x.TaxNumber.Contains(search)) ||
                (x.CommercialRegistrationNo != null && x.CommercialRegistrationNo.Contains(search)) ||
                (x.ContactInfo.Phone != null && x.ContactInfo.Phone.Contains(search)) ||
                (x.ContactInfo.Mobile != null && x.ContactInfo.Mobile.Contains(search)) ||
                (x.ContactInfo.Email != null && x.ContactInfo.Email.Contains(search)) ||
                (x.ContactInfo.Address.City != null && x.ContactInfo.Address.City.Contains(search)) ||
                (x.ContactInfo.Address.Country != null && x.ContactInfo.Address.Country.Contains(search)) ||
                accounts.Any(a => a.Id == x.AccountId && a.Code.Contains(search)));
        }

        var total = await query.LongCountAsync(ct);

        query = OrderSuppliers(query, p.SortBy, p.SortDirection);

        var suppliers = await query
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync(ct);

        var items = await MapSuppliersAsync(suppliers, ct);

        return new PagedResult<SupplierDto>
        {
            Items = items,
            PageNumber = p.PageNumber,
            PageSize = p.PageSize,
            TotalCount = total
        };
    }

    public async Task<SupplierDto?> GetSupplierAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var supplier = await db.Set<Supplier>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (supplier is null)
        {
            return null;
        }

        var account = await db.Set<Account>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == supplier.AccountId, ct);

        if (account is null)
        {
            throw new InvalidOperationException(
                $"Supplier '{supplier.Id}' references missing account '{supplier.AccountId}'.");
        }

        Account? parent = null;
        if (account.ParentAccountId.HasValue)
        {
            parent = await db.Set<Account>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == account.ParentAccountId.Value, ct);
        }

        return MapSupplier(supplier, account, parent);
    }

    public async Task<IReadOnlyList<SupplierLookupDto>> LookupSuppliersAsync(
        string? search,
        int take,
        CancellationToken ct = default)
    {
        var suppliers = db.Set<Supplier>()
            .AsNoTracking()
            .Where(x => x.IsActive);

        var accounts = db.Set<Account>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();

            suppliers = suppliers.Where(x =>
                x.SupplierCode.Contains(s) ||
                x.NameAr.Contains(s) ||
                accounts.Any(a => a.Id == x.AccountId && a.Code.Contains(s)));
        }

        var rows = await (
            from s in suppliers
            join a in accounts on s.AccountId equals a.Id
            orderby s.SupplierCode
            select new
            {
                s.Id,
                s.SupplierCode,
                AccountCode = a.Code,
                s.NameAr,
                s.IsActive
            })
            .Take(take)
            .ToListAsync(ct);

        return rows
            .Select(x => new SupplierLookupDto(
                x.Id,
                x.SupplierCode,
                x.AccountCode,
                x.NameAr,
                x.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<SupplierAccountParentDto>> GetSupplierParentsAsync(
        string? search,
        int take,
        CancellationToken ct = default)
    {
        var query = db.Set<Account>()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.AccountClass == AccountClass.Liability &&
                x.AccountType == AccountType.Control &&
                x.IsControlAccount &&
                x.NormalBalance == NormalBalance.Credit);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                x.Code.Contains(s) ||
                x.NameAr.Contains(s) ||
                (x.NameEn != null && x.NameEn.Contains(s)));
        }

        return await query
            .OrderBy(x => x.Code)
            .Take(take)
            .Select(x => new SupplierAccountParentDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.Level))
            .ToListAsync(ct);
    }

    private static IQueryable<Customer> ApplyCustomerFilter(
        IQueryable<Customer> query,
        string? filter)
    {
        return filter?.Trim().ToLowerInvariant() switch
        {
            "active" => query.Where(x => x.IsActive),
            "inactive" => query.Where(x => !x.IsActive),
            "individual" => query.Where(x => x.EntityType == PartyEntityType.Individual),
            "organization" => query.Where(x => x.EntityType == PartyEntityType.Organization),
            "credit" => query.Where(x => x.IsCreditAllowed),
            "cash" => query.Where(x => !x.IsCreditAllowed),
            _ => query
        };
    }

    private static IQueryable<Supplier> ApplySupplierFilter(
        IQueryable<Supplier> query,
        string? filter)
    {
        return filter?.Trim().ToLowerInvariant() switch
        {
            "active" => query.Where(x => x.IsActive),
            "inactive" => query.Where(x => !x.IsActive),
            "individual" => query.Where(x => x.EntityType == PartyEntityType.Individual),
            "organization" => query.Where(x => x.EntityType == PartyEntityType.Organization),
            "local" => query.Where(x => x.SupplierScope == SupplierScope.Local),
            "international" => query.Where(x => x.SupplierScope == SupplierScope.International),
            _ => query
        };
    }

    private IQueryable<Customer> OrderCustomers(
        IQueryable<Customer> query,
        string? sortBy,
        SortDirection direction)
    {
        var accounts = db.Set<Account>().AsNoTracking();
        var normalized = sortBy?.Trim().ToLowerInvariant();

        return (normalized, direction) switch
        {
            ("namear", SortDirection.Descending) =>
                query.OrderByDescending(x => x.NameAr),

            ("accountcode", SortDirection.Descending) =>
                query.OrderByDescending(x =>
                    accounts
                        .Where(a => a.Id == x.AccountId)
                        .Select(a => a.Code)
                        .FirstOrDefault()),

            ("customercode", SortDirection.Descending) =>
                query.OrderByDescending(x => x.CustomerCode),

            (_, SortDirection.Descending) =>
                query.OrderByDescending(x => x.CustomerCode),

            ("namear", _) =>
                query.OrderBy(x => x.NameAr),

            ("accountcode", _) =>
                query.OrderBy(x =>
                    accounts
                        .Where(a => a.Id == x.AccountId)
                        .Select(a => a.Code)
                        .FirstOrDefault()),

            _ =>
                query.OrderBy(x => x.CustomerCode)
        };
    }

    private IQueryable<Supplier> OrderSuppliers(
        IQueryable<Supplier> query,
        string? sortBy,
        SortDirection direction)
    {
        var accounts = db.Set<Account>().AsNoTracking();
        var normalized = sortBy?.Trim().ToLowerInvariant();

        return (normalized, direction) switch
        {
            ("namear", SortDirection.Descending) =>
                query.OrderByDescending(x => x.NameAr),

            ("accountcode", SortDirection.Descending) =>
                query.OrderByDescending(x =>
                    accounts
                        .Where(a => a.Id == x.AccountId)
                        .Select(a => a.Code)
                        .FirstOrDefault()),

            ("suppliercode", SortDirection.Descending) =>
                query.OrderByDescending(x => x.SupplierCode),

            (_, SortDirection.Descending) =>
                query.OrderByDescending(x => x.SupplierCode),

            ("namear", _) =>
                query.OrderBy(x => x.NameAr),

            ("accountcode", _) =>
                query.OrderBy(x =>
                    accounts
                        .Where(a => a.Id == x.AccountId)
                        .Select(a => a.Code)
                        .FirstOrDefault()),

            _ =>
                query.OrderBy(x => x.SupplierCode)
        };
    }

    private async Task<IReadOnlyList<CustomerDto>> MapCustomersAsync(
        IReadOnlyList<Customer> customers,
        CancellationToken ct)
    {
        if (customers.Count == 0)
        {
            return Array.Empty<CustomerDto>();
        }

        var accountIds = customers
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var parentIds = accounts.Values
            .Where(x => x.ParentAccountId.HasValue)
            .Select(x => x.ParentAccountId!.Value)
            .Distinct()
            .ToArray();

        var parents = parentIds.Length == 0
            ? new Dictionary<Guid, Account>()
            : await db.Set<Account>()
                .AsNoTracking()
                .Where(x => parentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

        var result = new List<CustomerDto>(customers.Count);

        foreach (var customer in customers)
        {
            if (!accounts.TryGetValue(customer.AccountId, out var account))
            {
                throw new InvalidOperationException(
                    $"Customer '{customer.Id}' references missing account '{customer.AccountId}'.");
            }

            Account? parent = null;
            if (account.ParentAccountId.HasValue)
            {
                parents.TryGetValue(account.ParentAccountId.Value, out parent);
            }

            result.Add(MapCustomer(customer, account, parent));
        }

        return result;
    }

    private async Task<IReadOnlyList<SupplierDto>> MapSuppliersAsync(
        IReadOnlyList<Supplier> suppliers,
        CancellationToken ct)
    {
        if (suppliers.Count == 0)
        {
            return Array.Empty<SupplierDto>();
        }

        var accountIds = suppliers
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();

        var accounts = await db.Set<Account>()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var parentIds = accounts.Values
            .Where(x => x.ParentAccountId.HasValue)
            .Select(x => x.ParentAccountId!.Value)
            .Distinct()
            .ToArray();

        var parents = parentIds.Length == 0
            ? new Dictionary<Guid, Account>()
            : await db.Set<Account>()
                .AsNoTracking()
                .Where(x => parentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

        var result = new List<SupplierDto>(suppliers.Count);

        foreach (var supplier in suppliers)
        {
            if (!accounts.TryGetValue(supplier.AccountId, out var account))
            {
                throw new InvalidOperationException(
                    $"Supplier '{supplier.Id}' references missing account '{supplier.AccountId}'.");
            }

            Account? parent = null;
            if (account.ParentAccountId.HasValue)
            {
                parents.TryGetValue(account.ParentAccountId.Value, out parent);
            }

            result.Add(MapSupplier(supplier, account, parent));
        }

        return result;
    }

    private static CustomerDto MapCustomer(
        Customer c,
        Account a,
        Account? p) =>
        new(
            c.Id,
            c.CustomerCode,
            c.AccountId,
            a.Code,
            a.NameAr,
            a.ParentAccountId,
            p?.Code,
            (ContractPartyEntityType)(byte)c.EntityType,
            c.NameAr,
            c.NameEn,
            c.TradeName,
            c.NationalId,
            c.CommercialRegistrationNo,
            c.TaxNumber,
            c.DateOfBirth,
            (ContractGender)(byte)c.Gender,
            c.ContactInfo.ContactPersonName,
            c.ContactInfo.ContactPersonTitle,
            c.ContactInfo.Phone,
            c.ContactInfo.Mobile,
            c.ContactInfo.AlternatePhone,
            c.ContactInfo.WhatsAppNumber,
            c.ContactInfo.Email,
            c.ContactInfo.Website,
            (ContractContactMethod)(byte)c.ContactInfo.PreferredContactMethod,
            c.ContactInfo.Address.Country,
            c.ContactInfo.Address.Governorate,
            c.ContactInfo.Address.City,
            c.ContactInfo.Address.District,
            c.ContactInfo.Address.Street,
            c.ContactInfo.Address.Building,
            c.ContactInfo.Address.PostalCode,
            c.ContactInfo.Address.AddressDetails,
            c.IsCreditAllowed,
            c.CreditLimit,
            c.PaymentTermDays,
            c.CustomerSince,
            c.IsActive,
            c.Notes,
            Convert.ToBase64String(c.RowVersion),
            c.CreatedAtUtc,
            c.CreatedBy,
            c.LastModifiedAtUtc,
            c.LastModifiedBy);

    private static SupplierDto MapSupplier(
        Supplier s,
        Account a,
        Account? p) =>
        new(
            s.Id,
            s.SupplierCode,
            s.AccountId,
            a.Code,
            a.NameAr,
            a.ParentAccountId,
            p?.Code,
            (ContractPartyEntityType)(byte)s.EntityType,
            (ContractSupplierScope)(byte)s.SupplierScope,
            s.NameAr,
            s.NameEn,
            s.TradeName,
            s.NationalId,
            s.CommercialRegistrationNo,
            s.TaxNumber,
            s.ContactInfo.ContactPersonName,
            s.ContactInfo.ContactPersonTitle,
            s.ContactInfo.Phone,
            s.ContactInfo.Mobile,
            s.ContactInfo.AlternatePhone,
            s.ContactInfo.WhatsAppNumber,
            s.ContactInfo.Email,
            s.ContactInfo.Website,
            (ContractContactMethod)(byte)s.ContactInfo.PreferredContactMethod,
            s.ContactInfo.Address.Country,
            s.ContactInfo.Address.Governorate,
            s.ContactInfo.Address.City,
            s.ContactInfo.Address.District,
            s.ContactInfo.Address.Street,
            s.ContactInfo.Address.Building,
            s.ContactInfo.Address.PostalCode,
            s.ContactInfo.Address.AddressDetails,
            s.CreditLimit,
            s.PaymentTermDays,
            s.DefaultLeadTimeDays,
            s.SupplierSince,
            s.IsActive,
            s.Notes,
            Convert.ToBase64String(s.RowVersion),
            s.CreatedAtUtc,
            s.CreatedBy,
            s.LastModifiedAtUtc,
            s.LastModifiedBy);
}
