using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OAS.Printing.Core.Models;
using OAS.Printing.Core.Services;

namespace OAS.Print.Desktop.Services;

public sealed class TemplateRenderer
{
    public const double DipPerMm = 96d / 25.4d;

    public Canvas BuildCanvas(TemplateDefinition template, JsonElement data, bool designMode = false) =>
        BuildCanvasCore(template, data, designMode, null, null);

    private Canvas BuildCanvasCore(
        TemplateDefinition template,
        JsonElement data,
        bool designMode,
        TablePageSlice? tableSlice,
        Func<TemplateElement, bool>? includeElement)
    {
        var canvas = new Canvas
        {
            Width = Mm(template.Landscape ? template.PaperHeightMm : template.PaperWidthMm),
            Height = Mm(template.Landscape ? template.PaperWidthMm : template.PaperHeightMm),
            Background = Brushes.White,
            FlowDirection = FlowDirection.LeftToRight,
            ClipToBounds = true
        };

        foreach (var element in template.Elements)
        {
            if (includeElement is not null && !includeElement(element))
                continue;

            var visual = BuildElement(element, data, designMode, tableSlice);
            visual.Tag = element;
            Canvas.SetLeft(visual, Mm(element.Xmm));
            Canvas.SetTop(visual, Mm(element.Ymm));
            canvas.Children.Add(visual);
        }

        return canvas;
    }

    public FixedDocument BuildDocument(TemplateDefinition template, JsonElement data)
    {
        var pageWidth = Mm(template.Landscape ? template.PaperHeightMm : template.PaperWidthMm);
        var pageHeight = Mm(template.Landscape ? template.PaperWidthMm : template.PaperHeightMm);
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);

        // Voucher line tables can exceed the visual capacity of one page. Instead of silently
        // dropping rows after MaxRows, split the main Lines table across pages and keep the
        // totals/signatures on the final page only. This preserves every voucher line while
        // keeping the compact template layout intact.
        var pagedTable = template.Elements
            .FirstOrDefault(x =>
                x.Type == TemplateElementType.Table &&
                string.Equals(x.FieldPath, "Lines", StringComparison.OrdinalIgnoreCase));

        var items = pagedTable is null
            ? null
            : JsonValueResolver.ResolveElement(data, pagedTable.FieldPath);

        if (pagedTable is null || items is not { ValueKind: JsonValueKind.Array })
        {
            AddPage(document, pageWidth, pageHeight, BuildCanvasCore(template, data, false, null, null));
            return document;
        }

        var totalRows = items.Value.GetArrayLength();
        var rowsPerPage = Math.Max(1, pagedTable.MaxRows);
        if (totalRows <= rowsPerPage)
        {
            AddPage(document, pageWidth, pageHeight, BuildCanvasCore(template, data, false, null, null));
            return document;
        }

        var pageCount = (int)Math.Ceiling(totalRows / (double)rowsPerPage);
        var tableBottom = pagedTable.Ymm + pagedTable.HeightMm;

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var isLastPage = pageIndex == pageCount - 1;
            var slice = new TablePageSlice(pagedTable.Id, pageIndex * rowsPerPage, rowsPerPage);

            bool IncludeElement(TemplateElement element) =>
                isLastPage || element.Ymm <= tableBottom + 0.01;

