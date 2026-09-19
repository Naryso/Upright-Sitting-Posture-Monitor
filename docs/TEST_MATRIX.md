# Upright Windows release test matrix

Reference environment: Windows 10 22H2 build 19045, x64, .NET SDK 10.0.302, one integrated 640x480 camera, two displays.

| Area | Scenario | Result | Evidence or limitation |
|---|---|---:|---|
| Build | Release solution build | Pass | 0 warnings, 0 errors in isolated output while soak executable held the standard output |
| Automated | Core behavior | Pass | 40 tests |
| Automated | Vision adapter and preprocessing | Pass | 6 tests |
| Automated | App integration/settings/overlay | Pass | 12 tests |
| Camera | Enumerate and initialize integrated camera | Pass | 640x480 BGRA stream; initialization about 216-243 ms |
| Camera | Stop and restart | Pass | restart initialization 166.5 ms; body and nose valid |
| Vision | Full/upper body | Pass | live body observations with usable nose confidence 0.79-0.87 |
| Vision | Static reference image | Pass | body 0.9488; nose 0.8881; bottom-origin nose Y 0.9075 |
| Vision | Face fallback | Pass | live Windows FaceDetector observations |
| Runtime | DirectML unavailable | Pass with fallback | CPU fallback used after DirectML HRESULT 0x887A0004 |
| Performance | 30-second tuned run | Pass | average pipeline 65.10 ms; CPU 4.492%; working set 193.57 MB |
| Runtime | Single instance | Pass | second launch exits; one running instance remains |
| Runtime | Close-to-tray | Pass | main window hides and process remains running |
| Runtime | Warning onset, zero configured delay | Pass | deterministic cadence: 8 bad samples at 250 ms, within 2.0 seconds |
| Runtime | Warning recovery | Pass | deterministic cadence: 5 good samples at 100 ms, within 0.5 seconds |
| Settings | Persist and sanitize preferences/calibration | Pass | app integration tests |
| Overlay | Two-monitor warning | Pass | two overlays; click-through, no-activate, tool-window flags verified |
| Privacy | Process network connections | Pass | no network connection for Upright process throughout observed soak checks |
| Soak | 30-minute continuous camera monitoring | Pass | 1800.77 s; 6706 samples; CPU 4.119%; maximum working set 200.04 MB |
| Camera | Post-soak fresh open | Blocked by current system state | both standard and published builds received `UnauthorizedAccessException`; policy remained Allow and the UVC device remained Started; a permitted FrameServer restart was rejected by Windows |
| Packaging | Self-contained portable x64 | Pass | archive created; published image inference smoke passed |
| Packaging | Unsigned MSIX structure | Pass | official MakeAppx pack/unpack passed; model hashes matched |
| Packaging | Windows App Certification Kit | Not run | tool requires elevation and the MSIX is intentionally unsigned |
| OS | Windows 11 x64 | Not run | not available on the reference machine |
| Camera | External USB webcam | Not run | not available on the reference machine |
| Lifecycle | sleep/wake and lock/unlock | Not run | disruptive manual scenarios deferred |
| Camera faults | denied, occupied, unplugged, reconnect | Partial | UI error and permission routing implemented; hardware cases not all exercised |
| Display | mixed-DPI hot-plug | Partial | two-screen creation and display-change handling implemented; physical hot-plug not exercised |
| Lighting | low light | Not run | environment-dependent |

## Self-use release disposition

The current-machine camera-only build is released for personal use: the 30-minute soak, final standard-path build, automated tests, published image smoke, package hashes, and portable archive passed. The camera also passed start/restart testing before the soak. A fresh camera open after the soak is currently blocked by Windows/device state and should be retried after checking the physical camera privacy control or restarting Windows.

Windows 11, a second external USB camera, sleep/wake, physical hot-plug, mixed-DPI hot-plug, and low-light coverage remain broader-distribution qualification work and are not silently treated as passed.
