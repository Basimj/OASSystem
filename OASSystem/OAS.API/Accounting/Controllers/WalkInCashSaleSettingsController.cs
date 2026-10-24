using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OAS.API.Accounting.Controllers;
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

        if (request.CustomerId is { } id)
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new ConflictException("walkin_customer_missing", "العميل النقدي الافتراضي غير موجود.");
            if (!customer.IsActive || customer.IsCreditAllowed)
                throw new ConflictException("walkin_customer_invalid", "اختر عميلًا عامًا فعالًا وغير مسموح له بالبيع الآجل.");

            var account = await db.Set<Account>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == customer.AccountId, ct);
            if (account is null || !account.CanReceivePosting() ||
                account.AccountClass != AccountClass.Asset ||
                account.NormalBalance != NormalBalance.Debit)
                throw new ConflictException("walkin_account_invalid", "حساب ذمم العميل النقدي يجب أن يكون حساب أصل مدينًا وقابلًا للترحيل.");
        }

        await WalkInCashSaleSettingsStore.SaveAsync(db, request.CustomerId, ct);
        return Ok(await WalkInCashSaleSettingsStore.ReadAsync(db, ct));
    }
}
