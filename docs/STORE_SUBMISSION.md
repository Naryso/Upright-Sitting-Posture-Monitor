# Microsoft Store submission

## Product identity

- Store product name: `Upright! - Sitting Posture Monitor`
- Package identity name: `Naryso.Upright-SittingPostureMonitor`
- Publisher: `CN=4D9EB1DD-5945-4823-BDEC-8858EC5C2DD5`
- Publisher display name: `Naryso`
- Store ID: `9NCGJ06PV3TK`
- Package family name:
  `Naryso.Upright-SittingPostureMonitor_e58jb1c7p9pt0`

## Restricted capability justification

Paste the following into the Partner Center restricted-capability explanation
for `runFullTrust`:

> Upright is a self-contained x64 WPF desktop application that performs local
> webcam capture, ONNX posture inference, notification-area integration, and
> click-through warning overlays on the Windows desktop. These Win32 desktop
> features require the runFullTrust capability. Upright does not install a
> service or driver, request elevation, modify protected system locations, or
> execute downloaded code. Camera frames and posture observations are
> processed locally and are not saved or transmitted.

## Certification notes

> Upright is a local, camera-based posture reminder for Windows. No account,
> password, subscription, paid content, or internet connection is required.
>
> Test requirements: Windows 10 version 2004 or later, an x64 processor, an
> integrated or USB camera, and permission for desktop applications to access
> the camera.
>
> Launch Upright, select an available camera, and click Calibrate. The
> calibration screen shows a dimmed live camera view and a `Camera preview`
> panel. Look naturally at each of the four full-screen corner targets and
> wait until the yellow nose marker appears with the status `Nose detected —
> ready to capture` (or the face-proxy equivalent). Then press Space or click
> the target to capture each sample. After calibration, monitoring starts
> automatically. Move the head and upper body forward or downward, outside the
> calibrated posture range. The status changes to Posture needs attention and
> a click-through red warning gradient appears around the screen edges. Return
> to the calibrated posture to clear the warning.
>
> The camera preview is shown only during calibration. The main window and
> monitoring mode intentionally do not display camera frames. Closing the main
> window hides Upright in the notification area. Right-click the Upright
> notification-area icon and select Quit to terminate it completely.
>
> All camera images and posture observations are processed locally in memory.
> Camera images are not saved or transmitted. Upright has no analytics,
> advertising, cloud service, user account, or independent updater. Upright is
> a wellness reminder and is not a medical device or diagnostic application.
