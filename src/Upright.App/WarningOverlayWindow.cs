using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace Upright.App;

public sealed class WarningOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const double MaximumGradientDepth = 72;
    private static readonly nint HwndTopmost = new(-1);

    private readonly Forms.Screen _screen;
    private readonly WarningEdge _edge;
    private readonly EdgeWarningFrame _warningFrame;
    private System.Drawing.Rectangle _expectedBounds;

    public WarningOverlayWindow(Forms.Screen screen, WarningEdge edge)
    {
        _screen = screen;
        _edge = edge;
        Title = $"Upright warning - {screen.DeviceName} - {edge}";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        IsHitTestVisible = false;

        _warningFrame = new EdgeWarningFrame
        {
            SnapsToDevicePixels = true,
            IsHitTestVisible = false,
        };
        Content = _warningFrame;
        SourceInitialized += OnSourceInitialized;
    }

    public string DeviceName => _screen.DeviceName;

    public WarningEdge Edge => _edge;

    public long ExtendedWindowStyle
    {
        get
        {
            nint handle = new WindowInteropHelper(this).Handle;
            return handle == nint.Zero
                ? 0
                : GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        }
    }

    public bool HasClickThroughStyle =>
        (ExtendedWindowStyle & WsExTransparent) != 0;

    public bool HasNoActivateStyle =>
        (ExtendedWindowStyle & WsExNoActivate) != 0;

    public bool HasToolWindowStyle =>
        (ExtendedWindowStyle & WsExToolWindow) != 0;

    public System.Drawing.Rectangle DisplayBounds => _screen.Bounds;

    public System.Drawing.Rectangle WorkingArea => _screen.WorkingArea;

    public System.Drawing.Rectangle ExpectedBounds => _expectedBounds;

    public System.Drawing.Rectangle WindowBounds
    {
        get
        {
            nint handle = new WindowInteropHelper(this).Handle;
            return handle != nint.Zero &&
                GetWindowRect(handle, out NativeRect rect)
                    ? System.Drawing.Rectangle.FromLTRB(
                        rect.Left,
                        rect.Top,
                        rect.Right,
                        rect.Bottom)
                    : System.Drawing.Rectangle.Empty;
        }
    }

    public void SetSeverity(double severity)
    {
        double sanitized = double.IsFinite(severity)
            ? Math.Clamp(severity, 0, 1)
            : 0;
        if (sanitized > 0)
        {
            if (!IsVisible)
            {
                Show();
            }
        }
        else if (IsVisible)
        {
            Hide();
        }

        WarningVisualState visual = WarningVisualState.FromSeverity(sanitized);
        _warningFrame.GradientDepth = visual.BorderThickness;
        Opacity = visual.WindowOpacity;
        PositionOnScreen();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        nint handle = new WindowInteropHelper(this).Handle;
        nint style = GetWindowLongPtr(handle, GwlExStyle);
        nint updatedStyle = new(
            style.ToInt64() |
            WsExTransparent |
            WsExToolWindow |
            WsExNoActivate);
        SetWindowLongPtr(handle, GwlExStyle, updatedStyle);
        PositionOnScreen();
    }

    private void PositionOnScreen()
    {
        nint handle = new WindowInteropHelper(this).Handle;
        if (handle == nint.Zero)
        {
            return;
        }

        uint dpi = GetDpiForWindow(handle);
        double scale = dpi > 0 ? dpi / 96.0 : 1;
        int maximumPixelDepth = Math.Max(
            1,
            (int)Math.Ceiling(MaximumGradientDepth * scale));
        System.Drawing.Rectangle display = _screen.Bounds;

        _expectedBounds = _edge switch
        {
            WarningEdge.Top => new System.Drawing.Rectangle(
                display.Left,
                display.Top,
                display.Width,
                maximumPixelDepth),
            WarningEdge.Bottom => new System.Drawing.Rectangle(
                display.Left,
                display.Bottom - maximumPixelDepth,
                display.Width,
                maximumPixelDepth),
            WarningEdge.Left => new System.Drawing.Rectangle(
                display.Left,
                display.Top + maximumPixelDepth,
                maximumPixelDepth,
                Math.Max(1, display.Height - (2 * maximumPixelDepth))),
            WarningEdge.Right => new System.Drawing.Rectangle(
                display.Right - maximumPixelDepth,
                display.Top + maximumPixelDepth,
                maximumPixelDepth,
                Math.Max(1, display.Height - (2 * maximumPixelDepth))),
            _ => display,
        };

        _warningFrame.ConfigureViewport(
            display.Width / scale,
            display.Height / scale,
            (_expectedBounds.Left - display.Left) / scale,
            (_expectedBounds.Top - display.Top) / scale);

        SetWindowPos(
            handle,
            HwndTopmost,
            _expectedBounds.Left,
            _expectedBounds.Top,
            _expectedBounds.Width,
            _expectedBounds.Height,
            SwpNoActivate | SwpShowWindow);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(
        nint window,
        out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(
        nint window,
        int index,
        nint newValue);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
