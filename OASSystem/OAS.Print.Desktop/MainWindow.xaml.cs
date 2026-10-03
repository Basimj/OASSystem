using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using OAS.Print.Desktop.Models;
using OAS.Print.Desktop.Services;
using OAS.Printing.Core.Models;
using OAS.Printing.Core.Services;

namespace OAS.Print.Desktop;

public partial class MainWindow : Window
{
    private readonly TemplateStore _templateStore = new();
    private readonly TemplateRenderer _renderer = new();
    private readonly PrinterService _printerService = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly JobPollingService _jobPolling = new();
    private readonly ObservableCollection<TemplateDefinition> _templates = [];
    private readonly ObservableCollection<PrintJobHistoryItem> _jobHistory = [];
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    private TemplateDefinition? _currentTemplate;
    private TemplateElement? _selectedElement;
    private FrameworkElement? _selectedVisual;
    private JsonElement _previewData = JsonDocument.Parse("{}").RootElement.Clone();
    private AppSettings _settings = new();
    private bool _loadingUi;
    private bool _isDragging;
    private Point _dragStart;
    private double _elementStartX;
    private double _elementStartY;
    private double _zoomPercent = 100;
    private string? _lastRequestedBy;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        FitWindowToWorkArea();

        _settings = _settingsStore.Load();
        _settings.PrinterBindings ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        DocumentTypeCombo.ItemsSource = DocumentFieldCatalog.DocumentTypes;
        JobsGrid.ItemsSource = _jobHistory;
        DashboardJobsGrid.ItemsSource = _jobHistory;

        LoadPrinters();
        LoadTemplates();
        LoadSettingsUi();
        UpdateHeaderAndDashboard();
        ShowPanel(DashboardPanel, NavDashboardButton);

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        _jobPolling.JobReceived += HandlePrintJobAsync;
        _jobPolling.StatusChanged += OnPollingStatusChanged;
        _jobPolling.Start(_settings);

