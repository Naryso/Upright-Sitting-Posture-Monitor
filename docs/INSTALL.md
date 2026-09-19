# Upright Windows installation

## Microsoft Store

Install the signed release from the
[Microsoft Store](https://apps.microsoft.com/detail/9NCGJ06PV3TK). Approve
Windows camera access if prompted, select a camera, and complete calibration
before starting posture monitoring.

Closing the main window keeps Upright in the notification area. Use the tray
menu to reopen the window, view About, or quit.

## Monitoring settings

Use `Settings` in the main-window title bar to enter:

- `Dead zone`: default `0.03`, range `0.00–0.20`; lower values trigger sooner.
- `Alert delay`: default `0` seconds, range `0–30` seconds.

Saved values are applied the next time Upright starts. Quit Upright from the
notification area and reopen it to activate the new values.

The portable build is self-contained. It does not require a separate .NET installation.

## Build from source

Install the .NET 10 SDK. Download and verify the two pinned OpenMMLab model
archives specified in `models/manifest.json`, then place these extracted files
in `models/`:

- `yolox-nano-person-416x416.onnx`
- `rtmpose-t-body17-256x192.onnx`

Build and test from the repository root:

```powershell
dotnet restore .\Upright.Windows.sln
dotnet build .\Upright.Windows.sln -c Release --no-restore
dotnet test .\Upright.Windows.sln -c Release --no-build --no-restore
```

## Camera permission

If Windows denies the camera, open:

`Settings > Privacy > Camera`

Enable camera access and desktop app camera access, then restart Upright.

## Remove Upright

- Microsoft Store build: uninstall Upright from Windows Settings or the Start menu.
- Source build: quit Upright from the tray, then delete the build output.
- Settings are stored separately at `%LOCALAPPDATA%\Upright\settings.json`. Delete that file only if calibration and preferences should also be removed.
