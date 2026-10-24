namespace OAS.UiLib.Core.Models;

// UI-only model. Client maps this to the API contract to keep UiLib independent.
public sealed class AppearanceSettingsModel
{
    public string ThemePreset { get; set; } = "Ocean";
    public string ThemeMode { get; set; } = "Light";
    public string PrimaryColor { get; set; } = "#0B8095";
    public string SidebarBackground { get; set; } = "#DDF3F8";
    public string SidebarTextColor { get; set; } = "#174558";
    public string SidebarIconColor { get; set; } = "#28667B";
    public string SidebarHoverBackground { get; set; } = "#C8EAF2";
    public string SidebarActiveBackground { get; set; } = "#B9E5F1";
    public string SidebarActiveTextColor { get; set; } = "#123D56";
    public string SidebarActiveIconColor { get; set; } = "#086C87";
    public string HeaderBackground { get; set; } = "#FFFFFF";
    public string HeaderTextColor { get; set; } = "#21485C";
    public string HeaderIconColor { get; set; } = "#3B7185";
    // Application tab strip — independent from the top header.
    public string TabsBarBackground { get; set; } = "#FFFFFF";
    public string TabsBackground { get; set; } = "#FFFFFF";
    public string TabsTextColor { get; set; } = "#42657B";
    public string TabsIconColor { get; set; } = "#42657B";
    public string TabsActiveBackground { get; set; } = "#D7F1F8";
    public string TabsActiveTextColor { get; set; } = "#126078";
    public string TabsActiveIconColor { get; set; } = "#0B8095";
    public string TabsBorderColor { get; set; } = "#C8E5EE";
    public string TabsStyle { get; set; } = "Classic";
    public string TabsSize { get; set; } = "Compact";
    public string HeaderLayout { get; set; } = "Classic";
    public string[] HeaderOrder { get; set; } = ["user", "notifications", "session"];
    public string FontFamily { get; set; } = "Cairo";
    public decimal FontScale { get; set; } = 1.0m;
    public string SidebarMode { get; set; } = "Expanded";
}
