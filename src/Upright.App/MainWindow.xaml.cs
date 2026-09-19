using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Upright.Core;
using Upright.Vision;
using Forms = System.Windows.Forms;

namespace Upright.App;

public partial class MainWindow : Window
{
    private readonly CameraCaptureService _camera = new();
    private readonly SemaphoreSlim _inferenceGate = new(1, 1);
    private readonly Stopwatch _processingClock = Stopwatch.StartNew();
    private readonly AppSettingsStore _settingsStore = new();
    private readonly WarningOverlayManager _overlayManager = new();
    private readonly List<CameraCalibrationSample> _calibrationSamples = [];
    private readonly string[] _calibrationCornerNames =
        ["top-left corner", "top-right corner", "bottom-right corner", "bottom-left corner"];
    private TwoStagePoseDetector? _poseDetector;
    private WindowsFaceDetector? _faceDetector;
    private PostureMonitoringSession? _monitoringSession;
    private UprightAppSettings _settings = new();
    private Forms.NotifyIcon? _trayIcon;
    private FullScreenCalibrationWindow? _calibrationWindow;
    private AboutWindow? _aboutWindow;
    private bool _isRunning;
    private bool _monitoringPaused;
    private bool _allowClose;
    private bool _isInitializing = true;
    private readonly bool _isUiPreview;
    private double _runtimeDeadZone = MonitorSettingsInput.DefaultDeadZone;
    private double _runtimeWarningDelaySeconds =
        MonitorSettingsInput.DefaultWarningDelaySeconds;
    private TimeSpan? _lastProcessedAt;
    private VisionDecision? _latestDecision;
    private int _calibrationStep = -1;

    public MainWindow()
        : this(isUiPreview: false)
    {
    }

    internal MainWindow(bool isUiPreview)
    {
        _isUiPreview = isUiPreview;
        InitializeComponent();
        if (isUiPreview)
        {
            _allowClose = true;
            CameraComboBox.ItemsSource =
                new[] { new CameraDevice("preview", "Built-in Camera") };
            CameraComboBox.SelectedIndex = 0;
            CalibrateButton.IsEnabled = true;
            StartStopButton.IsEnabled = true;
            StartStopButton.Content = "Start";
            SettingsButton.IsEnabled = true;
            _isRunning = false;
            SetDisplayStatus(MonitorDisplayStatus.ToolInactive);
            return;
        }

        _camera.FrameArrived += OnCameraFrameArrived;
        _camera.CaptureFailed += OnCameraCaptureFailed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isUiPreview)
        {
            _isInitializing = false;
            CalibrateButton.IsEnabled = true;
            StartStopButton.IsEnabled = true;
            return;
        }

