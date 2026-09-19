using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Upright.App;

public static class MainWindowSmokeRunner
{
    public static async Task<int> RunAsync(string outputPath)
    {
        var window = new MainWindow(isUiPreview: true);
        window.Show();
        await window.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.ApplicationIdle);
        await Task.Delay(250);

        FrameworkElement content = (FrameworkElement)window.Content;
        int width = Math.Max(1, (int)Math.Ceiling(content.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(content.ActualHeight));
        var bitmap = new RenderTargetBitmap(
            width,
            height,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(content);

        string fullPath = Path.GetFullPath(outputPath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using (FileStream stream = File.Create(fullPath))
        {
            encoder.Save(stream);
        }

        var calibrateButton =
            (System.Windows.Controls.Button)window.FindName("CalibrateButton");
        var startStopButton =
            (System.Windows.Controls.Button)window.FindName("StartStopButton");
        var workingIndicator =
            (System.Windows.Controls.Border)window.FindName("WorkingIndicator");
        var statusText =
            (System.Windows.Controls.TextBlock)window.FindName("StatusText");
        var statusDot =
            (System.Windows.Shapes.Ellipse)window.FindName("StatusDot");
        var brandWordmark =
            (System.Windows.Controls.Border)window.FindName("BrandWordmark");
        var brandSubtitle =
            (System.Windows.Controls.TextBlock)window.FindName("BrandSubtitle");
        var minimizeButton =
            (System.Windows.Controls.Button)window.FindName("MinimizeButton");
        var closeButton =
            (System.Windows.Controls.Button)window.FindName("CloseButton");
        var settingsButton =
            (System.Windows.Controls.Button)window.FindName("SettingsButton");
        System.Windows.Point calibrateBottom = calibrateButton.TranslatePoint(
            new System.Windows.Point(0, calibrateButton.ActualHeight),
            content);
        System.Windows.Point startStopBottom = startStopButton.TranslatePoint(
            new System.Windows.Point(0, startStopButton.ActualHeight),
            content);
        System.Windows.Point wordmarkLeft = brandWordmark.TranslatePoint(
            new System.Windows.Point(0, 0),
            content);
        System.Windows.Point subtitleLeft = brandSubtitle.TranslatePoint(
            new System.Windows.Point(0, 0),
            content);
        bool allStatusesPassed = Enum
            .GetValues<MonitorDisplayStatus>()
            .All(status =>
            {
                window.SetDisplayStatusForPreview(status);
                MonitorDisplayStatusPresentation expected =
                    MonitorDisplayStatusPresentation.From(status);
                return statusText.Text == expected.Text &&
                    statusDot.Fill is SolidColorBrush dotBrush &&
                    dotBrush.Color == expected.DotColor &&
                    statusText.Foreground is SolidColorBrush textBrush &&
                    textBrush.Color == expected.TextColor &&
                    workingIndicator.Visibility == Visibility.Visible;
            });
        window.SetDisplayStatusForPreview(
            MonitorDisplayStatus.MonitoringActive);
        bool passed =
            window.IsVisible &&
            window.WindowStyle == WindowStyle.None &&
            window.AllowsTransparency &&
            width >= 560 &&
            height >= 300 &&
            calibrateButton.ActualHeight >= 50 &&
            startStopButton.ActualHeight >= 50 &&
            calibrateBottom.Y <= content.ActualHeight &&
            startStopBottom.Y <= content.ActualHeight &&
            workingIndicator.Visibility == Visibility.Visible &&
            allStatusesPassed &&
            Math.Abs((wordmarkLeft.X + 2) - subtitleLeft.X) <= 0.5 &&
            minimizeButton.Content?.ToString() == "−" &&
            closeButton.Content?.ToString() == "×" &&
            settingsButton.Content?.ToString() == "Settings" &&
            settingsButton.ActualWidth >= 80 &&
            minimizeButton.ActualWidth >= 40 &&
            closeButton.ActualWidth >= 40 &&
            CountVisualChildren<System.Windows.Controls.Button>(content) == 5 &&
            CountVisualChildren<System.Windows.Controls.ComboBox>(content) == 1 &&
            CountVisualChildren<System.Windows.Controls.Image>(content) == 0 &&
            CountVisualChildren<System.Windows.Controls.Expander>(content) == 0 &&
            File.Exists(fullPath) &&
            new FileInfo(fullPath).Length > 0;
        window.Close();
        return passed ? 0 : 7;
    }

    private static int CountVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        int count = 0;
        for (int index = 0;
             index < VisualTreeHelper.GetChildrenCount(root);
             index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T)
            {
                count++;
            }

            count += CountVisualChildren<T>(child);
        }

        return count;
    }
}
