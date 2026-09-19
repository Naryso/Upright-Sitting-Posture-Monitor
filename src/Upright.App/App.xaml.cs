using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Upright.App;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        int smokeIndex = Array.IndexOf(e.Args, "--camera-smoke");
        if (smokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (smokeIndex + 1 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--camera-smoke requires an output JSON path.");
                }

                exitCode = await CameraDiagnosticRunner.RunAsync(
                    e.Args[smokeIndex + 1]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(smokeIndex + 1) ??
                            Path.Combine(AppContext.BaseDirectory, "camera-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int imageSmokeIndex = Array.IndexOf(e.Args, "--image-smoke");
        if (imageSmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (imageSmokeIndex + 2 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--image-smoke requires input image and output JSON paths.");
                }

                exitCode = await ImageDiagnosticRunner.RunAsync(
                    e.Args[imageSmokeIndex + 1],
                    e.Args[imageSmokeIndex + 2]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(imageSmokeIndex + 2) ??
                            Path.Combine(AppContext.BaseDirectory, "image-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int overlaySmokeIndex = Array.IndexOf(e.Args, "--overlay-smoke");
        if (overlaySmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (overlaySmokeIndex + 1 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--overlay-smoke requires an output JSON path.");
                }

                exitCode = await WarningOverlaySmokeRunner.RunAsync(
                    e.Args[overlaySmokeIndex + 1]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(overlaySmokeIndex + 1) ??
                            Path.Combine(AppContext.BaseDirectory, "overlay-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int calibrationSmokeIndex = Array.IndexOf(
            e.Args,
            "--calibration-window-smoke");
        if (calibrationSmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (calibrationSmokeIndex + 1 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--calibration-window-smoke requires an output JSON path.");
                }

                exitCode = await CalibrationWindowSmokeRunner.RunAsync(
                    e.Args[calibrationSmokeIndex + 1]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(calibrationSmokeIndex + 1) ??
                            Path.Combine(
                                AppContext.BaseDirectory,
                                "calibration-window-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int mainWindowSmokeIndex = Array.IndexOf(
            e.Args,
            "--main-window-smoke");
        if (mainWindowSmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (mainWindowSmokeIndex + 1 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--main-window-smoke requires an output PNG path.");
                }

                exitCode = await MainWindowSmokeRunner.RunAsync(
                    e.Args[mainWindowSmokeIndex + 1]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(mainWindowSmokeIndex + 1) ??
                            Path.Combine(
                                AppContext.BaseDirectory,
                                "main-window-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int aboutWindowSmokeIndex = Array.IndexOf(
            e.Args,
            "--about-window-smoke");
        if (aboutWindowSmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (aboutWindowSmokeIndex + 1 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--about-window-smoke requires an output PNG path.");
                }

                exitCode = await AboutWindowSmokeRunner.RunAsync(
                    e.Args[aboutWindowSmokeIndex + 1]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(aboutWindowSmokeIndex + 1) ??
                            Path.Combine(
                                AppContext.BaseDirectory,
                                "about-window-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int settingsWindowSmokeIndex = Array.IndexOf(
            e.Args,
            "--settings-window-smoke");
        if (settingsWindowSmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (settingsWindowSmokeIndex + 1 >= e.Args.Length)
                {
                    throw new ArgumentException(
                        "--settings-window-smoke requires an output PNG path.");
                }

                exitCode = await SettingsWindowSmokeRunner.RunAsync(
                    e.Args[settingsWindowSmokeIndex + 1]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(settingsWindowSmokeIndex + 1) ??
                            Path.Combine(
                                AppContext.BaseDirectory,
                                "settings-window-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        int soakSmokeIndex = Array.IndexOf(e.Args, "--soak-smoke");
        if (soakSmokeIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            int exitCode = 1;
            try
            {
                if (soakSmokeIndex + 2 >= e.Args.Length ||
                    !double.TryParse(
                        e.Args[soakSmokeIndex + 1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double seconds))
                {
                    throw new ArgumentException(
                        "--soak-smoke requires duration seconds and output JSON path.");
                }

                exitCode = await SoakDiagnosticRunner.RunAsync(
                    TimeSpan.FromSeconds(seconds),
                    e.Args[soakSmokeIndex + 2]);
            }
            catch (Exception exception)
            {
                try
                {
                    await File.WriteAllTextAsync(
                        e.Args.ElementAtOrDefault(soakSmokeIndex + 2) ??
                            Path.Combine(AppContext.BaseDirectory, "soak-smoke-error.txt"),
                        exception.ToString());
                }
                catch
                {
                    // Preserve the original failure as the process exit code.
                }
            }

            Shutdown(exitCode);
            return;
        }

        try
        {
            var mutex = new Mutex(
                initiallyOwned: true,
                name: @"Local\Upright.Windows.CameraMonitor",
                createdNew: out bool createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                Shutdown(0);
                return;
            }

            _singleInstanceMutex = mutex;
            new MainWindow().Show();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Log("Application startup failed", exception);
            System.Windows.MessageBox.Show(
                "Upright could not start. A diagnostic log was written to:\n\n" +
                StartupDiagnostics.LogPath + "\n\n" + exception.Message,
                "Upright",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        StartupDiagnostics.Log("Unhandled UI exception", e.Exception);
    }

    private static void OnUnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            StartupDiagnostics.Log("Unhandled application exception", exception);
        }
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        StartupDiagnostics.Log("Unobserved task exception", e.Exception);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
