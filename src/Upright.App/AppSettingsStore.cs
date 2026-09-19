using System.IO;
using System.Text.Json;
using Upright.Core;

namespace Upright.App;

public sealed record UprightAppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string? CameraId { get; init; }
    public CameraCalibrationData? CameraCalibration { get; init; }
    public double DeadZone { get; init; } =
        MonitorSettingsInput.DefaultDeadZone;
    public double WarningDelaySeconds { get; init; } =
        MonitorSettingsInput.DefaultWarningDelaySeconds;
    public double WarningIntensity { get; init; } = 1;
    public bool AwayDetectionEnabled { get; init; } = true;
    public bool MonitoringPaused { get; init; }
}

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _path;
    private readonly string? _legacyPath;

    public AppSettingsStore(string? path = null, string? legacyPath = null)
    {
        string localAppData =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _path = path ?? System.IO.Path.Combine(
            localAppData,
            "Upright",
            "settings.json");
        _legacyPath = legacyPath ?? (path is null
            ? System.IO.Path.Combine(localAppData, "Dorso", "settings.json")
            : null);
    }

    public string Path => _path;

    public async Task<UprightAppSettings> LoadAsync()
    {
        string? sourcePath = File.Exists(_path)
            ? _path
            : _legacyPath is not null && File.Exists(_legacyPath)
                ? _legacyPath
                : null;
        if (sourcePath is null)
        {
            return new UprightAppSettings();
        }

        try
        {
            string json = await File.ReadAllTextAsync(sourcePath);
            UprightAppSettings? settings =
                JsonSerializer.Deserialize<UprightAppSettings>(json, SerializerOptions);
            UprightAppSettings sanitized = Sanitize(settings);
            if (!string.Equals(sourcePath, _path, StringComparison.OrdinalIgnoreCase))
            {
                await SaveAsync(sanitized);
            }

            return sanitized;
        }
        catch (JsonException)
        {
            return new UprightAppSettings();
        }
        catch (IOException)
        {
            return new UprightAppSettings();
        }
    }

    public async Task SaveAsync(UprightAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        UprightAppSettings sanitized = Sanitize(settings);
        string? directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = _path + ".tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(sanitized, SerializerOptions));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    private static UprightAppSettings Sanitize(UprightAppSettings? settings)
    {
        if (settings is null ||
            settings.SchemaVersion != UprightAppSettings.CurrentSchemaVersion)
        {
            return new UprightAppSettings();
        }

        CameraCalibrationData? calibration =
            settings.CameraCalibration?.IsValid == true
                ? settings.CameraCalibration
                : null;
        return settings with
        {
            SchemaVersion = UprightAppSettings.CurrentSchemaVersion,
            CameraCalibration = calibration,
            DeadZone = Math.Clamp(
                settings.DeadZone,
                MonitorSettingsInput.MinimumDeadZone,
                MonitorSettingsInput.MaximumDeadZone),
            WarningDelaySeconds = Math.Clamp(
                settings.WarningDelaySeconds,
                MonitorSettingsInput.MinimumWarningDelaySeconds,
                MonitorSettingsInput.MaximumWarningDelaySeconds),
            WarningIntensity = Math.Clamp(settings.WarningIntensity, 0.25, 4),
        };
    }
}
