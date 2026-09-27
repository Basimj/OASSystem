using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OAS.Print.Desktop;

public sealed class PreviewWindow : Window
{
    public PreviewWindow(UIElement content)
    {
        Title = "معاينة الطباعة — OAS Print";
        Width = 1120;
        Height = 820;
        MinWidth = 760;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        FlowDirection = FlowDirection.RightToLeft;
        Background = Brush("#FBFBFD");
        FontFamily = new FontFamily("Segoe UI");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush("#E7E5EF"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 12, 18, 12)
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titlePanel = new StackPanel();
        titlePanel.Children.Add(new TextBlock
        {
            Text = "معاينة المستند",
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#24213B")
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "المعاينة تحافظ على مقاس الورق الحقيقي؛ استخدم أشرطة التمرير لرؤية كامل الصفحة.",
            FontSize = 11,
            Foreground = Brush("#77758C"),
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(titlePanel, 0);
        headerGrid.Children.Add(titlePanel);

        var brand = new Border
        {
            Background = Brush("#F4F0FF"),
            BorderBrush = Brush("#D9CCFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 7, 12, 7),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "OAS PRINT",
                FlowDirection = FlowDirection.LeftToRight,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brush("#5A2DD6")
            }
        };
        Grid.SetColumn(brand, 1);
        headerGrid.Children.Add(brand);
        header.Child = headerGrid;
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var pageHost = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush("#D9CCFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = content,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(24)
        };

        var scroll = new ScrollViewer
        {
            Background = Brush("#EEF0F5"),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Top,
            Content = pageHost
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        Content = root;
    }

    private static Brush Brush(string value) =>
        (Brush)new BrushConverter().ConvertFromString(value)!;
}
