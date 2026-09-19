using System.IO;

namespace Upright.App;

internal static class StartupDiagnostics
{
    private static readonly object SyncRoot = new();

    internal static string LogPath
    {
        get
        {
            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Upright", "startup.log");
        }
    }

    internal static void Log(string stage, Exception exception)
    {
        try
        {
            string path = LogPath;
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string entry =
                $"[{DateTimeOffset.Now:O}] {stage}{Environment.NewLine}" +
                exception + Environment.NewLine + Environment.NewLine;
            lock (SyncRoot)
            {
                File.AppendAllText(path, entry);
            }
        }
        catch
        {
            // Startup diagnostics must never become another startup failure.
        }
    }
}
