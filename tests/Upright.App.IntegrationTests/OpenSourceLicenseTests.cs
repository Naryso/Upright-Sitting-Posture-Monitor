namespace Upright.App.IntegrationTests;

public sealed class OpenSourceLicenseTests
{
    [Fact]
    public void ProjectMitLicenseRetainsBothCopyrightNotices()
    {
        string license = ReadOutputFile("LICENSE.txt");

        Assert.Contains("MIT License", license, StringComparison.Ordinal);
        Assert.Contains(
            "Copyright (c) 2025 Posturr Contributors",
            license,
            StringComparison.Ordinal);
        Assert.Contains(
            "Copyright (c) 2026 Naryso",
            license,
            StringComparison.Ordinal);
        Assert.Contains(
            "The above copyright notice and this permission notice shall be included",
            license,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DistributionIncludesProjectAndThirdPartyLicenseSet()
    {
        string[] requiredFiles =
        [
            "NOTICE.txt",
            "OPEN_SOURCE.md",
            "THIRD_PARTY_NOTICES.md",
            Path.Combine("licenses", "UPSTREAM-MIT-LICENSE.txt"),
            Path.Combine(
                "models",
                "licenses",
                "OpenMMLab-Apache-2.0-LICENSE.txt"),
            Path.Combine("licenses", "ONNX-Runtime-MIT-LICENSE.txt"),
            Path.Combine("licenses", "ONNX-Runtime-ThirdPartyNotices.txt"),
            Path.Combine("licenses", "DirectML-LICENSE.txt"),
            Path.Combine("licenses", "DirectML-ThirdPartyNotices.txt"),
            Path.Combine("licenses", "DotNET-MIT-LICENSE.txt"),
            Path.Combine("licenses", "DotNET-ThirdPartyNotices.txt"),
        ];

        foreach (string relativePath in requiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(AppContext.BaseDirectory, relativePath)),
                $"Missing required license file: {relativePath}");
        }
    }

    private static string ReadOutputFile(string relativePath)
    {
        string path = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(path), $"Missing required file: {relativePath}");
        return File.ReadAllText(path);
    }
}
