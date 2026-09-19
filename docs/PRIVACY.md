# Upright! - Sitting Posture Monitor privacy statement

Upright uses the selected Windows camera to estimate body and face position for posture monitoring.

- Camera frames are processed locally in memory.
- Raw camera frames are not saved by the application.
- Pose points and face boxes are not transmitted.
- The application has no analytics, advertising, account, cloud, or update service.
- Only user settings and camera-specific calibration values are stored in `%LOCALAPPDATA%\Upright\settings.json`.
- Warning overlays do not capture screen content.

The release qualification includes a process-level network check and a source scan for frame persistence.
