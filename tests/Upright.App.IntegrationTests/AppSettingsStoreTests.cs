using Upright.Core;

namespace Upright.App.IntegrationTests;

public sealed class AppSettingsStoreTests
{
    [Fact]
    public async Task RoundTripPreservesVersionedCameraSettings()
    {
        string path = TemporarySettingsPath();
        var store = new AppSettingsStore(path);
        var expected = new UprightAppSettings
        {
            CameraId = "camera-1",
            CameraCalibration = new CameraCalibrationData(
                0.7,
                0.3,
                0.5,
                0.4,
                "camera-1",
                0.2),
            DeadZone = 0.04,
            WarningDelaySeconds = 2,
            WarningIntensity = 1.5,
            AwayDetectionEnabled = false,
            MonitoringPaused = true,
        };

        await store.SaveAsync(expected);
        UprightAppSettings actual = await store.LoadAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task CorruptFileFallsBackToSafeDefaults()
    {
        string path = TemporarySettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{not-json");

        UprightAppSettings settings = await new AppSettingsStore(path).LoadAsync();

        Assert.Equal(new UprightAppSettings(), settings);
    }

    [Fact]
    public async Task ValuesAreSanitizedBeforePersistence()
    {
        string path = TemporarySettingsPath();
        var store = new AppSettingsStore(path);

        await store.SaveAsync(
            new UprightAppSettings
            {
                DeadZone = 99,
                WarningDelaySeconds = -5,
                WarningIntensity = 0,
            });
        UprightAppSettings actual = await store.LoadAsync();

        Assert.Equal(0.20, actual.DeadZone);
        Assert.Equal(0, actual.WarningDelaySeconds);
        Assert.Equal(0.25, actual.WarningIntensity);
    }

    [Fact]
    public async Task LegacySettingsAreCopiedToTheUprightPath()
    {
        string uprightPath = TemporarySettingsPath();
        string legacyPath = TemporarySettingsPath();
        var expected = new UprightAppSettings
        {
            CameraId = "legacy-camera",
            DeadZone = 0.05,
            WarningDelaySeconds = 3,
        };
        await new AppSettingsStore(legacyPath).SaveAsync(expected);

        UprightAppSettings actual =
            await new AppSettingsStore(uprightPath, legacyPath).LoadAsync();

        Assert.Equal(expected, actual);
        Assert.True(File.Exists(uprightPath));
        Assert.True(File.Exists(legacyPath));
    }

    private static string TemporarySettingsPath() =>
        Path.Combine(
            Path.GetTempPath(),
            "Upright.Tests",
            Guid.NewGuid().ToString("N"),
            "settings.json");
}
