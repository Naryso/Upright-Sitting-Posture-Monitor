using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Forms = System.Windows.Forms;

namespace Upright.App;

public static class WarningOverlaySmokeRunner
{
    public static async Task<int> RunAsync(string outputPath)
    {
        using var manager = new WarningOverlayManager();
        manager.Update(0.65);
        await Task.Delay(350);

        IReadOnlyList<WarningOverlayDiagnostic> diagnostics =
            manager.GetDiagnostics();
        nint taskbar = FindWindow("Shell_TrayWnd", null);
        bool taskbarVisibleDuringWarning =
            taskbar != nint.Zero && IsWindowVisible(taskbar);
        var clearStopwatch = Stopwatch.StartNew();
        manager.Clear();
        clearStopwatch.Stop();
        await Task.Delay(950);
        IReadOnlyList<WarningOverlayDiagnostic> clearedDiagnostics =
            manager.GetDiagnostics();
        bool passed =
            diagnostics.Count == Forms.Screen.AllScreens.Length * 4 &&
            diagnostics.Count > 0 &&
            diagnostics.All(item =>
                item.IsVisible &&
                item.Opacity > 0 &&
                item.HasClickThroughStyle &&
                item.HasNoActivateStyle &&
                item.HasToolWindowStyle &&
                item.WindowBounds == item.ExpectedBounds &&
                item.WindowBounds != item.DisplayBounds) &&
            Forms.Screen.AllScreens.All(screen =>
            {
                WarningOverlayDiagnostic[] edges = diagnostics
                    .Where(item => item.DeviceName == screen.DeviceName)
                    .ToArray();
                return edges.Length == 4 &&
                    edges.Select(item => item.Edge).Distinct().Count() == 4 &&
                    edges.Single(item => item.Edge == WarningEdge.Top)
                        .WindowBounds.Top == screen.Bounds.Top &&
                    edges.Single(item => item.Edge == WarningEdge.Bottom)
                        .WindowBounds.Bottom == screen.Bounds.Bottom &&
                    edges.Single(item => item.Edge == WarningEdge.Left)
                        .WindowBounds.Left == screen.Bounds.Left &&
                    edges.Single(item => item.Edge == WarningEdge.Right)
                        .WindowBounds.Right == screen.Bounds.Right;
            }) &&
            taskbarVisibleDuringWarning &&
            clearedDiagnostics.All(item => !item.IsVisible) &&
            clearStopwatch.Elapsed < TimeSpan.FromSeconds(1);

        string fullPath = Path.GetFullPath(outputPath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(
                new
                {
                    Status = passed ? "passed" : "failed",
                    ActiveScreenCount = Forms.Screen.AllScreens.Length,
                    OverlayCount = diagnostics.Count,
                    ClearMilliseconds = clearStopwatch.Elapsed.TotalMilliseconds,
                    ClearedWithinOneSecond =
                        clearedDiagnostics.All(item => !item.IsVisible),
                    TaskbarVisibleDuringWarning =
                        taskbarVisibleDuringWarning,
                    Windows = diagnostics,
                },
                new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 5;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(
        string? className,
        string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);
}