        StatusText.Text = "جاهز لاستقبال مهام الطباعة.";
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _clockTimer.Stop();
        _jobPolling.Dispose();
    }

    private void FitWindowToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var availableWidth = Math.Max(1, workArea.Width - 16);
        var availableHeight = Math.Max(1, workArea.Height - 16);

        // Never allow the minimum size to push the title bar outside the usable desktop.
        MinWidth = Math.Min(MinWidth, availableWidth);
        MinHeight = Math.Min(MinHeight, availableHeight);
        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;

        Width = Math.Clamp(Width, Math.Max(1, MinWidth), availableWidth);
        Height = Math.Clamp(Height, Math.Max(1, MinHeight), availableHeight);

        Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
        Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        ClockText.Text = now.ToString("HH:mm:ss");
        DateText.Text = now.ToString("dd/MM/yyyy");
    }

    private void OnPollingStatusChanged(string message)
    {
        Dispatcher.Invoke(() =>
        {
            ConnectionText.Text = message;
            HeaderConnectionText.Text = message.StartsWith("متصل", StringComparison.OrdinalIgnoreCase)
                ? "متصل بـ OAS"
                : message.StartsWith("غير متصل", StringComparison.OrdinalIgnoreCase)
                    ? "غير متصل"
                    : message;

            ConnectionDot.Fill = message.StartsWith("متصل", StringComparison.OrdinalIgnoreCase)
                ? Brushes.SeaGreen
                : message.StartsWith("غير متصل", StringComparison.OrdinalIgnoreCase) || message.StartsWith("API:", StringComparison.OrdinalIgnoreCase)
                    ? Brushes.IndianRed
                    : Brushes.Goldenrod;
        });
    }

    private void LoadPrinters()
    {
        try
        {
            var printers = _printerService.GetPrinters().ToArray();
            PrinterCombo.ItemsSource = printers;
            DefaultPrinterCombo.ItemsSource = printers;
            SettingsPrinterCombo.ItemsSource = printers;
            SettingsReceiptPrinterCombo.ItemsSource = printers;
            SettingsPaymentPrinterCombo.ItemsSource = printers;
            PrintersList.ItemsSource = printers;

            var preferred = !string.IsNullOrWhiteSpace(_settings.DefaultPrinterName)
                ? _settings.DefaultPrinterName
                : printers.FirstOrDefault();

            PrinterCombo.SelectedItem = preferred;
            DefaultPrinterCombo.SelectedItem = preferred;
            SettingsPrinterCombo.SelectedItem = preferred;

            PrintersCountText.Text = printers.Length.ToString();
            SidebarPrinterText.Text = string.IsNullOrWhiteSpace(preferred) ? "لم تحدد طابعة" : preferred;
            DashboardPrinterText.Text = string.IsNullOrWhiteSpace(preferred) ? "لم تحدد طابعة" : preferred;
        }
        catch (Exception ex)
        {
            StatusText.Text = "تعذر قراءة الطابعات: " + ex.Message;
        }
    }

    private void LoadTemplates(Guid? selectId = null)
    {
        _templates.Clear();
        foreach (var template in _templateStore.LoadAll())
            _templates.Add(template);

        if (_templates.Count == 0)
        {
            var template = CreateDefaultTemplate();
            _templateStore.Save(template);
            _templates.Add(template);
        }

        TemplatesList.ItemsSource = _templates;
        DesignerTemplateCombo.ItemsSource = _templates;
        TemplatesCountText.Text = _templates.Count.ToString();

        var selected = selectId.HasValue
            ? _templates.FirstOrDefault(x => x.Id == selectId.Value) ?? _templates[0]
            : _currentTemplate is not null
                ? _templates.FirstOrDefault(x => x.Id == _currentTemplate.Id) ?? _templates[0]
                : _templates[0];

        TemplatesList.SelectedItem = selected;
        DesignerTemplateCombo.SelectedItem = selected;
    }

    private TemplateDefinition CreateDefaultTemplate() => new()
    {
        Code = "TPL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
        Name = "قالب جديد",
        DocumentType = "ReceiptVoucher",
        PaperWidthMm = 210,
        PaperHeightMm = 297,
        IsDefault = false,
        Elements =
        [
            new TemplateElement
            {
                Type = TemplateElementType.Text,
                Name = "العنوان",
                Text = "سند قبض",
                Xmm = 15,
                Ymm = 18,
                WidthMm = 180,
                HeightMm = 18,
                FontSize = 24,
                Bold = true,
                Alignment = TemplateTextAlignment.Center,
                ForegroundColor = "#24213B"
            }
        ]
    };

    private void TemplatesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi || TemplatesList.SelectedItem is not TemplateDefinition template)
            return;

        SelectTemplate(template, loadSampleData: true, synchronizeControls: true);
    }

    private void DesignerTemplateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi || DesignerTemplateCombo.SelectedItem is not TemplateDefinition template)
            return;

        SelectTemplate(template, loadSampleData: true, synchronizeControls: true);
    }

    private void SelectTemplate(TemplateDefinition template, bool loadSampleData, bool synchronizeControls)
    {
        _currentTemplate = template;
        _selectedElement = null;

        if (loadSampleData)
            _previewData = SampleDataStore.Load(template.DocumentType);

        _loadingUi = true;
        if (synchronizeControls)
        {
            TemplatesList.SelectedItem = template;
            DesignerTemplateCombo.SelectedItem = template;
        }
        _loadingUi = false;

        LoadTemplateUi();
        UpdateTemplateSummary();
        RefreshDesigner();
    }

    private void LoadTemplateUi()
    {
        if (_currentTemplate is null)
            return;

        _loadingUi = true;
        TemplateNameText.Text = _currentTemplate.Name;
        TemplateCodeText.Text = _currentTemplate.Code;
        DocumentTypeCombo.SelectedItem = _currentTemplate.DocumentType;
        PaperWidthText.Text = _currentTemplate.PaperWidthMm.ToString("0.##");
        PaperHeightText.Text = _currentTemplate.PaperHeightMm.ToString("0.##");
        LandscapeCheck.IsChecked = _currentTemplate.Landscape;
        DefaultTemplateCheck.IsChecked = _currentTemplate.IsDefault;
        LoadFieldCatalog();
        ClearElementUi();
        _loadingUi = false;
    }

    private void UpdateTemplateSummary()
    {
        if (_currentTemplate is null)
        {
            TemplateSummaryName.Text = "-";
            TemplateSummaryCode.Text = "-";
            TemplateSummaryType.Text = "-";
            TemplateSummaryPaper.Text = "-";
            TemplateSummaryElements.Text = "0";
            return;
        }

        TemplateSummaryName.Text = _currentTemplate.Name;
        TemplateSummaryCode.Text = _currentTemplate.Code;
        TemplateSummaryType.Text = GetDocumentTypeArabicName(_currentTemplate.DocumentType);
        TemplateSummaryPaper.Text = $"{_currentTemplate.PaperWidthMm:0.##} × {_currentTemplate.PaperHeightMm:0.##} مم" + (_currentTemplate.Landscape ? " — أفقي" : "");
        TemplateSummaryElements.Text = _currentTemplate.Elements.Count.ToString();
    }

    private void LoadFieldCatalog()
    {
        var documentType = _currentTemplate?.DocumentType ?? DocumentTypeCombo.SelectedItem as string;
        FieldPathCombo.ItemsSource = DocumentFieldCatalog.GetFields(documentType);
    }

    private void RefreshDesigner()
    {
        if (_currentTemplate is null)
            return;

        var rendered = _renderer.BuildCanvas(_currentTemplate, _previewData, true);
        DesignerCanvas.Children.Clear();
        DesignerCanvas.Width = rendered.Width;
        DesignerCanvas.Height = rendered.Height;

        while (rendered.Children.Count > 0)
        {
            var child = rendered.Children[0];
            rendered.Children.RemoveAt(0);
            DesignerCanvas.Children.Add(child);
        }

        _selectedVisual = null;
        if (_selectedElement is not null)
        {
            foreach (FrameworkElement child in DesignerCanvas.Children)
            {
                if (child.Tag is TemplateElement element && element.Id == _selectedElement.Id)
                {
                    _selectedVisual = child;
                    if (child is Border border)
                    {
                        border.BorderBrush = Brushes.DarkOrange;
                        border.BorderThickness = new Thickness(Math.Max(1.2, border.BorderThickness.Left));
                    }
                    break;
                }
            }
        }

        UpdateTemplateSummary();
    }

    private void SelectElement(TemplateElement element, FrameworkElement visual)
    {
        _selectedElement = element;
        _selectedVisual = visual;
        LoadElementUi();
        RefreshDesigner();
    }

    private void LoadElementUi()
    {
        if (_selectedElement is null)
        {
            ClearElementUi();
            return;
        }

        _loadingUi = true;
        var e = _selectedElement;
        SelectedElementTypeText.Text = $"{e.Type} — {e.Name}";
        ElementNameText.Text = e.Name;
        ContentText.Text = e.Text;
        FieldPathCombo.SelectedValue = e.FieldPath;
        if (FieldPathCombo.SelectedValue is null)
            FieldPathCombo.Text = e.FieldPath;
        FormatText.Text = e.Format ?? string.Empty;
        XText.Text = e.Xmm.ToString("0.##");
        YText.Text = e.Ymm.ToString("0.##");
        WidthText.Text = e.WidthMm.ToString("0.##");
        HeightText.Text = e.HeightMm.ToString("0.##");
        FontSizeText.Text = e.FontSize.ToString("0.##");
        BorderText.Text = (e.Type == TemplateElementType.Line ? e.LineThickness : e.BorderThickness).ToString("0.##");
        PaddingText.Text = e.PaddingMm.ToString("0.##");
        CornerRadiusText.Text = e.CornerRadiusMm.ToString("0.##");
        ForegroundText.Text = e.ForegroundColor;
        BackgroundText.Text = e.BackgroundColor ?? string.Empty;
        BorderColorText.Text = e.BorderColor;
        RowHeightText.Text = e.RowHeightMm.ToString("0.##");
        MaxRowsText.Text = e.MaxRows.ToString();
        BoldCheck.IsChecked = e.Bold;
        RtlCheck.IsChecked = e.RightToLeft;
        AlignmentCombo.SelectedValue = e.Alignment.ToString();
        ImagePathText.Text = e.ImagePath ?? string.Empty;
        _loadingUi = false;
    }

    private void ClearElementUi()
    {
        SelectedElementTypeText.Text = "لم يتم تحديد عنصر";
        ElementNameText.Text = ContentText.Text = FormatText.Text = XText.Text = YText.Text = WidthText.Text = HeightText.Text = FontSizeText.Text = BorderText.Text = ImagePathText.Text = string.Empty;
        PaddingText.Text = "1.1";
        CornerRadiusText.Text = "0";
        ForegroundText.Text = "#27263B";
        BackgroundText.Text = string.Empty;
        BorderColorText.Text = "#E7E5EF";
        RowHeightText.Text = "7";
        MaxRowsText.Text = "12";
        FieldPathCombo.SelectedIndex = -1;
        FieldPathCombo.Text = string.Empty;
        BoldCheck.IsChecked = false;
        RtlCheck.IsChecked = true;
        AlignmentCombo.SelectedValue = "Right";
    }

    private void DesignerCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var visual = FindTaggedElement(e.OriginalSource as DependencyObject);
        if (visual?.Tag is not TemplateElement element)
            return;

        SelectElement(element, visual);
        _isDragging = true;
        _dragStart = e.GetPosition(DesignerCanvas);
        _elementStartX = element.Xmm;
        _elementStartY = element.Ymm;
        DesignerCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void DesignerCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _selectedElement is null || _selectedVisual is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var current = e.GetPosition(DesignerCanvas);
        var dxMm = TemplateRenderer.ToMm(current.X - _dragStart.X);
        var dyMm = TemplateRenderer.ToMm(current.Y - _dragStart.Y);
        var x = Math.Max(0, _elementStartX + dxMm);
        var y = Math.Max(0, _elementStartY + dyMm);

        if (SnapCheck.IsChecked == true)
        {
            x = Math.Round(x);
            y = Math.Round(y);
        }

        _selectedElement.Xmm = x;
        _selectedElement.Ymm = y;
        Canvas.SetLeft(_selectedVisual, TemplateRenderer.Mm(x));
        Canvas.SetTop(_selectedVisual, TemplateRenderer.Mm(y));

        _loadingUi = true;
        XText.Text = x.ToString("0.##");
        YText.Text = y.ToString("0.##");
        _loadingUi = false;
    }

    private void DesignerCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        DesignerCanvas.ReleaseMouseCapture();
        StatusText.Text = "تم تحريك العنصر. احفظ القالب لتثبيت التعديل.";
    }

    private FrameworkElement? FindTaggedElement(DependencyObject? start)
    {
        var current = start;
        while (current is not null && current != DesignerCanvas)
        {
            if (current is FrameworkElement fe && fe.Tag is TemplateElement)
                return fe;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void AddElement(TemplateElement element)
    {
        if (_currentTemplate is null)
            return;

        _currentTemplate.Elements.Add(element);
        _selectedElement = element;
        RefreshDesigner();
        LoadElementUi();
        StatusText.Text = "تمت إضافة عنصر جديد.";
    }

    private void AddText_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Text,
        Name = "نص",
        Text = "نص جديد",
        Xmm = 10,
        Ymm = 30,
        WidthMm = 55,
        HeightMm = 9,
        FontSize = 12,
        Alignment = TemplateTextAlignment.Right,
        RightToLeft = true
    });

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        var field = DocumentFieldCatalog.GetFields(_currentTemplate?.DocumentType).FirstOrDefault();
        AddElement(new TemplateElement
        {
            Type = TemplateElementType.Field,
            Name = "حقل بيانات",
            FieldPath = field?.Path ?? "VoucherNumber",
            Xmm = 10,
            Ymm = 42,
            WidthMm = 55,
            HeightMm = 9,
            FontSize = 11,
            BorderThickness = 0
        });
    }

    private void AddTable_Click(object sender, RoutedEventArgs e)
    {
        var isJournal = string.Equals(_currentTemplate?.DocumentType, "JournalEntry", StringComparison.OrdinalIgnoreCase);
        var columns = isJournal
            ? new List<TemplateTableColumn>
            {
                new() { Header = "الحساب", FieldPath = "AccountName", WidthMm = 48 },
                new() { Header = "البيان", FieldPath = "Description", WidthMm = 50 },
                new() { Header = "مدين", FieldPath = "DebitAmount", WidthMm = 25, Format = "N2", Alignment = TemplateTextAlignment.Center },
                new() { Header = "دائن", FieldPath = "CreditAmount", WidthMm = 25, Format = "N2", Alignment = TemplateTextAlignment.Center }
            }
            : new List<TemplateTableColumn>
            {
                new() { Header = "الطرف", FieldPath = "PartyName", WidthMm = 36 },
                new() { Header = "طريقة الدفع", FieldPath = "PaymentMethod", WidthMm = 27 },
                new() { Header = "الصندوق / البنك", FieldPath = "SettlementAccountName", WidthMm = 38 },
                new() { Header = "العملة", FieldPath = "CurrencyCode", WidthMm = 18, Alignment = TemplateTextAlignment.Center },
                new() { Header = "المبلغ", FieldPath = "Amount", WidthMm = 27, Format = "N2", Alignment = TemplateTextAlignment.Center }
            };

        AddElement(new TemplateElement
        {
            Type = TemplateElementType.Table,
            Name = "جدول البنود",
            FieldPath = "Lines",
            Xmm = 10,
            Ymm = 75,
            WidthMm = Math.Min(190, (_currentTemplate?.PaperWidthMm ?? 210) - 20),
            HeightMm = 65,
            BorderThickness = 0.35,
            BorderColor = "#D9E2EC",
            HeaderBackgroundColor = "#123F6D",
            HeaderForegroundColor = "#FFFFFF",
            AlternateRowBackgroundColor = "#F8FBFE",
            RowHeightMm = 8,
            MaxRows = 10,
            FontSize = 10,
            Columns = columns
        });
    }

    private void AddRectangle_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Rectangle,
        Name = "بطاقة",
        Xmm = 10,
        Ymm = 55,
        WidthMm = 60,
        HeightMm = 20,
        BorderThickness = 0.5,
        BorderColor = "#D9E2EC",
        BackgroundColor = "#F7FAFC",
        CornerRadiusMm = 2
    });

    private void AddLine_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Line,
        Name = "خط",
        Xmm = 10,
        Ymm = 55,
        WidthMm = 60,
        HeightMm = 2,
        LineThickness = 1,
        BorderColor = "#123F6D"
    });

    private void AddImage_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Image,
        Name = "شعار",
        Xmm = 10,
        Ymm = 10,
        WidthMm = 35,
        HeightMm = 22
    });

    private void DeleteElement_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null || _selectedElement is null)
            return;

        _currentTemplate.Elements.RemoveAll(x => x.Id == _selectedElement.Id);
        _selectedElement = null;
        RefreshDesigner();
        ClearElementUi();
        StatusText.Text = "تم حذف العنصر من القالب. احفظ لتثبيت التغيير.";
    }

    private void ApplyElement_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElement is null)
            return;

        var item = _selectedElement;
        item.Name = ElementNameText.Text.Trim();
        item.Text = ContentText.Text;
        item.FieldPath = (FieldPathCombo.SelectedValue as string ?? FieldPathCombo.Text).Trim();
        item.Format = string.IsNullOrWhiteSpace(FormatText.Text) ? null : FormatText.Text.Trim();
        item.Xmm = ParseDouble(XText.Text, item.Xmm);
        item.Ymm = ParseDouble(YText.Text, item.Ymm);
        item.WidthMm = Math.Max(1, ParseDouble(WidthText.Text, item.WidthMm));
        item.HeightMm = Math.Max(1, ParseDouble(HeightText.Text, item.HeightMm));
        item.FontSize = Math.Max(5, ParseDouble(FontSizeText.Text, item.FontSize));
        item.PaddingMm = Math.Max(0, ParseDouble(PaddingText.Text, item.PaddingMm));
        item.CornerRadiusMm = Math.Max(0, ParseDouble(CornerRadiusText.Text, item.CornerRadiusMm));
        item.ForegroundColor = NormalizeColor(ForegroundText.Text, item.ForegroundColor);
        item.BackgroundColor = string.IsNullOrWhiteSpace(BackgroundText.Text) ? null : BackgroundText.Text.Trim();
        item.BorderColor = NormalizeColor(BorderColorText.Text, item.BorderColor);
        item.RowHeightMm = Math.Max(3, ParseDouble(RowHeightText.Text, item.RowHeightMm));
        item.MaxRows = Math.Max(1, ParseInt(MaxRowsText.Text, item.MaxRows));

        var thickness = Math.Max(0, ParseDouble(BorderText.Text, item.Type == TemplateElementType.Line ? item.LineThickness : item.BorderThickness));
        if (item.Type == TemplateElementType.Line)
            item.LineThickness = thickness;
        else
            item.BorderThickness = thickness;

        item.Bold = BoldCheck.IsChecked == true;
        item.RightToLeft = RtlCheck.IsChecked == true;
        item.ImagePath = string.IsNullOrWhiteSpace(ImagePathText.Text) ? null : ImagePathText.Text.Trim();
        if (Enum.TryParse<TemplateTextAlignment>(AlignmentCombo.SelectedValue as string, out var alignment))
            item.Alignment = alignment;

        RefreshDesigner();
        LoadElementUi();
        StatusText.Text = "تم تطبيق خصائص العنصر.";
    }

    private void NewTemplate_Click(object sender, RoutedEventArgs e)
    {
        var template = CreateDefaultTemplate();
        _templateStore.Save(template);
        LoadTemplates(template.Id);
        ShowPanel(DesignerPanel, NavDesignerButton);
        StatusText.Text = "تم إنشاء قالب جديد.";
    }

    private void SaveTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null)
            return;

        ApplyTemplateHeaderToModel();
        if (_currentTemplate.IsDefault)
        {
            foreach (var other in _templates.Where(x => x.Id != _currentTemplate.Id &&
                         string.Equals(x.DocumentType, _currentTemplate.DocumentType, StringComparison.OrdinalIgnoreCase) && x.IsDefault))
            {
                other.IsDefault = false;
                _templateStore.Save(other);
            }
        }

        _templateStore.Save(_currentTemplate);
        TemplatesList.Items.Refresh();
        DesignerTemplateCombo.Items.Refresh();
        UpdateTemplateSummary();
        StatusText.Text = "تم حفظ القالب.";
    }

    private void DuplicateTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null)
            return;

        var clone = TemplateSerializer.Deserialize(TemplateSerializer.Serialize(_currentTemplate));
        clone.Id = Guid.NewGuid();
        clone.Code = _currentTemplate.Code + "-COPY-" + DateTime.Now.ToString("HHmmss");
        clone.Name = _currentTemplate.Name + " - نسخة";
        clone.IsDefault = false;
        foreach (var element in clone.Elements)
            element.Id = Guid.NewGuid();

        _templateStore.Save(clone);
        LoadTemplates(clone.Id);
        StatusText.Text = "تم إنشاء نسخة من القالب.";
    }

    private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null)
            return;

        var result = MessageBox.Show($"حذف القالب '{_currentTemplate.Name}'؟", "تأكيد حذف القالب", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        _templateStore.Delete(_currentTemplate);
        _currentTemplate = null;
        LoadTemplates();
        StatusText.Text = "تم حذف القالب.";
    }

    private void OpenSelectedTemplateInDesigner_Click(object sender, RoutedEventArgs e)
    {
        if (TemplatesList.SelectedItem is TemplateDefinition template)
            SelectTemplate(template, loadSampleData: true, synchronizeControls: true);

        ShowPanel(DesignerPanel, NavDesignerButton);
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null)
            return;

        ApplyTemplateHeaderToModel();
        var preview = new PreviewWindow(_renderer.BuildCanvas(_currentTemplate, _previewData, false)) { Owner = this };
        preview.ShowDialog();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null)
            return;

        try
        {
            ApplyTemplateHeaderToModel();
            var printer = PrinterCombo.SelectedItem as string ?? _settings.DefaultPrinterName;
            if (string.IsNullOrWhiteSpace(printer))
                throw new InvalidOperationException("اختر طابعة قبل تنفيذ الطباعة التجريبية.");

            var document = _renderer.BuildDocument(_currentTemplate, _previewData);
            _printerService.Print(document, printer, 1);
            StatusText.Text = $"تم إرسال '{_currentTemplate.Name}' إلى {printer}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "فشلت الطباعة التجريبية.";
        }
    }

    private void LoadData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(dialog.FileName));
            _previewData = doc.RootElement.Clone();
            RefreshDesigner();
            StatusText.Text = "تم تحميل بيانات المعاينة: " + Path.GetFileName(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ملف JSON غير صالح", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) == true)
            ImagePathText.Text = dialog.FileName;
    }

    private void DocumentTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi || _currentTemplate is null || DocumentTypeCombo.SelectedItem is not string value)
            return;

        _currentTemplate.DocumentType = value;
        _previewData = SampleDataStore.Load(value);
        LoadFieldCatalog();
        RefreshDesigner();
    }

    private void TemplateHeader_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || _currentTemplate is null)
            return;

        ApplyTemplateHeaderToModel();
        RefreshDesigner();
    }

    private void TemplateHeaderCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingUi || _currentTemplate is null)
            return;

        ApplyTemplateHeaderToModel();
        RefreshDesigner();
    }

    private void ApplyTemplateHeaderToModel()
    {
        if (_currentTemplate is null)
            return;

        _currentTemplate.Name = string.IsNullOrWhiteSpace(TemplateNameText.Text) ? _currentTemplate.Name : TemplateNameText.Text.Trim();
        _currentTemplate.Code = string.IsNullOrWhiteSpace(TemplateCodeText.Text) ? _currentTemplate.Code : TemplateCodeText.Text.Trim();
        if (DocumentTypeCombo.SelectedItem is string docType)
            _currentTemplate.DocumentType = docType;
        _currentTemplate.PaperWidthMm = Math.Max(30, ParseDouble(PaperWidthText.Text, _currentTemplate.PaperWidthMm));
        _currentTemplate.PaperHeightMm = Math.Max(30, ParseDouble(PaperHeightText.Text, _currentTemplate.PaperHeightMm));
        _currentTemplate.Landscape = LandscapeCheck.IsChecked == true;
        _currentTemplate.IsDefault = DefaultTemplateCheck.IsChecked == true;
        UpdateTemplateSummary();
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowPanel(DashboardPanel, NavDashboardButton);
    private void NavTemplates_Click(object sender, RoutedEventArgs e) => ShowPanel(TemplatesPanel, NavTemplatesButton);
    private void NavDesigner_Click(object sender, RoutedEventArgs e) => ShowPanel(DesignerPanel, NavDesignerButton);
    private void NavJobs_Click(object sender, RoutedEventArgs e) => ShowPanel(JobsPanel, NavJobsButton);
    private void NavPrinters_Click(object sender, RoutedEventArgs e) => ShowPanel(PrintersPanel, NavPrintersButton);
    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        LoadSettingsUi();
        ShowPanel(SettingsPanel, NavSettingsButton);
    }

    private void ShowPanel(UIElement panel, Button activeButton)
    {
        DashboardPanel.Visibility = Visibility.Collapsed;
        TemplatesPanel.Visibility = Visibility.Collapsed;
        DesignerPanel.Visibility = Visibility.Collapsed;
        JobsPanel.Visibility = Visibility.Collapsed;
        PrintersPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        panel.Visibility = Visibility.Visible;

        foreach (var button in new[] { NavDashboardButton, NavTemplatesButton, NavDesignerButton, NavJobsButton, NavPrintersButton, NavSettingsButton })
        {
            button.Background = Brushes.Transparent;
            button.Foreground = (Brush)FindResource("OasTextBrush");
            button.BorderBrush = Brushes.Transparent;
        }

        activeButton.Background = (Brush)FindResource("OasPrimarySoftBrush");
        activeButton.Foreground = (Brush)FindResource("OasPrimaryBrush");
        activeButton.BorderBrush = (Brush)FindResource("OasPrimaryBorderBrush");
    }

    private void SetZoom(double percent)
    {
        percent = Math.Clamp(percent, 25, 200);
        _zoomPercent = percent;
        var scale = percent / 100d;
        DesignerScaleTransform.ScaleX = scale;
        DesignerScaleTransform.ScaleY = scale;

        if (Math.Abs(ZoomSlider.Value - percent) > 0.01)
            ZoomSlider.Value = percent;
        ZoomText.Text = $"{percent:0}%";
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;
        SetZoom(e.NewValue);
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomPercent + 10);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomPercent - 10);
    private void ActualSize_Click(object sender, RoutedEventArgs e) => SetZoom(100);

    private void FitToScreen_Click(object sender, RoutedEventArgs e)
    {
        if (DesignerCanvas.Width <= 0 || DesignerCanvas.Height <= 0)
            return;

        var availableWidth = DesignerScrollViewer.ViewportWidth - 40;
        var availableHeight = DesignerScrollViewer.ViewportHeight - 40;
        if (availableWidth <= 0 || availableHeight <= 0)
            return;

        var scale = Math.Min(availableWidth / DesignerCanvas.Width, availableHeight / DesignerCanvas.Height);
        SetZoom(Math.Clamp(scale * 100d, 25d, 200d));
    }

    private void DesignerScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        SetZoom(_zoomPercent + (e.Delta > 0 ? 10 : -10));
        e.Handled = true;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.S)
        {
            SaveTemplate_Click(sender, e);
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.P)
        {
            Preview_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && DesignerPanel.Visibility == Visibility.Visible && _selectedElement is not null)
        {
            DeleteElement_Click(sender, e);
            e.Handled = true;
        }
    }

    private void RefreshPrinters_Click(object sender, RoutedEventArgs e)
    {
        LoadPrinters();
        StatusText.Text = "تم تحديث قائمة الطابعات.";
    }

    private void PrinterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrinterCombo.SelectedItem is not string printer)
            return;
        SidebarPrinterText.Text = printer;
    }

    private void SaveDefaultPrinter_Click(object sender, RoutedEventArgs e)
    {
        if (DefaultPrinterCombo.SelectedItem is not string printer || string.IsNullOrWhiteSpace(printer))
        {
            MessageBox.Show("اختر طابعة افتراضية أولاً.", "الطابعات", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _settings.DefaultPrinterName = printer;
        _settingsStore.Save(_settings);
        PrinterCombo.SelectedItem = printer;
        SettingsPrinterCombo.SelectedItem = printer;
        UpdateHeaderAndDashboard();
        StatusText.Text = "تم حفظ الطابعة الافتراضية.";
    }

    private void LoadSettingsUi()
    {
        SettingsApiUrlText.Text = _settings.ApiBaseUrl;
        SettingsWorkstationText.Text = _settings.WorkstationCode;
        SettingsOperatorText.Text = _settings.OperatorName;
        SettingsApiKeyBox.Password = _settings.ApiKey;
        SettingsPollCheck.IsChecked = _settings.PollEnabled;
        SettingsIntervalText.Text = _settings.PollIntervalSeconds.ToString();
        SettingsPrinterCombo.SelectedItem = _settings.DefaultPrinterName;
        SettingsReceiptPrinterCombo.SelectedItem = _settings.PrinterBindings.TryGetValue("ReceiptVoucher", out var receiptPrinter) ? receiptPrinter : null;
        SettingsPaymentPrinterCombo.SelectedItem = _settings.PrinterBindings.TryGetValue("PaymentVoucher", out var paymentPrinter) ? paymentPrinter : null;
    }

    private void SaveSettingsInline_Click(object sender, RoutedEventArgs e)
    {
        var interval = ParseInt(SettingsIntervalText.Text, _settings.PollIntervalSeconds);
        _settings.ApiBaseUrl = SettingsApiUrlText.Text.Trim().TrimEnd('/');
        _settings.WorkstationCode = string.IsNullOrWhiteSpace(SettingsWorkstationText.Text) ? "DEFAULT" : SettingsWorkstationText.Text.Trim();
        _settings.OperatorName = string.IsNullOrWhiteSpace(SettingsOperatorText.Text) ? Environment.UserName : SettingsOperatorText.Text.Trim();
        _settings.ApiKey = SettingsApiKeyBox.Password;
        _settings.PollEnabled = SettingsPollCheck.IsChecked == true;
        _settings.PollIntervalSeconds = Math.Clamp(interval, 1, 60);
        _settings.DefaultPrinterName = SettingsPrinterCombo.SelectedItem as string ?? _settings.DefaultPrinterName;
        SetPrinterBinding("ReceiptVoucher", SettingsReceiptPrinterCombo.SelectedItem as string);
        SetPrinterBinding("PaymentVoucher", SettingsPaymentPrinterCombo.SelectedItem as string);

        _settingsStore.Save(_settings);
        _jobPolling.Start(_settings);
        LoadSettingsUi();
        UpdateHeaderAndDashboard();
        StatusText.Text = "تم حفظ الإعدادات وإعادة تشغيل اتصال الطباعة.";
    }


    private void SetPrinterBinding(string documentType, string? printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            _settings.PrinterBindings.Remove(documentType);
        else
            _settings.PrinterBindings[documentType] = printerName;
    }

    private void ReloadSettings_Click(object sender, RoutedEventArgs e)
    {
        _settings = _settingsStore.Load();
        _settings.PrinterBindings ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        LoadSettingsUi();
        UpdateHeaderAndDashboard();
        StatusText.Text = "تمت إعادة تحميل الإعدادات.";
    }

    private void UpdateHeaderAndDashboard()
    {
        UserText.Text = !string.IsNullOrWhiteSpace(_lastRequestedBy)
            ? _lastRequestedBy
            : string.IsNullOrWhiteSpace(_settings.OperatorName)
                ? Environment.UserName
                : _settings.OperatorName;
        SidebarWorkstationText.Text = string.IsNullOrWhiteSpace(_settings.WorkstationCode) ? "DEFAULT" : _settings.WorkstationCode;
        SidebarPrinterText.Text = string.IsNullOrWhiteSpace(_settings.DefaultPrinterName) ? "لم تحدد طابعة" : _settings.DefaultPrinterName;
        DashboardPrinterText.Text = string.IsNullOrWhiteSpace(_settings.DefaultPrinterName) ? "لم تحدد طابعة" : _settings.DefaultPrinterName;
        DashboardApiText.Text = string.IsNullOrWhiteSpace(_settings.ApiBaseUrl) ? "غير محدد" : _settings.ApiBaseUrl;
        TemplatesCountText.Text = _templates.Count.ToString();
        JobsCountText.Text = _jobHistory.Count.ToString();
        SuccessJobsCountText.Text = _jobHistory.Count(x => x.Success).ToString();
    }

    private async Task<bool> HandlePrintJobAsync(PrintJob job) =>
        await Dispatcher.InvokeAsync(() => ExecutePrintJob(job));

    private bool ExecutePrintJob(PrintJob job)
    {
        var receivedAt = DateTimeOffset.Now;
        string? printer = null;
        string? error = null;
        var success = false;

        try
        {
            var template = !string.IsNullOrWhiteSpace(job.TemplateCode)
                ? _templates.FirstOrDefault(x => string.Equals(x.Code, job.TemplateCode, StringComparison.OrdinalIgnoreCase))
                : null;

            template ??= _templates.FirstOrDefault(x => string.Equals(x.DocumentType, job.DocumentType, StringComparison.OrdinalIgnoreCase) && x.IsDefault);
            template ??= _templates.FirstOrDefault(x => string.Equals(x.DocumentType, job.DocumentType, StringComparison.OrdinalIgnoreCase));
            if (template is null)
                throw new InvalidOperationException($"لا يوجد قالب للمستند {job.DocumentType}.");

            // Keep the designer synchronized with the real payload received from OAS.
            _currentTemplate = template;
            _selectedElement = null;
            _previewData = job.Data.Clone();
            _loadingUi = true;
            TemplatesList.SelectedItem = template;
            DesignerTemplateCombo.SelectedItem = template;
            _loadingUi = false;
            LoadTemplateUi();
            RefreshDesigner();

            printer = job.PrinterName;
            if (string.IsNullOrWhiteSpace(printer) && _settings.PrinterBindings.TryGetValue(job.DocumentType, out var binding))
                printer = binding;
            if (string.IsNullOrWhiteSpace(printer))
                printer = _settings.DefaultPrinterName;
            if (string.IsNullOrWhiteSpace(printer))
                printer = PrinterCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(printer))
                throw new InvalidOperationException("لا توجد طابعة محددة لمهمة الطباعة.");

            if (PrinterCombo.Items.Cast<string>().Any(x => string.Equals(x, printer, StringComparison.OrdinalIgnoreCase)))
                PrinterCombo.SelectedItem = printer;

            if (!string.IsNullOrWhiteSpace(job.RequestedBy))
            {
                _lastRequestedBy = job.RequestedBy;
                UserText.Text = job.RequestedBy;
            }

            StatusText.Text = $"جاري تنفيذ {GetDocumentTypeArabicName(job.DocumentType)} — {GetDocumentNumber(job.Data)}";
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            if (job.ShowPreview)
            {
                var preview = new PreviewWindow(_renderer.BuildCanvas(template, job.Data, false)) { Owner = this };
                preview.ShowDialog();
            }
            else
            {
                _printerService.Print(_renderer.BuildDocument(template, job.Data), printer, Math.Max(1, job.Copies));
            }

            // Keep the last real document visible in the designer after execution.
            _previewData = job.Data.Clone();
            RefreshDesigner();
            StatusText.Text = $"تم تنفيذ مهمة الطباعة {job.JobId}.";
            success = true;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            StatusText.Text = "فشل تنفيذ مهمة الطباعة: " + ex.Message;
            return false;
        }
        finally
        {
            AddJobHistory(new PrintJobHistoryItem
            {
                JobId = job.JobId,
                ReceivedAt = receivedAt,
                DocumentType = job.DocumentType,
                DocumentName = GetDocumentTypeArabicName(job.DocumentType),
                DocumentNumber = GetDocumentNumber(job.Data),
                PrinterName = printer ?? "-",
                Copies = Math.Max(1, job.Copies),
                Success = success,
                Error = error
            });
        }
    }

    private void AddJobHistory(PrintJobHistoryItem item)
    {
        _jobHistory.Insert(0, item);
        while (_jobHistory.Count > 100)
            _jobHistory.RemoveAt(_jobHistory.Count - 1);
        UpdateHeaderAndDashboard();
    }

    private static string GetDocumentNumber(JsonElement data)
    {
        var voucher = JsonValueResolver.ResolveText(data, "VoucherNumber");
        if (!string.IsNullOrWhiteSpace(voucher))
            return voucher;
        var expense = JsonValueResolver.ResolveText(data, "ExpenseNumber");
        if (!string.IsNullOrWhiteSpace(expense))
            return expense;
        var journal = JsonValueResolver.ResolveText(data, "JournalNumber");
        if (!string.IsNullOrWhiteSpace(journal))
            return journal;
        var payslip = JsonValueResolver.ResolveText(data, "PayslipNumber");
        return string.IsNullOrWhiteSpace(payslip) ? "-" : payslip;
    }

    private static string GetDocumentTypeArabicName(string? documentType) => documentType switch
    {
        "ReceiptVoucher" => "سند قبض",
        "PaymentVoucher" => "سند صرف",
        "Payslip" => "قسيمة راتب",
        "Expense" => "مستند مصروف",
        "JournalEntry" => "قيد يومية",
        _ => documentType ?? "مستند"
    };

    private static string NormalizeColor(string? text, string fallback) =>
        string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();

    private static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, out var value) ? value : fallback;

    private static double ParseDouble(string? text, double fallback) =>
        double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var value)
            || double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value)
            ? value
            : fallback;
}
