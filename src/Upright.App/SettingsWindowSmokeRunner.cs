using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Upright.App;

public static class SettingsWindowSmokeRunner
{
    public static async Task<int> RunAsync(string outputPath)
    {
        var window = new SettingsWindow(
            MonitorSettingsInput.DefaultDeadZone,
            MonitorSettingsInput.DefaultWarningDelaySeconds);
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

        var shell =
            (System.Windows.Controls.Border)window.FindName("SettingsShell");
        var title =
            (System.Windows.Controls.TextBlock)window.FindName("SettingsTitleText");
        var deadZone =
            (System.Windows.Controls.TextBox)window.FindName("DeadZoneTextBox");
        var warningDelay =
            (System.Windows.Controls.TextBox)window.FindName("WarningDelayTextBox");
        var deadZoneRange =
            (System.Windows.Controls.TextBlock)window.FindName("DeadZoneRangeText");
        var warningDelayRange =
            (System.Windows.Controls.TextBlock)window.FindName("WarningDelayRangeText");
        var restartNotice =
            (System.Windows.Controls.TextBlock)window.FindName("RestartNoticeText");
        var cancel =
            (System.Windows.Controls.Button)window.FindName("CancelButton");
        var save =
            (System.Windows.Controls.Button)window.FindName("SaveButton");
        System.Windows.Point saveBottom = save.TranslatePoint(
            new System.Windows.Point(0, save.ActualHeight),
            content);

        bool passed =
            window.IsVisible &&
            window.WindowStyle == WindowStyle.None &&
            window.AllowsTransparency &&
            !window.ShowInTaskbar &&
            shell.CornerRadius.TopLeft >= 16 &&
            title.Text == "Settings" &&
            deadZone.Text == "0.03" &&
            warningDelay.Text == "0" &&
            deadZoneRange.Text.Contains(
                "Default: 0.03",
                StringComparison.Ordinal) &&
            deadZoneRange.Text.Contains(
                "Range: 0.00–0.20",
                StringComparison.Ordinal) &&
            warningDelayRange.Text.Contains(
                "Default: 0 seconds",
                StringComparison.Ordinal) &&
            warningDelayRange.Text.Contains(
                "Range: 0–30 seconds",
                StringComparison.Ordinal) &&
            restartNotice.Text.Contains(
                "next time Upright starts",
                StringComparison.Ordinal) &&
            cancel.ActualHeight >= 44 &&
            save.ActualHeight >= 44 &&
            saveBottom.Y <= content.ActualHeight &&
            File.Exists(fullPath) &&
            new FileInfo(fullPath).Length > 0;

        window.Close();
        return passed ? 0 : 10;
    }
}
