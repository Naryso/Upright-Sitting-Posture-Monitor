using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Upright.App;

public static class AboutWindowSmokeRunner
{
    public static async Task<int> RunAsync(string outputPath)
    {
        var window = new AboutWindow();
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

        var shell = (Border)window.FindName("AboutShell");
        var aboutTitle = (TextBlock)window.FindName("AboutTitleText");
        var purpose = (TextBlock)window.FindName("PurposeText");
        var privacy = (TextBlock)window.FindName("PrivacyNoticeText");
        var productVersion = (TextBlock)window.FindName("ProductVersionText");
        var copyright = (TextBlock)window.FindName("CopyrightNoticeText");
        var license = (TextBlock)window.FindName("LicenseNoticeText");
        var thirdParty = (TextBlock)window.FindName("ThirdPartyNoticeText");
        var closeHeader =
            (System.Windows.Controls.Button)window.FindName("CloseHeaderButton");
        var closeFooter =
            (System.Windows.Controls.Button)window.FindName("CloseFooterButton");
        System.Windows.Point closeFooterBottom = closeFooter.TranslatePoint(
            new System.Windows.Point(0, closeFooter.ActualHeight),
            content);

        bool passed =
            window.IsVisible &&
            window.WindowStyle == WindowStyle.None &&
            window.AllowsTransparency &&
            !window.ShowInTaskbar &&
            shell.CornerRadius.TopLeft >= 16 &&
            aboutTitle.Text == "About Upright" &&
            purpose.Text == AboutDisclosure.Purpose &&
            privacy.Text == AboutDisclosure.PrivacyNotice &&
            productVersion.Text == AboutDisclosure.ProductVersion &&
            copyright.Text == "Copyright © 2026 Naryso" &&
            license.Text == "This software is open source under the MIT License." &&
            thirdParty.Text.Contains(
                "Open-Source Licenses and Third-Party Notices",
                StringComparison.Ordinal) &&
            closeHeader.ActualWidth >= 36 &&
            closeFooter.ActualHeight >= 44 &&
            closeFooterBottom.Y <= content.ActualHeight &&
            File.Exists(fullPath) &&
            new FileInfo(fullPath).Length > 0;

        window.Close();
        return passed ? 0 : 9;
    }
}
