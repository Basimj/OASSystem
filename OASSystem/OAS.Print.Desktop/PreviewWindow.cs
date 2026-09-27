using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OAS.Print.Desktop;

public sealed class PreviewWindow : Window
{
    public PreviewWindow(UIElement content)
    {
        Title = "معاينة الطباعة";
        Width = 1000;
        Height = 850;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = new SolidColorBrush(Color.FromRgb(225, 229, 235));

        var host = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = content,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(20)
        };
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = host
        };
    }
}
