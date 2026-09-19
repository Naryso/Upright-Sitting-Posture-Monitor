using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace Upright.App;

public partial class FullScreenCalibrationWindow : Window
{
    private const uint SwpShowWindow = 0x0040;
    private static readonly nint HwndTopmost = new(-1);
    private readonly Forms.Screen _screen;
    private bool _closingSilently;
    private int _step;
    private int _previewPixelWidth;
    private int _previewPixelHeight;
    private double? _noseXFromLeft;
    private double? _noseYFromBottom;

    public FullScreenCalibrationWindow(Forms.Screen screen)
    {
        _screen = screen;
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public event EventHandler? CaptureRequested;

    public event EventHandler? CancelRequested;

    public string DeviceName => _screen.DeviceName;

    public void UpdateStep(int step, int total, string cornerName)
    {
        _step = step;
        StepText.Text = $"Calibration {step + 1}/{total}";
        CornerText.Text = $"Look at the {cornerName} of the screen";
        InstructionText.Text =
            "Keep a natural seated posture. Look at the ring, then press Space to capture. You can also click the ring.";
        UpdateTargetPosition();
    }

    public void ShowMessage(string message)
    {
        InstructionText.Text = message;
    }

    public void UpdatePreview(CameraFrame frame)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(() => UpdatePreview(frame));
            return;
        }

        int stride = checked(frame.Width * 4);
        int requiredLength = checked(stride * frame.Height);
        if (frame.Width <= 0 ||
            frame.Height <= 0 ||
            frame.BgraPixels.Length < requiredLength)
        {
            return;
        }

        BitmapSource preview = BitmapSource.Create(
            frame.Width,
            frame.Height,
            96,
            96,
            PixelFormats.Bgr32,
            null,
            frame.BgraPixels,
            stride);
        preview.Freeze();
        CameraPreviewImage.Source = preview;
        _previewPixelWidth = frame.Width;
        _previewPixelHeight = frame.Height;
        UpdateNoseMarkerPosition();
    }

    public void UpdateNoseMarker(
        double? noseXFromLeft,
        double? noseYFromBottom)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(() =>
                UpdateNoseMarker(noseXFromLeft, noseYFromBottom));
            return;
        }

        _noseXFromLeft = noseXFromLeft;
        _noseYFromBottom = noseYFromBottom;
        UpdateNoseMarkerPosition();
    }

    private void UpdateNoseMarkerPosition()
    {
        if (_noseXFromLeft is not double noseX ||
            _noseYFromBottom is not double noseY ||
            _previewPixelWidth <= 0 ||
            _previewPixelHeight <= 0 ||
            PreviewSurface.ActualWidth <= 0 ||
            PreviewSurface.ActualHeight <= 0)
        {
            NoseMarker.Visibility = Visibility.Collapsed;
            return;
        }

        double surfaceWidth = PreviewSurface.ActualWidth;
        double surfaceHeight = PreviewSurface.ActualHeight;
        double sourceAspect = (double)_previewPixelWidth / _previewPixelHeight;
        double surfaceAspect = surfaceWidth / surfaceHeight;
        double displayedWidth;
        double displayedHeight;
        double offsetX;
        double offsetY;

        if (surfaceAspect > sourceAspect)
        {
            displayedHeight = surfaceHeight;
            displayedWidth = displayedHeight * sourceAspect;
            offsetX = (surfaceWidth - displayedWidth) / 2;
            offsetY = 0;
        }
        else
        {
            displayedWidth = surfaceWidth;
            displayedHeight = displayedWidth / sourceAspect;
            offsetX = 0;
            offsetY = (surfaceHeight - displayedHeight) / 2;
        }

        double markerX = offsetX +
            Math.Clamp(noseX, 0, 1) * displayedWidth;
        double markerY = offsetY +
            (1 - Math.Clamp(noseY, 0, 1)) * displayedHeight;
        System.Windows.Controls.Canvas.SetLeft(
            NoseMarker,
            markerX - NoseMarker.Width / 2);
        System.Windows.Controls.Canvas.SetTop(
            NoseMarker,
            markerY - NoseMarker.Height / 2);
        NoseMarker.Visibility = Visibility.Visible;
    }

    public void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOnScreen();
        UpdateTargetPosition();
        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        PositionOnScreen();
    }

    private void PositionOnScreen()
    {
        nint handle = new WindowInteropHelper(this).Handle;
        if (handle == nint.Zero)
        {
            return;
        }

        System.Drawing.Rectangle bounds = _screen.Bounds;
        SetWindowPos(
            handle,
            HwndTopmost,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            SwpShowWindow);
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTargetPosition();
    }

    private void UpdateTargetPosition()
    {
        double width = TargetCanvas.ActualWidth;
        double height = TargetCanvas.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        CalibrationTargetPosition position =
            CalibrationTargetLayout.Calculate(width, height, _step);
        System.Windows.Controls.Canvas.SetLeft(CalibrationTarget, position.X);
        System.Windows.Controls.Canvas.SetTop(CalibrationTarget, position.Y);
    }

    private void OnPreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void OnCaptureClicked(object sender, RoutedEventArgs e)
    {
        CaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnTargetClicked(object sender, MouseButtonEventArgs e)
    {
        CaptureRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_closingSilently)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

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
}