        try
        {
            _settings = await _settingsStore.LoadAsync();
            _runtimeDeadZone = _settings.DeadZone;
            _runtimeWarningDelaySeconds = _settings.WarningDelaySeconds;
            _monitoringPaused = false;
            if (_settings.MonitoringPaused)
            {
                _settings = _settings with { MonitoringPaused = false };
                await _settingsStore.SaveAsync(_settings);
            }

            await RefreshCamerasAsync();
            LoadModels();
            _faceDetector = await WindowsFaceDetector.CreateAsync();
            RestoreMonitoringSession();
        }
        catch (Exception exception)
        {
            ShowError("Initialization failed", exception);
        }
        finally
        {
            _isInitializing = false;
            SettingsButton.IsEnabled = true;
            TryCreateTrayIcon();
        }
    }

    private async void OnStartStopClicked(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            await StopCameraAsync();
            return;
        }

        await StartCameraAsync();
    }

    private async Task<bool> StartCameraAsync()
    {
        if (CameraComboBox.SelectedItem is not CameraDevice selected)
        {
            System.Windows.MessageBox.Show(
                this,
                "No camera is available.",
                "Upright",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        try
        {
            StartStopButton.IsEnabled = false;
            CalibrateButton.IsEnabled = false;
            await _camera.StartAsync(selected.Id);

            _isRunning = true;
            StartStopButton.Content = "Stop";
            SetDisplayStatus(MonitorDisplayStatus.MonitoringActive);
            CalibrateButton.IsEnabled = true;
            RestoreMonitoringSession();
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            const string permissionMessage =
                "Camera access was denied. Allow desktop apps to access the camera in Windows privacy settings.";
            MessageBoxResult result = System.Windows.MessageBox.Show(
                this,
                permissionMessage + "\n\nOpen camera privacy settings now?",
                "Upright",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(
                    new ProcessStartInfo("ms-settings:privacy-webcam")
                    {
                        UseShellExecute = true,
                    });
            }

            Debug.WriteLine(exception);
            return false;
        }
        catch (Exception exception)
        {
            ShowError("Camera startup failed", exception);
            return false;
        }
        finally
        {
            StartStopButton.IsEnabled = true;
            CalibrateButton.IsEnabled =
                CameraComboBox.SelectedItem is CameraDevice;
            if (!_isRunning)
            {
                SetDisplayStatus(MonitorDisplayStatus.ToolInactive);
            }
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        _trayIcon?.ShowBalloonTip(
            1500,
            "Upright is still monitoring",
            "Double-click the notification-area icon to reopen the window.",
            Forms.ToolTipIcon.Info);
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        CloseCalibrationWindow();
        _camera.FrameArrived -= OnCameraFrameArrived;
        _camera.CaptureFailed -= OnCameraCaptureFailed;
        await _camera.DisposeAsync();
        _poseDetector?.Dispose();
        _overlayManager.Dispose();
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _inferenceGate.Dispose();
    }

    private async Task RefreshCamerasAsync()
    {
        CameraDevice? previous = CameraComboBox.SelectedItem as CameraDevice;
        IReadOnlyList<CameraDevice> cameras =
            await CameraCaptureService.GetCamerasAsync();
        CameraComboBox.ItemsSource = cameras;
        CameraComboBox.SelectedItem =
            cameras.FirstOrDefault(camera => camera.Id == previous?.Id) ??
            cameras.FirstOrDefault(camera => camera.Id == _settings.CameraId) ??
            cameras.FirstOrDefault();
        StartStopButton.IsEnabled = cameras.Count > 0;
        CalibrateButton.IsEnabled = cameras.Count > 0;
    }

    private async void OnCameraDropDownOpened(object sender, EventArgs e)
    {
        if (_isInitializing || _isRunning)
        {
            return;
        }

        await RefreshCamerasAsync();
    }

    private void LoadModels()
    {
        (string detectorPath, string posePath) = ModelAssets.Resolve();

        _poseDetector = TwoStagePoseDetector.Create(
            detectorPath,
            posePath,
            preferDirectMl: true);
    }

    private void OnCameraFrameArrived(object? sender, CameraFrame frame)
    {
        _calibrationWindow?.UpdatePreview(frame);

        TimeSpan now = _processingClock.Elapsed;
        TimeSpan processingInterval =
            _monitoringSession?.ProcessingInterval ??
            PostureEngine.BaseFrameInterval;
        if (_lastProcessedAt is TimeSpan lastProcessedAt &&
            now - lastProcessedAt < processingInterval)
        {
            return;
        }

        _lastProcessedAt = now;
        _ = ProcessFrameAsync(frame);
    }

    private void OnCameraCaptureFailed(object? sender, Exception exception)
    {
        Debug.WriteLine($"Camera frame error: {exception}");
    }

    private async Task ProcessFrameAsync(CameraFrame frame)
    {
        if (!await _inferenceGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            TwoStagePoseDetector? poseDetector = _poseDetector;
            WindowsFaceDetector? faceDetector = _faceDetector;
            if (poseDetector is null || faceDetector is null)
            {
                return;
            }

            var visionFrame = new VisionFrame(
                frame.BgraPixels,
                frame.Width,
                frame.Height);
            PoseObservation pose = await Task.Run(() => poseDetector.Detect(visionFrame));
            _calibrationWindow?.UpdateNoseMarker(
                pose.HasUsableNose ? pose.NoseX : null,
                pose.HasUsableNose ? pose.NoseYFromBottom : null);
            FaceObservation? face = null;

            // Exact parity order: face fallback only when no body observation exists.
            if (!pose.HasBodyObservation)
            {
                face = await faceDetector.DetectFirstAsync(visionFrame);
            }

            VisionDecision decision = VisionDecision.From(pose, face);
            if (decision.Kind == VisionDecisionKind.AcceptedObservation)
            {
                _latestDecision = decision;
            }

            MonitoringUpdate? monitoringUpdate = null;
            if (_monitoringSession is not null && !_monitoringPaused)
            {
                monitoringUpdate = _monitoringSession.Process(
                    decision,
                    DateTimeOffset.UtcNow);
            }

            if (monitoringUpdate is not null)
            {
                await Dispatcher.InvokeAsync(() =>
                    UpdateMonitoringPresentation(monitoringUpdate));
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Detection error: {exception}");
        }
        finally
        {
            _inferenceGate.Release();
        }
    }

    private void UpdateMonitoringPresentation(MonitoringUpdate update)
    {
        bool showWarning =
            update.State.IsCurrentlySlouching &&
            !update.State.IsCurrentlyAway;
        if (showWarning)
        {
            _overlayManager.Update(update.State.PostureWarningIntensity);
            SetDisplayStatus(MonitorDisplayStatus.PoorPostureDetected);
        }
        else
        {
            _overlayManager.Clear();
            SetDisplayStatus(MonitorDisplayStatus.MonitoringActive);
        }
    }

    private async Task StopCameraAsync()
    {
        StartStopButton.IsEnabled = false;
        try
        {
            await _camera.StopAsync();
            _isRunning = false;
            StartStopButton.Content = "Start";
            SetDisplayStatus(MonitorDisplayStatus.ToolInactive);
            CalibrateButton.IsEnabled =
                CameraComboBox.SelectedItem is CameraDevice;
            _overlayManager.Clear();
        }
        finally
        {
            StartStopButton.IsEnabled = true;
        }
    }

    private async void OnCalibrateClicked(object sender, RoutedEventArgs e)
    {
        if (!_isRunning && !await StartCameraAsync())
        {
            return;
        }

        _calibrationSamples.Clear();
        _calibrationStep = 0;
        _latestDecision = null;
        _monitoringPaused = true;
        SetDisplayStatus(MonitorDisplayStatus.Calibrating);
        CalibrateButton.IsEnabled = false;
        StartStopButton.IsEnabled = false;
        OpenCalibrationWindow();
    }

    private void CaptureCalibrationSample()
    {
        if (_calibrationStep < 0)
        {
            return;
        }

        if (_latestDecision is not VisionDecision decision ||
            decision.Kind != VisionDecisionKind.AcceptedObservation)
        {
            _calibrationWindow?.ShowMessage(
                "No reliable body-pose nose or face is currently visible. Adjust your position and try again.");
            return;
        }

        _calibrationSamples.Add(
            new CameraCalibrationSample(
                decision.NoseYFromBottom,
                decision.FaceWidth));
        _latestDecision = null;
        _calibrationStep++;

        if (_calibrationStep < _calibrationCornerNames.Length)
        {
            UpdateCalibrationWindow();
            return;
        }

        CompleteCalibration();
    }

    private void CompleteCalibration()
    {
        string cameraId =
            (CameraComboBox.SelectedItem as CameraDevice)?.Id ?? string.Empty;
        CameraCalibrationData? calibration =
            CameraCalibration.Create(_calibrationSamples, cameraId);
        if (calibration is null || !calibration.IsValid)
        {
            _calibrationSamples.Clear();
            _calibrationStep = 0;
            _calibrationWindow?.ShowMessage(
                "The captured range is too small. Look clearly toward all four corners and repeat the four captures.");
            UpdateCalibrationWindow(preserveMessage: true);
            return;
        }

        _monitoringSession = new PostureMonitoringSession(
            calibration,
            _runtimeDeadZone,
            new PostureConfig(
                FrameThreshold: PostureEngine.DefaultBadFrameThreshold,
                GoodFrameThreshold: PostureEngine.DefaultGoodFrameThreshold,
                WarningOnsetDelay: TimeSpan.FromSeconds(
                    _runtimeWarningDelaySeconds),
                Intensity: _settings.WarningIntensity),
            awayEnabled: _settings.AwayDetectionEnabled);
        _settings = _settings with
        {
            CameraId = cameraId,
            CameraCalibration = calibration,
            MonitoringPaused = false,
        };
        _ = _settingsStore.SaveAsync(_settings);
        _calibrationStep = -1;
        CloseCalibrationWindow();
        _monitoringPaused = false;
        SetDisplayStatus(MonitorDisplayStatus.MonitoringActive);
        CalibrateButton.IsEnabled = true;
        StartStopButton.IsEnabled = true;
    }

    private void CancelCalibration()
    {
        _calibrationStep = -1;
        _calibrationSamples.Clear();
        CloseCalibrationWindow();
        _monitoringPaused = _monitoringSession is null;
        SetDisplayStatus(
            _isRunning
                ? MonitorDisplayStatus.MonitoringActive
                : MonitorDisplayStatus.ToolInactive);
        CalibrateButton.IsEnabled = _isRunning;
        StartStopButton.IsEnabled = true;
    }

    private void OpenCalibrationWindow()
    {
        nint handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        Forms.Screen screen = handle == nint.Zero
            ? Forms.Screen.PrimaryScreen!
            : Forms.Screen.FromHandle(handle);
        _calibrationWindow = new FullScreenCalibrationWindow(screen);
        _calibrationWindow.CaptureRequested += OnCalibrationCaptureRequested;
        _calibrationWindow.CancelRequested += OnCalibrationCancelRequested;
        _calibrationWindow.Show();
        UpdateCalibrationWindow();
    }

    private void UpdateCalibrationWindow(bool preserveMessage = false)
    {
        if (_calibrationStep < 0 ||
            _calibrationStep >= _calibrationCornerNames.Length ||
            _calibrationWindow is null)
        {
            return;
        }

        string? message = preserveMessage
            ? "The captured range is too small. Look clearly toward all four corners and repeat the four captures."
            : null;
        _calibrationWindow.UpdateStep(
            _calibrationStep,
            _calibrationCornerNames.Length,
            _calibrationCornerNames[_calibrationStep]);
        if (message is not null)
        {
            _calibrationWindow.ShowMessage(message);
        }
    }

    private void OnCalibrationCaptureRequested(object? sender, EventArgs e)
    {
        CaptureCalibrationSample();
    }

    private void OnCalibrationCancelRequested(object? sender, EventArgs e)
    {
        CancelCalibration();
    }

    private void CloseCalibrationWindow()
    {
        FullScreenCalibrationWindow? window = _calibrationWindow;
        if (window is null)
        {
            return;
        }

        _calibrationWindow = null;
        window.CaptureRequested -= OnCalibrationCaptureRequested;
        window.CancelRequested -= OnCalibrationCancelRequested;
        window.CloseSilently();
    }

    private async void OnCameraSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing ||
            CameraComboBox.SelectedItem is not CameraDevice selected ||
            selected.Id == _settings.CameraId)
        {
            return;
        }

        bool wasRunning = _isRunning;
        if (wasRunning)
        {
            await _camera.StopAsync();
            _isRunning = false;
        }

        CameraCalibrationData? calibration =
            _settings.CameraCalibration?.CameraId == selected.Id
                ? _settings.CameraCalibration
                : null;
        _settings = _settings with
        {
            CameraId = selected.Id,
            CameraCalibration = calibration,
        };
        await _settingsStore.SaveAsync(_settings);
        _monitoringSession = null;
        RestoreMonitoringSession();

        if (wasRunning)
        {
            await _camera.StartAsync(selected.Id);
            _isRunning = true;
            SetDisplayStatus(MonitorDisplayStatus.MonitoringActive);
        }
        else
        {
            SetDisplayStatus(MonitorDisplayStatus.ToolInactive);
        }

        CalibrateButton.IsEnabled = true;
    }

    private void RestoreMonitoringSession()
    {
        CameraCalibrationData? calibration = _settings.CameraCalibration;
        string? selectedCameraId =
            (CameraComboBox.SelectedItem as CameraDevice)?.Id;
        if (calibration?.IsValid != true ||
            calibration.CameraId != selectedCameraId)
        {
            _monitoringSession = null;
            return;
        }

        _monitoringSession = new PostureMonitoringSession(
            calibration,
            _runtimeDeadZone,
            new PostureConfig(
                FrameThreshold: PostureEngine.DefaultBadFrameThreshold,
                GoodFrameThreshold: PostureEngine.DefaultGoodFrameThreshold,
                WarningOnsetDelay: TimeSpan.FromSeconds(
                    _runtimeWarningDelaySeconds),
                Intensity: _settings.WarningIntensity),
            _settings.AwayDetectionEnabled);
        _monitoringPaused = false;
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Upright", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add("About Upright", null, (_, _) => Dispatcher.Invoke(ShowAbout));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(QuitApplication));

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Upright! - Sitting Posture Monitor",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                ?? System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
    }

    private void TryCreateTrayIcon()
    {
        if (_trayIcon is not null)
        {
            return;
        }

        try
        {
            CreateTrayIcon();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Log("System tray initialization failed", exception);
        }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ShowAbout()
    {
        if (_aboutWindow is { IsLoaded: true })
        {
            if (_aboutWindow.WindowState == WindowState.Minimized)
            {
                _aboutWindow.WindowState = WindowState.Normal;
            }

            _aboutWindow.Activate();
            return;
        }

        _aboutWindow = new AboutWindow();
        if (IsVisible)
        {
            _aboutWindow.Owner = this;
            _aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        _aboutWindow.Closed += (_, _) => _aboutWindow = null;
        _aboutWindow.Show();
        _aboutWindow.Activate();
    }

    private void OnWindowDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnChromeMouseDown(
        object sender,
        MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void OnMinimizeClicked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private async void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(
            _settings.DeadZone,
            _settings.WarningDelaySeconds)
        {
            Owner = this,
        };

        if (settingsWindow.ShowDialog() != true ||
            settingsWindow.SavedValues is not MonitorSettingsValues values)
        {
            return;
        }

        try
        {
            _settings = _settings with
            {
                DeadZone = values.DeadZone,
                WarningDelaySeconds = values.WarningDelaySeconds,
            };
            await _settingsStore.SaveAsync(_settings);

            System.Windows.MessageBox.Show(
                this,
                "Settings saved. Changes will be applied the next time Upright starts.",
                "Upright",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError("Settings could not be saved", exception);
        }
    }

    private void OnCloseWindowClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void QuitApplication()
    {
        _allowClose = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void ShowError(string heading, Exception exception)
    {
        string message = $"{heading}: {exception.Message}";
        System.Windows.MessageBox.Show(
            this,
            message,
            "Upright",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    internal void SetDisplayStatusForPreview(
        MonitorDisplayStatus status) =>
        SetDisplayStatus(status);

    private void SetDisplayStatus(MonitorDisplayStatus status)
    {
        MonitorDisplayStatusPresentation presentation =
            MonitorDisplayStatusPresentation.From(status);
        StatusText.Text = presentation.Text;
        StatusText.Foreground = new SolidColorBrush(presentation.TextColor);
        StatusDot.Fill = new SolidColorBrush(presentation.DotColor);
        WorkingIndicator.Visibility = Visibility.Visible;
    }
}
