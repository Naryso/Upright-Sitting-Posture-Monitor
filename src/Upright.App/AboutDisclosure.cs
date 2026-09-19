using System.Reflection;

namespace Upright.App;

public static class AboutDisclosure
{
    public const string ProductName = "Upright! - Sitting Posture Monitor";

    public const string Purpose =
        "Upright is a camera-based posture reminder that runs locally on Windows,\n" +
        "helping you maintain healthy sitting habits while using your computer.";

    public const string PrivacyNotice =
        "All images and posture data are processed locally on your device.\n" +
        "Upright does not connect to the internet or save any camera images.";

    public const string CopyrightNotice = "Copyright © 2026 Naryso";

    public const string LicenseNotice =
        "This software is open source under the MIT License.";

    public const string ThirdPartyNotice =
        "For copyright and license information for third-party components, see\n" +
        "“Open-Source Licenses and Third-Party Notices.”";

    public static string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version is Version version
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "Unknown";

    public static string ProductVersion => $"{ProductName} {Version}";
}
