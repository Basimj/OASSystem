using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsStore.Load();
        DocumentTypeCombo.ItemsSource = DocumentFieldCatalog.DocumentTypes;
        LoadPrinters();
        LoadTemplates();

        _jobPolling.JobReceived += HandlePrintJobAsync;
        _jobPolling.StatusChanged += message => Dispatcher.Invoke(() => ConnectionText.Text = message);
        _jobPolling.Start(_settings);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e) => _jobPolling.Dispose();

    private void LoadPrinters()
    {
        try
        {
            var printers = _printerService.GetPrinters();
            PrinterCombo.ItemsSource = printers;
            var preferred = !string.IsNullOrWhiteSpace(_settings.DefaultPrinterName) ? _settings.DefaultPrinterName : printers.FirstOrDefault();
            PrinterCombo.SelectedItem = preferred;
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
        TemplatesList.ItemsSource = _templates;

        if (_templates.Count == 0)
        {
            var template = CreateDefaultTemplate();
            _templateStore.Save(template);
            _templates.Add(template);
        }

        TemplatesList.SelectedItem = selectId.HasValue
            ? _templates.FirstOrDefault(x => x.Id == selectId.Value) ?? _templates[0]
            : _templates[0];
    }

    private TemplateDefinition CreateDefaultTemplate()
    {
        return new TemplateDefinition
        {
            Code = "TPL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Name = "قالب جديد",
            DocumentType = "ReceiptVoucher",
            PaperWidthMm = 148,
            PaperHeightMm = 210,
            Elements =
            [
                new TemplateElement
                {
                    Type = TemplateElementType.Text, Name = "العنوان", Text = "سند قبض",
                    Xmm = 14, Ymm = 12, WidthMm = 120, HeightMm = 12, FontSize = 20, Bold = true,
                    Alignment = TemplateTextAlignment.Center
                }
            ]
        };
    }

    private void TemplatesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TemplatesList.SelectedItem is not TemplateDefinition template) return;
        _currentTemplate = template;
        _selectedElement = null;
        _previewData = SampleDataStore.Load(template.DocumentType);
        LoadTemplateUi();
        RefreshDesigner();
    }

    private void LoadTemplateUi()
    {
        if (_currentTemplate is null) return;
        _loadingUi = true;
        TemplateNameText.Text = _currentTemplate.Name;
        TemplateCodeText.Text = _currentTemplate.Code;
        DocumentTypeCombo.SelectedItem = _currentTemplate.DocumentType;
        PaperWidthText.Text = _currentTemplate.PaperWidthMm.ToString("0.##");
        PaperHeightText.Text = _currentTemplate.PaperHeightMm.ToString("0.##");
        LoadFieldCatalog();
        ClearElementUi();
        _loadingUi = false;
    }

    private void LoadFieldCatalog()
    {
        var documentType = _currentTemplate?.DocumentType ?? DocumentTypeCombo.SelectedItem as string;
        FieldPathCombo.ItemsSource = DocumentFieldCatalog.GetFields(documentType);
    }

    private void RefreshDesigner()
    {
        if (_currentTemplate is null) return;
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
        if (FieldPathCombo.SelectedValue is null) FieldPathCombo.Text = e.FieldPath;
        FormatText.Text = e.Format ?? string.Empty;
        XText.Text = e.Xmm.ToString("0.##");
        YText.Text = e.Ymm.ToString("0.##");
        WidthText.Text = e.WidthMm.ToString("0.##");
        HeightText.Text = e.HeightMm.ToString("0.##");
        FontSizeText.Text = e.FontSize.ToString("0.##");
        BorderText.Text = (e.Type == TemplateElementType.Line ? e.LineThickness : e.BorderThickness).ToString("0.##");
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
        FieldPathCombo.SelectedIndex = -1;
        FieldPathCombo.Text = string.Empty;
        BoldCheck.IsChecked = false;
        RtlCheck.IsChecked = true;
        AlignmentCombo.SelectedValue = "Right";
    }

    private void DesignerCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var visual = FindTaggedElement(e.OriginalSource as DependencyObject);
        if (visual?.Tag is not TemplateElement element) return;

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
        _selectedElement.Xmm = Math.Max(0, _elementStartX + dxMm);
        _selectedElement.Ymm = Math.Max(0, _elementStartY + dyMm);
        Canvas.SetLeft(_selectedVisual, TemplateRenderer.Mm(_selectedElement.Xmm));
        Canvas.SetTop(_selectedVisual, TemplateRenderer.Mm(_selectedElement.Ymm));

        _loadingUi = true;
        XText.Text = _selectedElement.Xmm.ToString("0.##");
        YText.Text = _selectedElement.Ymm.ToString("0.##");
        _loadingUi = false;
    }

    private void DesignerCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
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
        if (_currentTemplate is null) return;
        _currentTemplate.Elements.Add(element);
        _selectedElement = element;
        RefreshDesigner();
        LoadElementUi();
        StatusText.Text = "تمت إضافة عنصر جديد.";
    }

    private void AddText_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Text, Name = "نص", Text = "نص جديد", Xmm = 10, Ymm = 30, WidthMm = 55, HeightMm = 9,
        FontSize = 12, Alignment = TemplateTextAlignment.Right, RightToLeft = true
    });

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        var field = DocumentFieldCatalog.GetFields(_currentTemplate?.DocumentType).FirstOrDefault();
        AddElement(new TemplateElement
        {
            Type = TemplateElementType.Field, Name = "حقل بيانات", FieldPath = field?.Path ?? "VoucherNumber",
            Xmm = 10, Ymm = 42, WidthMm = 55, HeightMm = 9, FontSize = 11, BorderThickness = 0
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
                new() { Header = "الحساب", FieldPath = "AccountName", WidthMm = 40 },
                new() { Header = "البيان", FieldPath = "Description", WidthMm = 60 },
                new() { Header = "المبلغ", FieldPath = "Amount", WidthMm = 28, Format = "N2", Alignment = TemplateTextAlignment.Center }
            };

        AddElement(new TemplateElement
        {
            Type = TemplateElementType.Table, Name = "جدول البنود", FieldPath = "Lines", Xmm = 10, Ymm = 75,
            WidthMm = Math.Min(128, (_currentTemplate?.PaperWidthMm ?? 148) - 20), HeightMm = 55,
            BorderThickness = 0.5, RowHeightMm = 7, MaxRows = 8, Columns = columns
        });
    }

    private void AddRectangle_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Rectangle, Name = "مستطيل", Xmm = 10, Ymm = 55, WidthMm = 60, HeightMm = 20, BorderThickness = 1
    });

    private void AddLine_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Line, Name = "خط", Xmm = 10, Ymm = 55, WidthMm = 60, HeightMm = 2, LineThickness = 1
    });

    private void AddImage_Click(object sender, RoutedEventArgs e) => AddElement(new TemplateElement
    {
        Type = TemplateElementType.Image, Name = "شعار", Xmm = 10, Ymm = 10, WidthMm = 35, HeightMm = 22
    });

    private void DeleteElement_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null || _selectedElement is null) return;
        _currentTemplate.Elements.RemoveAll(x => x.Id == _selectedElement.Id);
        _selectedElement = null;
        RefreshDesigner();
        ClearElementUi();
    }

    private void ApplyElement_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElement is null) return;
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
        var thickness = Math.Max(0, ParseDouble(BorderText.Text, item.Type == TemplateElementType.Line ? item.LineThickness : item.BorderThickness));
        if (item.Type == TemplateElementType.Line) item.LineThickness = thickness; else item.BorderThickness = thickness;
        item.Bold = BoldCheck.IsChecked == true;
        item.RightToLeft = RtlCheck.IsChecked == true;
        item.ImagePath = string.IsNullOrWhiteSpace(ImagePathText.Text) ? null : ImagePathText.Text.Trim();
        if (Enum.TryParse<TemplateTextAlignment>(AlignmentCombo.SelectedValue as string, out var alignment)) item.Alignment = alignment;
        RefreshDesigner();
        LoadElementUi();
        StatusText.Text = "تم تطبيق خصائص العنصر.";
    }

    private void NewTemplate_Click(object sender, RoutedEventArgs e)
    {
        var template = CreateDefaultTemplate();
        _templateStore.Save(template);
        LoadTemplates(template.Id);
        StatusText.Text = "تم إنشاء قالب جديد.";
    }

    private void SaveTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null) return;
        ApplyTemplateHeaderToModel();
        _templateStore.Save(_currentTemplate);
        TemplatesList.Items.Refresh();
        StatusText.Text = "تم حفظ القالب.";
    }

    private void DuplicateTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null) return;
        var clone = TemplateSerializer.Deserialize(TemplateSerializer.Serialize(_currentTemplate));
        clone.Id = Guid.NewGuid();
        clone.Code = _currentTemplate.Code + "-COPY-" + DateTime.Now.ToString("HHmmss");
        clone.Name = _currentTemplate.Name + " - نسخة";
        foreach (var element in clone.Elements) element.Id = Guid.NewGuid();
        _templateStore.Save(clone);
        LoadTemplates(clone.Id);
    }

    private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null) return;
        var result = MessageBox.Show($"حذف القالب '{_currentTemplate.Name}'؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _templateStore.Delete(_currentTemplate);
        LoadTemplates();
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null) return;
        ApplyTemplateHeaderToModel();
        var preview = new PreviewWindow(_renderer.BuildCanvas(_currentTemplate, _previewData, false)) { Owner = this };
        preview.ShowDialog();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTemplate is null) return;
        try
        {
            ApplyTemplateHeaderToModel();
            var printer = PrinterCombo.SelectedItem as string ?? _settings.DefaultPrinterName;
            var document = _renderer.BuildDocument(_currentTemplate, _previewData);
            _printerService.Print(document, printer, 1);
            StatusText.Text = $"تم إرسال '{_currentTemplate.Name}' إلى الطابعة.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != true) return;
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
        if (dialog.ShowDialog(this) == true) ImagePathText.Text = dialog.FileName;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var printers = PrinterCombo.ItemsSource?.Cast<string>().ToArray() ?? [];
        var window = new SettingsWindow(_settings, printers) { Owner = this };
        if (window.ShowDialog() != true) return;
        _settings = window.Settings;
        _settingsStore.Save(_settings);
        if (!string.IsNullOrWhiteSpace(_settings.DefaultPrinterName)) PrinterCombo.SelectedItem = _settings.DefaultPrinterName;
        _jobPolling.Start(_settings);
        StatusText.Text = "تم حفظ الإعدادات.";
    }

    private void DocumentTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi || _currentTemplate is null || DocumentTypeCombo.SelectedItem is not string value) return;
        _currentTemplate.DocumentType = value;
        _previewData = SampleDataStore.Load(value);
        LoadFieldCatalog();
        RefreshDesigner();
    }

    private void TemplateHeader_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || _currentTemplate is null) return;
        ApplyTemplateHeaderToModel();
        RefreshDesigner();
    }

    private void ApplyTemplateHeaderToModel()
    {
        if (_currentTemplate is null) return;
        _currentTemplate.Name = string.IsNullOrWhiteSpace(TemplateNameText.Text) ? _currentTemplate.Name : TemplateNameText.Text.Trim();
        _currentTemplate.Code = string.IsNullOrWhiteSpace(TemplateCodeText.Text) ? _currentTemplate.Code : TemplateCodeText.Text.Trim();
        if (DocumentTypeCombo.SelectedItem is string docType) _currentTemplate.DocumentType = docType;
        _currentTemplate.PaperWidthMm = Math.Max(30, ParseDouble(PaperWidthText.Text, _currentTemplate.PaperWidthMm));
        _currentTemplate.PaperHeightMm = Math.Max(30, ParseDouble(PaperHeightText.Text, _currentTemplate.PaperHeightMm));
    }

    private async Task<bool> HandlePrintJobAsync(PrintJob job)
    {
        return await Dispatcher.InvokeAsync(() => ExecutePrintJob(job));
    }

    private bool ExecutePrintJob(PrintJob job)
    {
        try
        {
            // 1) البحث عن القالب المطلوب
            var template = !string.IsNullOrWhiteSpace(job.TemplateCode)
                ? _templates.FirstOrDefault(x =>
                    string.Equals(
                        x.Code,
                        job.TemplateCode,
                        StringComparison.OrdinalIgnoreCase))
                : null;

            // القالب الافتراضي لنوع المستند
            template ??= _templates.FirstOrDefault(x =>
                string.Equals(
                    x.DocumentType,
                    job.DocumentType,
                    StringComparison.OrdinalIgnoreCase)
                && x.IsDefault);

            // أي قالب من نفس النوع
            template ??= _templates.FirstOrDefault(x =>
                string.Equals(
                    x.DocumentType,
                    job.DocumentType,
                    StringComparison.OrdinalIgnoreCase));

            if (template is null)
            {
                throw new InvalidOperationException(
                    $"لا يوجد قالب للمستند {job.DocumentType}.");
            }

            // ----------------------------------------------------
            // 2) عرض القالب الصحيح داخل المصمم
            // ----------------------------------------------------

            if (!ReferenceEquals(TemplatesList.SelectedItem, template))
            {
                TemplatesList.SelectedItem = template;
                TemplatesList.ScrollIntoView(template);
            }

            _currentTemplate = template;

            // مهم:
            // استخدام البيانات الحقيقية القادمة من OAS
            // وليس SampleData
            _previewData = job.Data.Clone();

            // تحديث خصائص القالب
            LoadTemplateUi();

            // إعادة وضع البيانات الحقيقية لأن LoadTemplateUi
            // لا يفترض أن يغيرها، ولكن نثبتها هنا احتياطياً.
            _previewData = job.Data.Clone();

            // إعادة رسم القالب بالبيانات الحقيقية
            RefreshDesigner();

            // ----------------------------------------------------
            // 3) تحديد الطابعة
            // ----------------------------------------------------

            var printer = job.PrinterName;

            if (string.IsNullOrWhiteSpace(printer) &&
                _settings.PrinterBindings.TryGetValue(
                    job.DocumentType,
                    out var binding))
            {
                printer = binding;
            }

            if (string.IsNullOrWhiteSpace(printer))
            {
                printer = _settings.DefaultPrinterName;
            }

            if (string.IsNullOrWhiteSpace(printer))
            {
                printer = PrinterCombo.SelectedItem as string;
            }

            if (string.IsNullOrWhiteSpace(printer))
            {
                throw new InvalidOperationException(
                    "لا توجد طابعة محددة لمهمة الطباعة.");
            }

            // إظهار الطابعة المستخدمة في الواجهة
            if (PrinterCombo.Items
                .Cast<string>()
                .Any(x => string.Equals(
                    x,
                    printer,
                    StringComparison.OrdinalIgnoreCase)))
            {
                PrinterCombo.SelectedItem = printer;
            }

            // ----------------------------------------------------
            // 4) تحديث حالة البرنامج قبل الطباعة
            // ----------------------------------------------------

            StatusText.Text =
                $"جاري تنفيذ {GetDocumentTypeArabicName(job.DocumentType)}"
                + $" - المهمة {job.JobId}";

            // إجبار WPF على تحديث الشاشة قبل فتح المعاينة/الطباعة
            Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Render,
                new Action(() => { }));

            // ----------------------------------------------------
            // 5) معاينة أو طباعة
            // ----------------------------------------------------

            if (job.ShowPreview)
            {
                var previewCanvas =
                    _renderer.BuildCanvas(
                        template,
                        job.Data,
                        false);

                var preview = new PreviewWindow(previewCanvas)
                {
                    Owner = this
                };

                preview.ShowDialog();
            }
            else
            {
                var document =
                    _renderer.BuildDocument(
                        template,
                        job.Data);

                _printerService.Print(
                    document,
                    printer,
                    Math.Max(1, job.Copies));
            }

            // ----------------------------------------------------
            // 6) إبقاء البيانات الحقيقية ظاهرة بعد الطباعة
            // ----------------------------------------------------

            _previewData = job.Data.Clone();
            RefreshDesigner();

            StatusText.Text =
                $"تم تنفيذ مهمة الطباعة {job.JobId}.";

            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text =
                "فشل تنفيذ مهمة الطباعة: " + ex.Message;

            MessageBox.Show(
                ex.Message,
                "خطأ في تنفيذ الطباعة",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return false;
        }
    }
    private static string GetDocumentTypeArabicName(string documentType)
    {
        return documentType switch
        {
            "ReceiptVoucher" => "سند قبض",
            "PaymentVoucher" => "سند صرف",
            "Expense" => "مستند مصروف",
            "JournalEntry" => "قيد يومية",
            _ => documentType
        };
    }
    private void SetZoom(double percent)
    {
        percent = Math.Clamp(percent, 25, 200);

        _zoomPercent = percent;

        var scale = percent / 100.0;

        DesignerScaleTransform.ScaleX = scale;
        DesignerScaleTransform.ScaleY = scale;

        if (ZoomSlider is not null &&
            Math.Abs(ZoomSlider.Value - percent) > 0.01)
        {
            ZoomSlider.Value = percent;
        }

        if (ZoomText is not null)
            ZoomText.Text = $"{percent:0}%";
    }

    private void ZoomSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
            return;

        SetZoom(e.NewValue);
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomPercent + 10);
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomPercent - 10);
    }

    private void ActualSize_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(100);
    }

    private void FitToScreen_Click(object sender, RoutedEventArgs e)
    {
        if (DesignerCanvas.Width <= 0 ||
            DesignerCanvas.Height <= 0)
        {
            return;
        }

        var availableWidth =
            DesignerScrollViewer.ViewportWidth - 20;

        var availableHeight =
            DesignerScrollViewer.ViewportHeight - 20;

        if (availableWidth <= 0 ||
            availableHeight <= 0)
        {
            return;
        }

        var widthScale =
            availableWidth / DesignerCanvas.Width;

        var heightScale =
            availableHeight / DesignerCanvas.Height;

        var scale =
            Math.Min(widthScale, heightScale);

        SetZoom(scale * 100);
    }

    private void DesignerScrollViewer_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        if (e.Delta > 0)
            SetZoom(_zoomPercent + 10);
        else
            SetZoom(_zoomPercent - 10);

        e.Handled = true;
    }
    private static double ParseDouble(string? text, double fallback) =>
        double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var value)
            || double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value)
            ? value
            : fallback;
}
