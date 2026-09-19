using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Upright.App;

public sealed class WarningOverlayManager : IDisposable
{
    private readonly Dictionary<string, WarningOverlayWindow> _windows =
        new(StringComparer.Ordinal);
    private readonly DispatcherTimer _transitionTimer;
    private long _lastTransitionTimestamp;
    private double _currentSeverity;
    private double _targetSeverity;
    private bool _disposed;

    public WarningOverlayManager()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _transitionTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            OnTransitionTick,
            System.Windows.Application.Current.Dispatcher);
    }

    public int WindowCount => _windows.Count;

    public IReadOnlyList<WarningOverlayDiagnostic> GetDiagnostics() =>
        _windows.Values
            .Select(window => new WarningOverlayDiagnostic(
                window.DeviceName,
                window.IsVisible,
                window.Opacity,
                window.HasClickThroughStyle,
                window.HasNoActivateStyle,
                window.HasToolWindowStyle,
                window.Edge,
                window.DisplayBounds,
                window.WorkingArea,
                window.ExpectedBounds,
                window.WindowBounds))
            .ToArray();

    public void Update(double severity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _targetSeverity = Math.Clamp(severity, 0, 1);
        RefreshDisplays();
        StartTransition();
    }

    public void Clear()
    {
        if (_disposed)
        {
            return;
        }

        _targetSeverity = 0;
        StartTransition();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _transitionTimer.Stop();
        foreach (WarningOverlayWindow window in _windows.Values)
        {
            window.Close();
        }

        _windows.Clear();
        GC.SuppressFinalize(this);
    }

    private void RefreshDisplays()
    {
        Forms.Screen[] screens = Forms.Screen.AllScreens;
        var activeNames = screens
            .SelectMany(screen => Enum.GetValues<WarningEdge>()
                .Select(edge => CreateKey(screen.DeviceName, edge)))
            .ToHashSet(StringComparer.Ordinal);

        foreach (string removed in _windows.Keys
                     .Where(name => !activeNames.Contains(name))
                     .ToArray())
        {
            _windows[removed].Close();
            _windows.Remove(removed);
        }

        foreach (Forms.Screen screen in screens)
        {
            foreach (WarningEdge edge in Enum.GetValues<WarningEdge>())
            {
                string key = CreateKey(screen.DeviceName, edge);
                if (!_windows.ContainsKey(key))
                {
                    _windows.Add(
                        key,
                        new WarningOverlayWindow(screen, edge));
                    _windows[key].SetSeverity(_currentSeverity);
                }
            }
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (!_disposed)
            {
                RefreshDisplays();
                foreach (WarningOverlayWindow window in _windows.Values)
                {
                    window.SetSeverity(_currentSeverity);
                }
            }
        });
    }

    private void StartTransition()
    {
        if (_targetSeverity > 0 && _currentSeverity == 0)
        {
            foreach (WarningOverlayWindow window in _windows.Values)
            {
                window.SetSeverity(double.Epsilon);
            }
        }

        if (!_transitionTimer.IsEnabled)
        {
            _lastTransitionTimestamp = Stopwatch.GetTimestamp();
            _transitionTimer.Start();
        }
    }

    private void OnTransitionTick(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(
            _lastTransitionTimestamp,
            now);
        _lastTransitionTimestamp = now;
        _currentSeverity = WarningSeverityTransition.Advance(
            _currentSeverity,
            _targetSeverity,
            elapsed);

        foreach (WarningOverlayWindow window in _windows.Values)
        {
            window.SetSeverity(_currentSeverity);
        }

        if (_currentSeverity == _targetSeverity)
        {
            _transitionTimer.Stop();
        }
    }

    private static string CreateKey(
        string deviceName,
        WarningEdge edge) =>
        $"{deviceName}|{edge}";
}

public sealed record WarningOverlayDiagnostic(
    string DeviceName,
    bool IsVisible,
    double Opacity,
    bool HasClickThroughStyle,
    bool HasNoActivateStyle,
    bool HasToolWindowStyle,
    WarningEdge Edge,
    System.Drawing.Rectangle DisplayBounds,
    System.Drawing.Rectangle WorkingArea,
    System.Drawing.Rectangle ExpectedBounds,
    System.Drawing.Rectangle WindowBounds);
