using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using OpenMoney.App.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenMoney.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = App.Services.GetRequiredService<MainViewModel>();
        DataContext = vm;
        SetCashIcon();
    }

    private void SetCashIcon()
    {
        const int size = 32;
        const double iconViewBox = 24.0;

        var pathData = new PackIcon { Kind = PackIconKind.Cash }.Data;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x00, 0x96, 0x88)), null, new Rect(0, 0, size, size));
            if (!string.IsNullOrEmpty(pathData))
            {
                var scale = size / iconViewBox;
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.DrawGeometry(Brushes.White, null, Geometry.Parse(pathData));
                dc.Pop();
            }
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        Icon = bitmap;
    }
}
