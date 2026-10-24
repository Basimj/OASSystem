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
            try { return NormalizeLegacyOcean(Validate(JsonSerializer.Deserialize<AppearanceSettingsDto>(json, JsonOptions) ?? new())); }
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

    // An unmodified Ocean preset saved by an earlier version should adopt the
    // updated Sea Glass defaults. Keep custom/personalized palettes unchanged.
    private static AppearanceSettingsDto NormalizeLegacyOcean(AppearanceSettingsDto settings)
    {
        // The former Sea Glass default stored petrol values in SQL. Upgrade the
        // built-in Ocean palette at read time; custom palettes are never altered.
        // A user who picked a single Ocean color keeps the rest of their choices.
        if (settings.ThemePreset == "Ocean" &&
            settings.SidebarBackground.Equals("#28596C", StringComparison.OrdinalIgnoreCase))
        {
            settings.SidebarBackground = "#DDF3F8";
            if (settings.SidebarTextColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase)) settings.SidebarTextColor = "#174558";
            if (settings.SidebarIconColor.Equals("#D3EAF0", StringComparison.OrdinalIgnoreCase)) settings.SidebarIconColor = "#28667B";
            if (settings.SidebarHoverBackground.Equals("#346F80", StringComparison.OrdinalIgnoreCase)) settings.SidebarHoverBackground = "#C8EAF2";
            if (settings.SidebarActiveBackground.Equals("#DDF7F1", StringComparison.OrdinalIgnoreCase)) settings.SidebarActiveBackground = "#B9E5F1";
            if (settings.SidebarActiveTextColor.Equals("#17495A", StringComparison.OrdinalIgnoreCase)) settings.SidebarActiveTextColor = "#123D56";
            if (settings.SidebarActiveIconColor.Equals("#0F866F", StringComparison.OrdinalIgnoreCase)) settings.SidebarActiveIconColor = "#086C87";
            if (settings.PrimaryColor.Equals("#0F866F", StringComparison.OrdinalIgnoreCase)) settings.PrimaryColor = "#0B8095";
            if (settings.HeaderTextColor.Equals("#243D49", StringComparison.OrdinalIgnoreCase)) settings.HeaderTextColor = "#21485C";
            if (settings.HeaderIconColor.Equals("#436B7C", StringComparison.OrdinalIgnoreCase)) settings.HeaderIconColor = "#3B7185";
            if (settings.TabsTextColor.Equals("#486678", StringComparison.OrdinalIgnoreCase)) settings.TabsTextColor = "#42657B";
            if (settings.TabsIconColor.Equals("#486678", StringComparison.OrdinalIgnoreCase)) settings.TabsIconColor = "#42657B";
            if (settings.TabsActiveBackground.Equals("#E2F4F7", StringComparison.OrdinalIgnoreCase)) settings.TabsActiveBackground = "#D7F1F8";
            if (settings.TabsActiveTextColor.Equals("#18586D", StringComparison.OrdinalIgnoreCase)) settings.TabsActiveTextColor = "#126078";
            if (settings.TabsActiveIconColor.Equals("#0F866F", StringComparison.OrdinalIgnoreCase)) settings.TabsActiveIconColor = "#0B8095";
            if (settings.TabsBorderColor.Equals("#CDE5EB", StringComparison.OrdinalIgnoreCase)) settings.TabsBorderColor = "#C8E5EE";
            return settings;
        }
        if (settings.ThemePreset == "Ocean" &&
            settings.PrimaryColor.Equals("#459DB9", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarBackground.Equals("#1B4659", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarTextColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarIconColor.Equals("#D6EAF1", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarHoverBackground.Equals("#335F72", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarActiveBackground.Equals("#287E9A", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarActiveTextColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) &&
            settings.SidebarActiveIconColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) &&
            settings.HeaderBackground.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) &&
            settings.HeaderTextColor.Equals("#1C3444", StringComparison.OrdinalIgnoreCase) &&
            settings.HeaderIconColor.Equals("#526C87", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsBarBackground.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsBackground.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsTextColor.Equals("#61647F", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsIconColor.Equals("#61647F", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsActiveBackground.Equals("#E8F6FA", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsActiveTextColor.Equals("#216781", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsActiveIconColor.Equals("#216781", StringComparison.OrdinalIgnoreCase) &&
            settings.TabsBorderColor.Equals("#B4DCE9", StringComparison.OrdinalIgnoreCase))
        {
            return new AppearanceSettingsDto
            {
                ThemeMode = settings.ThemeMode,
                SidebarMode = settings.SidebarMode,
                HeaderLayout = settings.HeaderLayout,
                HeaderOrder = settings.HeaderOrder,
                TabsStyle = settings.TabsStyle,
                TabsSize = settings.TabsSize,
                FontFamily = settings.FontFamily,
                FontScale = settings.FontScale
            };
        }
        // Partially customized settings from the original (pre Sea Glass)
        // Ocean palette: update only the retired background and unreadable
        // light-on-light navigation defaults, preserving all user edits.
        if (settings.ThemePreset == "Ocean" &&
            settings.SidebarBackground.Equals("#1B4659", StringComparison.OrdinalIgnoreCase))
        {
            settings.SidebarBackground = "#DDF3F8";
            if (settings.SidebarTextColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase)) settings.SidebarTextColor = "#174558";
            if (settings.SidebarIconColor.Equals("#D6EAF1", StringComparison.OrdinalIgnoreCase)) settings.SidebarIconColor = "#28667B";
            if (settings.SidebarHoverBackground.Equals("#335F72", StringComparison.OrdinalIgnoreCase)) settings.SidebarHoverBackground = "#C8EAF2";
            if (settings.SidebarActiveBackground.Equals("#287E9A", StringComparison.OrdinalIgnoreCase)) settings.SidebarActiveBackground = "#B9E5F1";
            if (settings.SidebarActiveTextColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase)) settings.SidebarActiveTextColor = "#123D56";
            if (settings.SidebarActiveIconColor.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase)) settings.SidebarActiveIconColor = "#086C87";
            if (settings.PrimaryColor.Equals("#459DB9", StringComparison.OrdinalIgnoreCase)) settings.PrimaryColor = "#0B8095";
        }
        return settings;
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
            s.ThemePreset is not ("Ocean" or "Custom" or "Classic" or "Midnight" or "Slate" or "SkyYellow") ||
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
