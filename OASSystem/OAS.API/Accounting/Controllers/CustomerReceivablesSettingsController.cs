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
[Route("api/accounting/customer-receivables-settings")]
public sealed class CustomerReceivablesSettingsController(
    OasDbContext db,
    IPermissionChecker permissions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CustomerReceivablesSettingsDto>> Get(CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(AccountingPermissions.Settings.View, ct) &&
            !await permissions.HasPermissionAsync(AccountingPermissions.Settings.Manage, ct) &&
            !await permissions.HasPermissionAsync(AccountingPermissions.Customers.Create, ct))
            return Forbid();
        return Ok(await CustomerReceivablesSettingsStore.ReadAsync(db, ct));
    }

    [HttpPut]
    public async Task<ActionResult<CustomerReceivablesSettingsDto>> Update(
        [FromBody] UpdateCustomerReceivablesSettingsRequest request,
        CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(AccountingPermissions.Settings.Manage, ct))
            return Forbid();

        if (request.ParentAccountId is { } id)
        {
            var account = await db.Set<Account>().AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, ct)
                ?? throw new ConflictException(
                    "customer_receivables_parent_missing",
                    "الحساب الرئيسي المحدد لذمم العملاء غير موجود.");

            if (!IsEligibleReceivablesParent(account))
                throw new ConflictException(
                    "customer_receivables_parent_invalid",
                    "اختر حساب ذمم عملاء نشطًا من نوع «حساب تحكمي»، وتصنيفه «الأصول»، وطبيعته «مدين».");
        }

        await CustomerReceivablesSettingsStore.SaveAsync(db, request.ParentAccountId, ct);
        return Ok(await CustomerReceivablesSettingsStore.ReadAsync(db, ct));
    }

    internal static bool IsEligibleReceivablesParent(Account account) =>
        account.IsActive &&
        account.IsControlAccount &&
        account.AccountType == AccountType.Control &&
        account.AccountClass == AccountClass.Asset &&
        account.NormalBalance == NormalBalance.Debit;
}
