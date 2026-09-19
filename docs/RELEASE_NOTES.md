# Upright Windows release notes

## 1.0.18

- Fixed the Microsoft Store build silently exiting before its main window
  appeared.
- Deferred notification-area initialization until after the main window has
  loaded, and made tray initialization non-fatal.
- Added early-startup exception logging at
  `%LOCALAPPDATA%\Upright\startup.log` so future launch failures report a
  concrete cause instead of closing without feedback.
- Preserved camera inference, calibration, posture decisions, thresholds, and
  warning behavior.

## 1.0.17

- Added a live camera preview inside the full-screen calibration card.
- Added a confidence-gated nose marker over the calibration preview.
- Added main-window guidance recommending good lighting and avoiding facial reflections.
- Preserved the existing posture detection, calibration sampling, thresholds, and warning behavior.

## 1.0.16

- Corrected the current model attribution in bundled notices and documentation
  to `YOLOX-Nano HumanArt` and `RTMPose-T Body17`; references to the former
  YOLOX-Tiny/RTMPose-S pair remain only where they describe release history.
- Renamed the Windows solution, projects, source directories, test directories,
  namespaces, assemblies, executable, icon source paths, package identity, and
  portable archive to `Upright`.
- Moved active settings storage to
  `%LOCALAPPDATA%\Upright\settings.json`.
- Added compatibility loading from the former settings location so existing
  camera calibration and preferences are copied automatically.
- Renamed the bundled upstream license path to
  `licenses/UPSTREAM-MIT-LICENSE.txt` while retaining its original copyright
  and MIT terms.
- Kept the macOS reference archive and upstream identity unchanged for
  provenance and license compliance.
- Camera inference, calibration, posture decisions, settings ranges, and
  warning behavior are unchanged.

## 1.0.15

- Added `Settings` beside the main-window minimize and close controls.
- Added numeric inputs for `Dead zone` and `Alert delay (seconds)`.
- Displayed each setting's default value, accepted range, and purpose beside
  its input.
- Added inline range validation: dead zone accepts `0.00–0.20`, and alert
  delay accepts `0–30` seconds.
- Saved changes without modifying the active monitoring session and clearly
  stated that the new values apply the next time Upright starts.
- Fixed recalibration so it consistently uses the monitoring values captured
  when the application started instead of temporarily reverting to defaults.
- Retained the 1.0.14 lightweight model pair and all posture-decision rules.

## 1.0.14

- Replaced YOLOX-Tiny HumanArt with YOLOX-Nano HumanArt and RTMPose-S
  Body17 with RTMPose-T Body17.
- Reduced the bundled model payload from 40.22 MiB to 16.28 MiB.
- Preserved the 416x416 detector input, 256x192 pose input, 17-keypoint
  SimCC output, body-first decision order, calibration, and posture rules.
- On the reference CPU and fixed test image, the mean five-run pipeline time
  decreased from 117.55 ms to 77.26 ms.
- This lightweight model edition can be less reliable for distant, occluded,
  or low-light people than 1.0.13; version 1.0.13 remains the stable fallback.

## 1.0.13

- Changed the complete About interface and disclosure copy from Chinese to English.
- Retained the concise product, local-processing privacy, version, Naryso copyright, MIT, and third-party notice structure introduced in 1.0.12.
- Camera inference, calibration, posture decisions, warning timing, and privacy behavior are unchanged.

## 1.0.12

- Replaced the About window body with the requested concise Chinese product and privacy description.
- Removed the former medical, detector-detail, and component-by-component About cards.
- Added the dynamic `Upright <version>` line, `Copyright © 2026 Naryso`, the MIT open-source statement, and the reference to `开源许可与第三方声明`.
- Changed the Upright copyright holder in the root MIT License and NOTICE from the provisional `Upright Contributors` label to `Naryso`.
- Camera inference, calibration, posture decisions, warning timing, and privacy behavior are unchanged.

## 1.0.11

- Released the Upright Windows source distribution under the MIT License.
- Added a root `LICENSE` containing the retained Dorso/Posturr copyright and the new Naryso copyright.
- Added `NOTICE`, `docs/OPEN_SOURCE.md`, and a structured `THIRD_PARTY_NOTICES.md` that distinguish project code from upstream code, model files, and runtime dependencies.
- Made the Windows repository self-contained for licensing by tracking the Dorso MIT text and MMPose Apache-2.0 text instead of depending on the adjacent macOS reference directory.
- Updated About Upright to identify the application as open-source MIT software and point to the complete bundled license set.
- Camera inference, calibration, posture decisions, warning timing, and privacy behavior are unchanged.

## 1.0.10

- Added `About Upright` to the notification-area right-click menu.
- Added a blue-white rounded About window with product purpose and version.
- Disclosed the wellness-only, non-medical-device limitation and clarified that the app does not clinically measure thoracic kyphosis.
- Disclosed local in-memory camera processing, no raw-frame storage, and the absence of accounts, advertising, analytics, cloud, and update services.
- Added Dorso/Posturr, MMPose model, ONNX Runtime, and third-party license attribution.
- Added `THIRD_PARTY_NOTICES.md` and bundled the relevant upstream license and notice files with the portable release.
- Camera inference, calibration, posture decisions, and warning behavior are unchanged.

