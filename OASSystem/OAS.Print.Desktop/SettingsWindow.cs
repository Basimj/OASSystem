using System.Windows;
using System.Windows.Controls;
using OAS.Print.Desktop.Models;

namespace OAS.Print.Desktop;

public sealed class SettingsWindow : Window
{
    private readonly TextBox _apiUrl = new();
    private readonly TextBox _workstation = new();
    private readonly TextBox _apiKey = new();
    private readonly CheckBox _poll = new() { Content = "استقبال مهام الطباعة من OAS API" };
    private readonly TextBox _interval = new();
    private readonly ComboBox _defaultPrinter = new();

    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings settings, IEnumerable<string> printers)
    {
        Settings = settings;
        Title = "إعدادات OAS Print";
        Width = 560;
        Height = 570;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        ResizeMode = ResizeMode.NoResize;

        _apiUrl.Text = settings.ApiBaseUrl;
        _workstation.Text = settings.WorkstationCode;
        _apiKey.Text = settings.ApiKey;
        _poll.IsChecked = settings.PollEnabled;
        _interval.Text = settings.PollIntervalSeconds.ToString();
        _defaultPrinter.ItemsSource = printers.ToArray();
        _defaultPrinter.SelectedItem = settings.DefaultPrinterName;

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(TitleText("اتصال OAS"));
        panel.Children.Add(Label("عنوان API")); panel.Children.Add(_apiUrl);
        panel.Children.Add(Label("رمز محطة العمل")); panel.Children.Add(_workstation);
        panel.Children.Add(Label("Print API Key (اختياري)")); panel.Children.Add(_apiKey);
        panel.Children.Add(_poll);
        panel.Children.Add(Label("الفحص كل كم ثانية")); panel.Children.Add(_interval);
        panel.Children.Add(TitleText("الطابعة الافتراضية"));
        panel.Children.Add(_defaultPrinter);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 18, 0, 0) };
        var save = new Button { Content = "حفظ", Width = 100, IsDefault = true };
        save.Click += (_, _) => SaveAndClose();
        var cancel = new Button { Content = "إلغاء", Width = 100, IsCancel = true };
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = panel;
    }

    private void SaveAndClose()
    {
        Settings.ApiBaseUrl = _apiUrl.Text.Trim();
        Settings.WorkstationCode = string.IsNullOrWhiteSpace(_workstation.Text) ? "DEFAULT" : _workstation.Text.Trim();
        Settings.ApiKey = _apiKey.Text.Trim();
        Settings.PollEnabled = _poll.IsChecked == true;
        Settings.PollIntervalSeconds = int.TryParse(_interval.Text, out var seconds) ? Math.Clamp(seconds, 1, 60) : 3;
        Settings.DefaultPrinterName = _defaultPrinter.SelectedItem as string ?? string.Empty;
        DialogResult = true;
        Close();
    }

    private static TextBlock Label(string text) => new() { Text = text, Margin = new Thickness(3, 7, 3, 0) };
    private static TextBlock TitleText(string text) => new() { Text = text, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(3, 10, 3, 8) };
}
