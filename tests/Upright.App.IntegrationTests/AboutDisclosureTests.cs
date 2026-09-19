using Upright.App;

namespace Upright.App.IntegrationTests;

public sealed class AboutDisclosureTests
{
    [Fact]
    public void PurposeMatchesRequestedEnglishCopy()
    {
        Assert.Equal(
            "Upright is a camera-based posture reminder that runs locally on Windows,\n" +
            "helping you maintain healthy sitting habits while using your computer.",
            AboutDisclosure.Purpose);
    }

    [Fact]
    public void PrivacyNoticeMatchesRequestedEnglishCopy()
    {
        Assert.Equal(
            "All images and posture data are processed locally on your device.\n" +
            "Upright does not connect to the internet or save any camera images.",
            AboutDisclosure.PrivacyNotice);
    }

    [Fact]
    public void CopyrightAndLicenseCopyMatchesRequestedText()
    {
        Assert.Equal("Copyright © 2026 Naryso", AboutDisclosure.CopyrightNotice);
        Assert.Equal(
            "This software is open source under the MIT License.",
            AboutDisclosure.LicenseNotice);
        Assert.Contains(
            "“Open-Source Licenses and Third-Party Notices.”",
            AboutDisclosure.ThirdPartyNotice,
            StringComparison.Ordinal);
    }
}
