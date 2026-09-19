using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace Upright.App;

public static class CalibrationWindowSmokeRunner
{
    public static async Task<int> RunAsync(string outputPath)
    {
        Forms.Screen screen = Forms.Screen.PrimaryScreen
            ?? throw new InvalidOperationException("No primary display is available.");
        var window = new FullScreenCalibrationWindow(screen);
        window.UpdateStep(0, 4, "top-left corner");
        window.Show();
        await Task.Delay(700);

        nint handle = new WindowInteropHelper(window).Handle;
        bool hasBounds = GetWindowRect(handle, out NativeRect rect);
        System.Drawing.Rectangle expected = screen.Bounds;
        bool passed =
            window.IsVisible &&
            window.Topmost &&
            window.AllowsTransparency &&
            window.Background is SolidColorBrush backgroundBrush &&
            backgroundBrush.Color.A == 0 &&
            hasBounds &&
            rect.Left == expected.Left &&
            rect.Top == expected.Top &&
            rect.Right - rect.Left == expected.Width &&
            rect.Bottom - rect.Top == expected.Height;
        var cornerText =
            (System.Windows.Controls.TextBlock)window.FindName("CornerText");
        passed =
            passed &&
            cornerText.TextWrapping == TextWrapping.Wrap &&
            cornerText.ActualWidth <= 640.5 &&
            cornerText.ActualHeight > 0 &&
            !ContainsText(
                window,
                "Targets are based on the corners of this display.");

        CalibrationTargetPosition[] targets = Enumerable.Range(0, 4)
            .Select(step => CalibrationTargetLayout.Calculate(
                window.ActualWidth,
                window.ActualHeight,
                step))
            .ToArray();

        string fullPath = Path.GetFullPath(outputPath);
        string screenshotPath = Path.ChangeExtension(fullPath, ".png");
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        FrameworkElement content = (FrameworkElement)window.Content;
        int renderWidth = Math.Max(
            1,
            (int)Math.Ceiling(content.ActualWidth));
        int renderHeight = Math.Max(
            1,
            (int)Math.Ceiling(content.ActualHeight));
        var bitmap = new RenderTargetBitmap(
            renderWidth,
            renderHeight,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using (FileStream stream = File.Create(screenshotPath))
        {
            encoder.Save(stream);
        }

        passed =
            passed &&
            File.Exists(screenshotPath) &&
            new FileInfo(screenshotPath).Length > 0;
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(
                new
                {
                    Status = passed ? "passed" : "failed",
                    screen.DeviceName,
                    ExpectedBounds = expected,
                    NativeBounds = hasBounds
                        ? new
                        {
                            rect.Left,
                            rect.Top,
                            Width = rect.Right - rect.Left,
                            Height = rect.Bottom - rect.Top,
                        }
                        : null,
                    WpfSize = new
                    {
                        window.ActualWidth,
                        window.ActualHeight,
                    },
                    Targets = targets,
                    Screenshot = screenshotPath,
                },
                new JsonSerializerOptions { WriteIndented = true }));

        window.CloseSilently();
        return passed ? 0 : 6;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);

    private static bool ContainsText(
        DependencyObject root,
        string text)
    {
        if (root is System.Windows.Controls.TextBlock textBlock &&
            textBlock.Text.Contains(text, StringComparison.Ordinal))
        {
            return true;
        }

        for (int index = 0;
             index < VisualTreeHelper.GetChildrenCount(root);
             index++)
        {
            if (ContainsText(
                    VisualTreeHelper.GetChild(root, index),
                    text))
            {
                return true;
            }
        }

        return false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
