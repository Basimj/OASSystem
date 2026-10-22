using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Security;
using OAS.Application.Identity.Appearance;
using OAS.Contracts.Identity.Appearance;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Identity.Services;

// The preferences table is deliberately a small JSON document store.
// SQL is parameterized, user identity comes only from the authenticated session,
// and the document's shape is validated before the write.
public sealed class SqlAppearancePreferencesService(
    OasDbContext db, ICurrentUser currentUser) : IAppearancePreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex ColorPattern = new("^#[A-Fa-f0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> HeaderKeys = ["user", "notifications", "session"];
    private Guid UserId => currentUser.IsAuthenticated && Guid.TryParse(currentUser.UserId, out var id)
        ? id : throw new UnauthorizedAccessException("يلزم تسجيل الدخول.");

    public async Task<AppearanceSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var id = UserId;
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT [SettingsJson] FROM [security].[UserAppearancePreferences] WHERE [UserId]=@userId";
            AddParameter(command, "@userId", id);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is not string json) return new AppearanceSettingsDto();
            try { return Validate(JsonSerializer.Deserialize<AppearanceSettingsDto>(json, JsonOptions) ?? new()); }
            catch (Exception ex) when (ex is JsonException or ArgumentException) { return new AppearanceSettingsDto(); }
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    public async Task<AppearanceSettingsDto> SaveAsync(AppearanceSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var id = UserId;
        var safe = Validate(settings);
        var json = JsonSerializer.Serialize(safe, JsonOptions);
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                UPDATE [security].[UserAppearancePreferences] WITH (UPDLOCK, SERIALIZABLE)
                  SET [SettingsJson]=@json, [UpdatedAtUtc]=@now
                  WHERE [UserId]=@userId;
                IF @@ROWCOUNT = 0
                  INSERT [security].[UserAppearancePreferences] ([UserId],[SettingsJson],[UpdatedAtUtc])
                  VALUES (@userId,@json,@now);
                COMMIT TRANSACTION;
                """;
            AddParameter(command, "@userId", id);
            AddParameter(command, "@json", json);
            AddParameter(command, "@now", DateTimeOffset.UtcNow);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return safe;
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var id = UserId;
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "DELETE FROM [security].[UserAppearancePreferences] WHERE [UserId]=@userId";
            AddParameter(command, "@userId", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }

    private static AppearanceSettingsDto Validate(AppearanceSettingsDto? s)
    {
        if (s is null) throw new ArgumentException("إعدادات المظهر مطلوبة.");
        var colors = new[] {
            s.PrimaryColor, s.SidebarBackground, s.SidebarTextColor, s.SidebarIconColor,
            s.SidebarHoverBackground, s.SidebarActiveBackground, s.SidebarActiveTextColor,
            s.SidebarActiveIconColor, s.HeaderBackground, s.HeaderTextColor, s.HeaderIconColor,
            s.TabsBarBackground, s.TabsBackground, s.TabsTextColor, s.TabsIconColor,
            s.TabsActiveBackground, s.TabsActiveTextColor, s.TabsActiveIconColor, s.TabsBorderColor
        };
        if (colors.Any(x => x is null || !ColorPattern.IsMatch(x)))
            throw new ArgumentException("قيمة اللون غير صالحة. يجب استخدام رمز HEX من ستة أرقام.");
        if (s.ThemeMode is not ("Light" or "Dark") ||
            s.ThemePreset is not ("Ocean" or "Custom" or "Classic" or "Midnight" or "Slate") ||
            s.HeaderLayout is not ("Classic" or "Compact" or "Minimal") ||
            s.TabsStyle is not ("Classic" or "Pills" or "Underline") ||
            s.TabsSize is not ("Compact" or "Normal" or "Comfortable") ||
            s.SidebarMode is not ("Expanded" or "Collapsed") ||
            s.FontFamily is not ("Cairo" or "Tajawal" or "IBM Plex Sans Arabic" or "Segoe UI") ||
            s.FontScale < .85m || s.FontScale > 1.25m)
            throw new ArgumentException("أحد خيارات المظهر غير مدعوم.");
        if (s.HeaderOrder is null || s.HeaderOrder.Length != 3 ||
            s.HeaderOrder.Distinct(StringComparer.Ordinal).Count() != 3 ||
            s.HeaderOrder.Any(x => !HeaderKeys.Contains(x)))
            throw new ArgumentException("ترتيب عناصر الشريط العلوي غير صالح.");
        // User-chosen colors intentionally have NO contrast threshold.
        // Only a valid RGB HEX format is required to prevent invalid CSS injection.
        return s;
    }

}