            var canvas = BuildCanvasCore(template, data, false, slice, IncludeElement);
            AddPage(document, pageWidth, pageHeight, canvas);
        }

        return document;
    }

    private static void AddPage(
        FixedDocument document,
        double pageWidth,
        double pageHeight,
        Canvas canvas)
    {
        var page = new FixedPage
        {
            Width = pageWidth,
            Height = pageHeight,
            Background = Brushes.White
        };

        page.Children.Add(canvas);
        var content = new PageContent();
        ((IAddChild)content).AddChild(page);
        document.Pages.Add(content);
    }

    private FrameworkElement BuildElement(
        TemplateElement element,
        JsonElement data,
        bool designMode,
        TablePageSlice? tableSlice) =>
        element.Type switch
        {
            TemplateElementType.Text => BuildText(element, element.Text, designMode),
            TemplateElementType.Field => BuildText(
                element,
                element.Prefix + JsonValueResolver.ResolveText(data, element.FieldPath, element.Format) + element.Suffix,
                designMode),
            TemplateElementType.Rectangle => BuildRectangle(element, designMode),
            TemplateElementType.Line => BuildLine(element, designMode),
            TemplateElementType.Image => BuildImage(element, designMode),
            TemplateElementType.Table => BuildTable(element, data, designMode, tableSlice),
            _ => BuildText(element, element.Text, designMode)
        };

    private FrameworkElement BuildText(TemplateElement element, string text, bool designMode)
    {
        var block = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(string.IsNullOrWhiteSpace(element.FontFamily) ? "Segoe UI" : element.FontFamily),
            FontSize = element.FontSize,
            FontWeight = element.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = element.Italic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = BrushFrom(element.ForegroundColor, Brushes.Black),
            TextAlignment = ToTextAlignment(element.Alignment),
            FlowDirection = element.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Padding = new Thickness(Mm(Math.Max(0, element.PaddingMm)))
        };

        var borderThickness = designMode && element.BorderThickness <= 0
            ? new Thickness(0.65)
            : new Thickness(Math.Max(0, element.BorderThickness));

        return new Border
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            Background = BrushFrom(element.BackgroundColor, Brushes.Transparent),
            BorderBrush = designMode && element.BorderThickness <= 0
                ? Brushes.SteelBlue
                : BrushFrom(element.BorderColor, Brushes.Black),
            BorderThickness = borderThickness,
            CornerRadius = new CornerRadius(Mm(Math.Max(0, element.CornerRadiusMm))),
            ClipToBounds = true,
            Child = block
        };
    }

    private FrameworkElement BuildRectangle(TemplateElement element, bool designMode) => new Border
    {
        Width = Mm(element.WidthMm),
        Height = Mm(element.HeightMm),
        Background = BrushFrom(element.BackgroundColor, Brushes.Transparent),
        BorderBrush = designMode && element.BorderThickness <= 0
            ? Brushes.SteelBlue
            : BrushFrom(element.BorderColor, Brushes.Black),
        BorderThickness = new Thickness(Math.Max(designMode ? 0.65 : 0, element.BorderThickness)),
        CornerRadius = new CornerRadius(Mm(Math.Max(0, element.CornerRadiusMm)))
    };

    private FrameworkElement BuildLine(TemplateElement element, bool designMode)
    {
        var grid = new Grid
        {
            Width = Mm(element.WidthMm),
            Height = Math.Max(Mm(element.HeightMm), 6)
        };
        grid.Children.Add(new Line
        {
            X1 = 0,
            Y1 = grid.Height / 2,
            X2 = grid.Width,
            Y2 = grid.Height / 2,
            Stroke = designMode ? Brushes.SteelBlue : BrushFrom(element.BorderColor, Brushes.Black),
            StrokeThickness = Math.Max(0.5, element.LineThickness)
        });
        return grid;
    }

    private FrameworkElement BuildImage(TemplateElement element, bool designMode)
    {
        var border = new Border
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            Background = BrushFrom(element.BackgroundColor, Brushes.Transparent),
            BorderBrush = designMode ? Brushes.SteelBlue : BrushFrom(element.BorderColor, Brushes.Transparent),
            BorderThickness = designMode ? new Thickness(0.65) : new Thickness(Math.Max(0, element.BorderThickness)),
            CornerRadius = new CornerRadius(Mm(Math.Max(0, element.CornerRadiusMm))),
            ClipToBounds = true
        };

        var imagePath = ResolveImagePath(element.ImagePath);
        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
        {
            border.Child = new Image
            {
                Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                Stretch = Stretch.Uniform
            };
        }
        else if (designMode)
        {
            border.Child = new TextBlock
            {
                Text = "صورة / شعار",
                Foreground = Brushes.SlateGray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return border;
    }

    private FrameworkElement BuildTable(TemplateElement element, JsonElement data, bool designMode, TablePageSlice? tableSlice)
    {
        var grid = new Grid
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            ClipToBounds = true,
            FlowDirection = element.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Background = BrushFrom(element.BackgroundColor, Brushes.White)
        };

        var columns = element.Columns.Count > 0
            ? element.Columns
            : [new TemplateTableColumn { Header = "البيان", FieldPath = "Description", WidthMm = element.WidthMm }];

        foreach (var column in columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(Math.Max(1, column.WidthMm), GridUnitType.Star)
            });

        var items = JsonValueResolver.ResolveElement(data, element.FieldPath);
        var resolvedRows = items is { ValueKind: JsonValueKind.Array }
            ? items.Value.EnumerateArray().ToArray()
            : [];

        IEnumerable<JsonElement> rowsToRender = resolvedRows;
        var rowHeightMm = element.RowHeightMm;

        if (tableSlice is { } slice && slice.TableId == element.Id)
        {
            rowsToRender = resolvedRows.Skip(slice.Skip).Take(slice.Take);
        }
        else if (string.Equals(element.FieldPath, "CurrencySummary", StringComparison.OrdinalIgnoreCase) &&
                 resolvedRows.Length > Math.Max(1, element.MaxRows))
        {
            // Currency summaries are normally short. If they contain more currencies than the
            // designed row count, keep all currencies visible by fitting the rows into the
            // configured table height rather than truncating the data.
            var rowCount = resolvedRows.Length + (element.ShowHeader ? 1 : 0);
            rowHeightMm = rowCount > 0 ? element.HeightMm / rowCount : element.RowHeightMm;
        }
        else
        {
            rowsToRender = resolvedRows.Take(Math.Max(1, element.MaxRows));
        }

        var rowIndex = 0;
        if (element.ShowHeader)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Mm(rowHeightMm)) });
            for (var c = 0; c < columns.Count; c++)
                grid.Children.Add(MakeCell(element, columns[c].Header, rowIndex, c, true, columns[c].Alignment));
            rowIndex++;
        }

        if (resolvedRows.Length > 0)
        {
            var dataRow = 0;
            foreach (var item in rowsToRender)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Mm(rowHeightMm)) });
                for (var c = 0; c < columns.Count; c++)
                {
                    var col = columns[c];
                    var text = JsonValueResolver.ResolveText(item, col.FieldPath, col.Format);
                    grid.Children.Add(MakeCell(element, text, rowIndex, c, false, col.Alignment, dataRow));
                }
                rowIndex++;
                dataRow++;
            }
        }
        else if (designMode)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Mm(rowHeightMm)) });
            for (var c = 0; c < columns.Count; c++)
                grid.Children.Add(MakeCell(element, "{{" + columns[c].FieldPath + "}}", rowIndex, c, false, columns[c].Alignment, 0));
        }

        return new Border
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            BorderBrush = designMode && element.BorderThickness <= 0
                ? Brushes.SteelBlue
                : BrushFrom(element.BorderColor, Brushes.Black),
            BorderThickness = new Thickness(designMode && element.BorderThickness <= 0 ? 0.65 : Math.Max(0.35, element.BorderThickness)),
            CornerRadius = new CornerRadius(Mm(Math.Max(0, element.CornerRadiusMm))),
            ClipToBounds = true,
            Child = grid
        };
    }

    private static FrameworkElement MakeCell(
        TemplateElement element,
        string text,
        int row,
        int column,
        bool header,
        TemplateTextAlignment alignment,
        int dataRow = 0)
    {
        var background = header
            ? BrushFrom(element.HeaderBackgroundColor, Brushes.DarkSlateBlue)
            : dataRow % 2 == 1
                ? BrushFrom(element.AlternateRowBackgroundColor, Brushes.Transparent)
                : Brushes.White;

        var border = new Border
        {
            BorderBrush = BrushFrom(element.BorderColor, Brushes.LightGray),
            BorderThickness = new Thickness(0.35),
            Background = background,
            Padding = new Thickness(2.0, 1.0, 2.0, 1.0)
        };

        border.Child = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(string.IsNullOrWhiteSpace(element.FontFamily) ? "Segoe UI" : element.FontFamily),
            FontSize = header ? Math.Max(9.5, element.FontSize - 1) : Math.Max(9.0, element.FontSize - 2),
            FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = header
                ? BrushFrom(element.HeaderForegroundColor, Brushes.White)
                : BrushFrom(element.ForegroundColor, Brushes.Black),
            TextAlignment = ToTextAlignment(alignment),
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = element.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        return border;
    }

    private readonly record struct TablePageSlice(Guid TableId, int Skip, int Take);

    private static string? ResolveImagePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (System.IO.Path.IsPathRooted(value))
            return value;

        return System.IO.Path.Combine(AppContext.BaseDirectory, value);
    }
    private static Brush BrushFrom(string? value, Brush fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        try
        {
            var brush = new BrushConverter().ConvertFromString(value) as Brush;
            if (brush is not null)
            {
                if (brush.CanFreeze) brush.Freeze();
                return brush;
            }
        }
        catch
        {
            // Keep rendering even when a user typed an invalid color.
        }

        return fallback;
    }

    private static TextAlignment ToTextAlignment(TemplateTextAlignment alignment) => alignment switch
    {
        TemplateTextAlignment.Left => TextAlignment.Left,
        TemplateTextAlignment.Center => TextAlignment.Center,
        _ => TextAlignment.Right
    };

    public static double Mm(double value) => value * DipPerMm;
    public static double ToMm(double value) => value / DipPerMm;
}
