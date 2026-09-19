using System.Security.Cryptography;
using System.Text.Json;

namespace Upright.App.IntegrationTests;

public sealed class ModelAssetTests
{
    [Theory]
    [InlineData(
        ModelAssets.DetectorFileName,
        "1450966DE24902B18AADA1A78913D7EFD8FC8DCD51BD4D0D5591476BD4A38821")]
    [InlineData(
        ModelAssets.PoseFileName,
        "A6C2F6A3896A4D51131D14D7A80A3D08B50F559AF5A58A45D5B098AEF510A70F")]
    public void LightweightModelFilesMatchPinnedHashes(
        string fileName,
        string expectedSha256)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "models",
            fileName);

        Assert.True(File.Exists(path), $"Missing model file: {fileName}");
        using FileStream stream = File.OpenRead(path);
        string actual = Convert.ToHexString(SHA256.HashData(stream));
        Assert.Equal(expectedSha256, actual);
    }

    [Fact]
    public void ModelManifestSelectsLightweightPipeline()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "models",
            "manifest.json");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(path));

        Assert.Equal(
            "yolox-nano-person + rtmpose-t-body17",
            manifest.RootElement.GetProperty("pipeline").GetString());
        string[] files = manifest.RootElement
            .GetProperty("models")
            .EnumerateArray()
            .Select(model => model.GetProperty("file").GetString()!)
            .ToArray();

        Assert.Equal(
            [ModelAssets.DetectorFileName, ModelAssets.PoseFileName],
            files);
    }
}
