using System.Globalization;
using System.IO;
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

    public Canvas BuildCanvas(TemplateDefinition template, JsonElement data, bool designMode = false)
    {
        var canvas = new Canvas
        {
            Width = Mm(template.Landscape ? template.PaperHeightMm : template.PaperWidthMm),
            Height = Mm(template.Landscape ? template.PaperWidthMm : template.PaperHeightMm),
            Background = Brushes.White,
            FlowDirection = FlowDirection.LeftToRight
        };

        foreach (var element in template.Elements)
        {
            var visual = BuildElement(element, data, designMode);
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

        var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };
        var canvas = BuildCanvas(template, data, false);
        page.Children.Add(canvas);

        var content = new PageContent();
        ((IAddChild)content).AddChild(page);
        document.Pages.Add(content);
        return document;
    }

    private FrameworkElement BuildElement(TemplateElement element, JsonElement data, bool designMode)
    {
        return element.Type switch
        {
            TemplateElementType.Text => BuildText(element, element.Text, designMode),
            TemplateElementType.Field => BuildText(element,
                element.Prefix + JsonValueResolver.ResolveText(data, element.FieldPath, element.Format) + element.Suffix,
                designMode),
            TemplateElementType.Rectangle => BuildRectangle(element, designMode),
            TemplateElementType.Line => BuildLine(element, designMode),
            TemplateElementType.Image => BuildImage(element, designMode),
            TemplateElementType.Table => BuildTable(element, data, designMode),
            _ => BuildText(element, element.Text, designMode)
        };
    }

    private FrameworkElement BuildText(TemplateElement element, string text, bool designMode)
    {
        var block = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(element.FontFamily),
            FontSize = element.FontSize,
            FontWeight = element.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = element.Italic ? FontStyles.Italic : FontStyles.Normal,
            TextAlignment = ToTextAlignment(element.Alignment),
            FlowDirection = element.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(2)
        };
        var border = new Border
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            BorderBrush = designMode ? Brushes.SteelBlue : Brushes.Black,
            BorderThickness = designMode ? new Thickness(0.7) : new Thickness(element.BorderThickness),
            Child = block
        };
        return border;
    }

    private FrameworkElement BuildRectangle(TemplateElement element, bool designMode) => new Border
    {
        Width = Mm(element.WidthMm),
        Height = Mm(element.HeightMm),
        BorderBrush = designMode ? Brushes.SteelBlue : Brushes.Black,
        BorderThickness = new Thickness(Math.Max(designMode ? 0.7 : 0, element.BorderThickness > 0 ? element.BorderThickness : 1))
    };

    private FrameworkElement BuildLine(TemplateElement element, bool designMode)
    {
        var grid = new Grid { Width = Mm(element.WidthMm), Height = Math.Max(Mm(element.HeightMm), 6) };
        grid.Children.Add(new Line
        {
            X1 = 0,
            Y1 = grid.Height / 2,
            X2 = grid.Width,
            Y2 = grid.Height / 2,
            Stroke = designMode ? Brushes.SteelBlue : Brushes.Black,
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
            BorderBrush = designMode ? Brushes.SteelBlue : Brushes.Transparent,
            BorderThickness = designMode ? new Thickness(0.7) : new Thickness(0)
        };
        if (!string.IsNullOrWhiteSpace(element.ImagePath) && File.Exists(element.ImagePath))
        {
            border.Child = new Image
            {
                Source = new BitmapImage(new Uri(element.ImagePath, UriKind.Absolute)),
                Stretch = Stretch.Uniform
            };
        }
        else if (designMode)
        {
            border.Child = new TextBlock { Text = "صورة / شعار", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }
        return border;
    }

    private FrameworkElement BuildTable(TemplateElement element, JsonElement data, bool designMode)
    {
        var grid = new Grid
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            ClipToBounds = true,
            FlowDirection = element.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };

        var columns = element.Columns.Count > 0
            ? element.Columns
            : [new TemplateTableColumn { Header = "البيان", FieldPath = "Description", WidthMm = element.WidthMm }];

        foreach (var column in columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(1, column.WidthMm), GridUnitType.Star) });

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Mm(element.RowHeightMm)) });
        for (var c = 0; c < columns.Count; c++)
            grid.Children.Add(MakeCell(columns[c].Header, 0, c, true, columns[c].Alignment));

        var items = JsonValueResolver.ResolveElement(data, element.FieldPath);
        var row = 1;
        if (items is { ValueKind: JsonValueKind.Array })
        {
            foreach (var item in items.Value.EnumerateArray().Take(Math.Max(1, element.MaxRows)))
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Mm(element.RowHeightMm)) });
                for (var c = 0; c < columns.Count; c++)
                {
                    var col = columns[c];
                    var text = JsonValueResolver.ResolveText(item, col.FieldPath, col.Format);
                    grid.Children.Add(MakeCell(text, row, c, false, col.Alignment));
                }
                row++;
            }
        }
        else if (designMode)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Mm(element.RowHeightMm)) });
            for (var c = 0; c < columns.Count; c++)
                grid.Children.Add(MakeCell("{{" + columns[c].FieldPath + "}}", 1, c, false, columns[c].Alignment));
        }

        var border = new Border
        {
            Width = Mm(element.WidthMm),
            Height = Mm(element.HeightMm),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(designMode ? 0.8 : Math.Max(0.5, element.BorderThickness)),
            Child = grid
        };
        return border;
    }

    private static FrameworkElement MakeCell(string text, int row, int column, bool header, TemplateTextAlignment alignment)
    {
        var border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.35), Padding = new Thickness(2) };
        border.Child = new TextBlock
        {
            Text = text,
            FontSize = header ? 10 : 9,
            FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = ToTextAlignment(alignment),
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        return border;
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
