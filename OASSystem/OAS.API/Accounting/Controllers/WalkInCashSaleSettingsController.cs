using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Settings;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/walk-in-customer")]
public sealed class WalkInCashSaleSettingsController(
    OasDbContext db,
    IPermissionChecker permissions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WalkInCashCustomerSettingsDto>> Get(CancellationToken ct)
        => Ok(await WalkInCashSaleSettingsStore.ReadAsync(db, ct));

    [HttpPut]
    public async Task<ActionResult<WalkInCashCustomerSettingsDto>> Update(
        [FromBody] UpdateWalkInCashCustomerSettingsRequest request,
        CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(AccountingPermissions.Settings.Manage, ct))
            return Forbid();

        // The source of truth is always CustomerId. The selected customer's
        // receivables AccountId is supplied by the customer master, not chosen
        // independently from the chart of accounts.
        if (request.ReceivablesAccountId.HasValue)
            throw new ConflictException(
                "walkin_choose_customer_not_account",
                "اختر العميل النقدي العام من قائمة العملاء؛ حساب الذمم يؤخذ تلقائيًا من سجل العميل.");

        if (request.CustomerId is { } id)
        {
            var customer = await db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new ConflictException(
                    "walkin_customer_missing",
                    "العميل النقدي الافتراضي المحدد غير موجود.");

            if (!customer.IsActive || customer.IsCreditAllowed ||
                !IsGenericCashCustomer(customer.CustomerCode, customer.NameAr))
                throw new ConflictException(
                    "walkin_customer_invalid",
                    "اختر عميلًا عامًا نشطًا باسم «عميل نقدي عام»، غير مسموح له بالبيع الآجل، وليس عميلًا حقيقيًا.");

            var account = await db.Set<Account>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == customer.AccountId, ct)
                ?? throw new ConflictException(
                    "walkin_customer_account_missing",
                    "حساب الذمم المرتبط بالعميل النقدي غير موجود.");

            await EnsureValidReceivablesAccountAsync(account, ct);
        }

        // NULL clears the default; the existing customer and posted documents
        // are never deleted or relinked by this settings endpoint.
        await WalkInCashSaleSettingsStore.SaveAsync(db, request.CustomerId, ct);
        return Ok(await WalkInCashSaleSettingsStore.ReadAsync(db, ct));
    }

    private static bool IsGenericCashCustomer(string? code, string? name) =>
        (!string.IsNullOrWhiteSpace(code) &&
         code.StartsWith("WALKIN-", StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(name?.Trim(), "عميل نقدي عام", StringComparison.Ordinal) ||
        string.Equals(name?.Trim(), "عميل نقدي", StringComparison.Ordinal);

    private async Task EnsureValidReceivablesAccountAsync(
        Account account, CancellationToken ct)
    {
        if (!account.CanReceivePosting() || account.IsControlAccount ||
            account.AccountType != AccountType.Subledger ||
            account.AccountClass != AccountClass.Asset ||
            account.NormalBalance != NormalBalance.Debit ||
            !account.ParentAccountId.HasValue)
            throw new ConflictException(
                "walkin_receivable_account_invalid",
                "حساب العميل النقدي يجب أن يكون حساب ذمم فرعيًا نشطًا، أصلًا مدينًا وقابلًا للترحيل.");

        var parent = await db.Set<Account>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == account.ParentAccountId.Value, ct);
        if (parent is null || !parent.IsActive || !parent.IsControlAccount ||
            parent.AccountType != AccountType.Control ||
            parent.AccountClass != AccountClass.Asset ||
            parent.NormalBalance != NormalBalance.Debit)
            throw new ConflictException(
                "walkin_receivable_parent_invalid",
                "حساب العميل النقدي يجب أن يكون تحت حساب مراقبة ذمم العملاء.");
    }
}
