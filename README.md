# Upright! - Sitting Posture Monitor

[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-Download-0078D4?logo=microsoft)](https://apps.microsoft.com/detail/9NCGJ06PV3TK)

Upright is an open-source, camera-only posture reminder for Windows 10 and
Windows 11. It processes webcam frames locally, estimates changes in head
position after calibration, and displays a gradual red warning around the
screen when posture remains outside the calibrated range.

## Features

- local webcam processing with no cloud inference;
- body-pose detection with a Windows face-box fallback;
- four-corner, camera-specific calibration;
- progressive multi-monitor warning edges;
- taskbar-safe, click-through warning overlays;
- compact notification-area application;
- no account, analytics, advertising, or raw-frame storage.

## Install

The recommended release is available from the
[Microsoft Store](https://apps.microsoft.com/detail/9NCGJ06PV3TK). Store
packages are signed and updated by Microsoft.

## Build

Requirements:

- Windows 10 22H2 or later, x64;
- .NET 10 SDK;
- the two model files and hashes specified in `models/manifest.json`.

Model binaries are intentionally not stored in Git. Download the two pinned
OpenMMLab archives listed in `models/manifest.json`, verify their SHA-256
hashes, and place these extracted files in `models/`:

- `yolox-nano-person-416x416.onnx`
- `rtmpose-t-body17-256x192.onnx`

```powershell
dotnet restore .\Upright.Windows.sln
dotnet build .\Upright.Windows.sln -c Release --no-restore
dotnet test .\Upright.Windows.sln -c Release --no-build --no-restore
```

The original macOS implementation is a behavioral reference and is not part of
this Windows source distribution.

## Privacy

Camera frames are processed locally in memory. Raw frames are not saved or
uploaded. Only settings and camera-specific calibration data are stored in
`%LOCALAPPDATA%\Upright\settings.json`. See `docs/PRIVACY.md`.

## License

Upright! - Sitting Posture Monitor is open-source software licensed under the
MIT License. The Windows implementation is adapted from the MIT-licensed Dorso
project, Copyright (c) 2025 Posturr Contributors.

Third-party models and runtime components retain their own licenses. See:

- `LICENSE` — project MIT license;
- `NOTICE` — project and upstream attribution;
- `THIRD_PARTY_NOTICES.md` — dependency and model summary;
- `docs/OPEN_SOURCE.md` — licensing scope and redistribution guidance;
- `licenses/` and `models/licenses/` — full third-party terms.

The license permits commercial and non-commercial use, modification, and
redistribution. Copyright and license notices must be retained.