## 1.0.9

- Added four persistent English status states: Tool inactive, Calibrating, Monitoring active, and Poor posture detected.
- Added gray, blue, green, and red status indicators that follow the application and posture-warning state.
- Replaced unavailable Fluent icon-font glyphs with Windows-safe minimize and close characters.
- Visually aligned the `Posture Monitor` subtitle with the supplied Upright wordmark.
- Removed the bottom display-corner/camera-capture note from the calibration screen.
- Detection thresholds, calibration sampling, and posture-decision logic are unchanged.

## 1.0.8

- Replaced the display-sized warning window with four synchronized, non-overlapping edge windows per screen.
- The red warning now reaches the complete physical screen edge, including over the taskbar, while the Windows taskbar remains visible and clickable.
- Kept every edge click-through, non-activating, and out of the task switcher.
- Rebuilt the main window to match the selected white reference layout.
- Replaced the repeated Dorso icon/name lockup with the supplied Upright wordmark and placed `Posture Monitor` directly beneath it.
- Added separate Camera and active Status sections while retaining only camera selection, calibration, and Start/Stop controls.
- Detection, calibration, warning targets, and posture-decision logic are unchanged.

## 1.0.7

- Changed warning overlays from full display bounds to the Windows working area.
- The Windows taskbar and any other reserved desktop edge are now excluded from the red warning window.
- Added runtime diagnostics that compare each overlay's native bounds with its screen working area.
- Calibration remains true full-screen and is unaffected.

## 1.0.6

- Converted the main interface into a true transparent, borderless rounded window instead of placing a rounded card inside a square system window.
- Reworked the palette to blue and white only, with a separate green Working indicator while the camera pipeline is active.
- Added custom rounded minimize and close controls and a draggable title area.
- Increased the main-window content space and verified that Calibrate and Start/Stop remain fully inside the rounded window.
- Changed the calibration screen to a transparent desktop overlay with a wider white instruction card.
- Enabled title wrapping and increased calibration card/button widths so all instructions remain visible.

## 1.0.5

- Rebuilt the main window as a compact blue, white, and yellow control panel with rounded cards and controls.
- The visible interface now contains only camera selection, calibration, and Start/Stop monitoring.
- Removed the camera preview, nose and face markers, diagnostic measurements, status panel, settings expander, pause button, and refresh button.
- Camera devices refresh automatically when the selector is opened while monitoring is stopped.
- Clicking Calibrate starts the selected camera automatically when needed.
- Removed preview bitmap copying and diagnostic-render updates while preserving the camera inference and posture-warning pipeline.

## 1.0.4

- Replaced immediate warning-edge opacity jumps with time-based interpolation.
- The red edge now takes approximately 1.35 seconds to reach 95% of its target intensity.
- Removed the previous nonzero-opacity step at warning onset, so the first visible frame starts near transparent.
- Recovery still clears the warning smoothly within approximately one second.
- Posture detection thresholds, warning onset decisions, and severity calculations are unchanged.

## 1.0.3

- Changed all user-facing Windows application text to English.
- Updated the main window, monitoring settings, runtime status messages, errors, notification-area menu, and full-screen calibration interface.
- Widened controls where needed for the English labels.
- Preserves the camera pipeline, calibration calculations, and posture-monitoring behavior.

## 1.0.2

- Moves calibration out of the camera preview into a dedicated full-screen window.
- Places the four calibration targets relative to the active display corners in clockwise order.
- Supports Space to sample, Escape to cancel, and direct target clicking.
- Keeps the camera running behind the calibration screen and preserves all calibration formulas and posture-decision behavior.

## 1.0.1

- Replaced the solid red border and uniform red screen tint with a continuous edge-to-center fade.
- Uses a smootherstep opacity curve sampled three times per device-independent pixel.
- Draws all four edges as closed concentric contours, eliminating overlapping edge brushes and visible corner seams.
- Preserves warning severity, timing, click-through behavior, and posture detection logic.

## 1.0.0

## Included

- local camera-based posture monitoring;
- original body-first, face-fallback decision order;
- original calibration, smoothing, hysteresis, consecutive-frame, severity, and away logic;
- guided camera-specific calibration;
- notification-area controls and persisted settings;
- multi-monitor click-through warning overlays;
- local ONNX pose inference with CPU fallback;
- self-contained Windows x64 portable build.

## Intentionally removed

- AirPods and Bluetooth tracking;
- Apple-only permissions and frameworks;
- automatic camera/headphone source switching;
- macOS private blur implementation.

## Known limitations

- Detection measures the original nose/face-position proxy; it is not a clinical thoracic-kyphosis measurement.
- DirectML is unavailable on the reference adapter/driver, so the verified build uses CPU inference.
- Broader hardware and Windows 11 qualification remains outstanding as listed in `TEST_MATRIX.md`.
- The MSIX artifact is unsigned until certificate use is explicitly authorized; use the portable archive for the current self-use release.
