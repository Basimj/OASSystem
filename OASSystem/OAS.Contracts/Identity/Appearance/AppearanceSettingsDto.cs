namespace OAS.Contracts.Identity.Appearance;

// The default is the currently adopted Ocean UI. These values are preferences,
// not permissions, and are always scoped to the authenticated user.
public sealed class AppearanceSettingsDto
{
    public string ThemePreset { get; set; } = "Ocean";
    public string ThemeMode { get; set; } = "Light";
    public string PrimaryColor { get; set; } = "#459DB9";
    public string SidebarBackground { get; set; } = "#1B4659";
    public string SidebarTextColor { get; set; } = "#FFFFFF";
    public string SidebarIconColor { get; set; } = "#D6EAF1";
    public string SidebarHoverBackground { get; set; } = "#335F72";
    public string SidebarActiveBackground { get; set; } = "#287E9A";
    public string SidebarActiveTextColor { get; set; } = "#FFFFFF";
    public string SidebarActiveIconColor { get; set; } = "#FFFFFF";
    public string HeaderBackground { get; set; } = "#FFFFFF";
    public string HeaderTextColor { get; set; } = "#1C3444";
    public string HeaderIconColor { get; set; } = "#526C87";
    // Application tab strip — independent from the top header.
    public string TabsBarBackground { get; set; } = "#FFFFFF";
    public string TabsBackground { get; set; } = "#FFFFFF";
    public string TabsTextColor { get; set; } = "#61647F";
    public string TabsIconColor { get; set; } = "#61647F";
    public string TabsActiveBackground { get; set; } = "#E8F6FA";
    public string TabsActiveTextColor { get; set; } = "#216781";
    public string TabsActiveIconColor { get; set; } = "#216781";
    public string TabsBorderColor { get; set; } = "#B4DCE9";
    public string TabsStyle { get; set; } = "Classic";
    public string TabsSize { get; set; } = "Compact";
    public string HeaderLayout { get; set; } = "Classic";
    public string[] HeaderOrder { get; set; } = ["user", "notifications", "session"];
    public string FontFamily { get; set; } = "Cairo";
    public decimal FontScale { get; set; } = 1.0m;
    public string SidebarMode { get; set; } = "Expanded";
}
