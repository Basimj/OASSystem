using Microsoft.EntityFrameworkCore;
using OAS.Contracts.Accounting.Settings;
using OAS.Domain.Accounting.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.API.Accounting.Controllers;

/// <summary>
/// Reads/writes the central customer receivables parent. New customers are linked
/// to this account; existing customer accounts are never changed by this setting.
/// </summary>
public static class CustomerReceivablesSettingsStore
{
    public static async Task<Guid?> GetParentIdAsync(OasDbContext db, CancellationToken ct)
    {
        var ids = await db.Database.SqlQueryRaw<Guid>(
            "SELECT [ParentAccountId] AS [Value] FROM [dbo].[tbl_CustomerReceivablesSettings] WHERE [Id] = 1")
            .ToListAsync(ct);
        return ids.Count == 0 ? null : ids[0];
    }

    public static async Task<CustomerReceivablesSettingsDto> ReadAsync(OasDbContext db, CancellationToken ct)
    {
        var id = await GetParentIdAsync(db, ct);
        if (!id.HasValue)
            return new CustomerReceivablesSettingsDto(null, null, null);

        var account = await db.Set<Account>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id.Value, ct);
        return new CustomerReceivablesSettingsDto(
            id,
            account?.Code,
            account?.NameAr);
    }

    public static async Task SaveAsync(OasDbContext db, Guid? id, CancellationToken ct)
    {
        if (!id.HasValue)
        {
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM [dbo].[tbl_CustomerReceivablesSettings] WHERE [Id] = 1", ct);
            return;
        }

        var accountId = id.Value;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE [dbo].[tbl_CustomerReceivablesSettings]
            SET [ParentAccountId] = {accountId}
            WHERE [Id] = 1;
            IF @@ROWCOUNT = 0
                INSERT INTO [dbo].[tbl_CustomerReceivablesSettings] ([Id], [ParentAccountId])
                VALUES (1, {accountId});", ct);
    }
}
