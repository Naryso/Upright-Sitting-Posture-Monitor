# Upright camera behavior contract

Version: 1.0  
Reference: macOS source extracted under `dorso-main`  
Purpose: preserve camera detection behavior while replacing Apple platform APIs.

## Coordinate and observation contract

- All positions and widths are normalized to `0...1`.
- The core receives Apple Vision-style coordinates with the origin at the bottom-left.
- A Windows model that uses a top-left origin must be adapted with `yFromBottom = 1 - yFromTop`.
- Body pose is attempted before face detection.
- Only the first body observation is considered.
- A nose point is accepted only when confidence is strictly greater than `0.3`.
- Face detection is attempted only when no body observation exists or the body request fails.
- If a body observation exists but the nose is missing or has insufficient confidence, that frame produces no accepted observation and does not use face fallback.
- The body path supplies nose Y and no face width.
- The face path supplies face-box midpoint Y and normalized face-box width.

## Frame contract

- Preferred capture size: 640x480.
- Preferred pixel layout: BGRA.
- Base processing interval: 0.25 seconds.
- Processing interval while the detector-level evaluator is in bad-posture hysteresis: 0.1 seconds.
- Late frames are discarded rather than queued.

## Calibration contract

Given at least four accepted camera samples:

```text
goodPostureY = maximum noseY
badPostureY = minimum noseY
neutralY = arithmetic mean noseY
postureRange = absolute(goodPostureY - badPostureY)
neutralFaceWidth = maximum non-null faceWidth, otherwise 0
```

The calibration object is valid only when `postureRange > 0.01` and the camera ID is not empty. Creation itself may still return an invalid object when the ID is empty; validity is a separate check.

## Per-observation evaluator

1. Add the accepted Y value to a five-value moving window.
2. Compute the arithmetic mean of the available values.
3. Compute `slouchAmount = badPostureY - smoothedY`.
4. Compute `deadZoneThreshold = deadZone * postureRange`.
5. Use the dead-zone threshold when not already bad.
6. Use 70% of the dead-zone threshold while already bad.
7. Vertical posture is bad only when `slouchAmount` is strictly greater than the active threshold.
8. Compute forward-head ratio only when both neutral and current face widths are positive.
9. Forward-head threshold is `1 + max(0.05, deadZone)`.
10. Forward-head severity is the clamped excess ratio divided by `0.15`.
11. Combined severity is the maximum of vertical and forward-head severity.
12. A positive forward-head severity has a minimum combined severity of `0.5`.
13. Vertical severity is clamped to `0...1`.
14. Any bad result enters detector-level bad-posture hysteresis immediately.
15. A good result with zero severity leaves detector-level hysteresis immediately.

## Monitoring engine contract

- Increment the bad counter and clear the good counter on every bad reading.
- Increment the good counter and clear the bad counter on every good reading.
- At eight bad readings, establish the bad-posture start time if missing.
- Apply the configurable onset delay from that start time.
- Enter the warning state once the frame threshold and onset delay have both passed.
- Record a slouch event and request a UI update only on the warning-state transition.
- Warning intensity is `pow(clamp(severity, 0, 1), 1 / intensity)`.
- A non-positive intensity is treated as `1`.
- A good reading clears bad start time and warning intensity immediately.
- Leave the warning state after five consecutive good readings.
- Request a warning renderer update after every reading.
- Analytics attribution uses the warning state before the current reading.

## Away contract

- When enabled, fifteen consecutive no-detection results enter away state.
- An accepted detection resets the no-detection counter.
- An accepted detection leaves away state when away handling is enabled.
- When disabled, no-detection results do not change away state.

## Source traceability

| Swift source | Swift symbol | C# source | C# symbol |
|---|---|---|---|
| `Sources/Detectors/PostureDetector.swift` | `CameraCalibrationSample` | `CameraCalibration.cs` | `CameraCalibrationSample` |
| `Sources/Detectors/PostureDetector.swift` | `CameraCalibrationData` | `CameraCalibration.cs` | `CameraCalibrationData` |
| `Sources/Detectors/CameraPostureDetector.swift` | `createCalibrationData` | `CameraCalibration.cs` | `CameraCalibration.Create` |
| `Sources/Detectors/CameraPostureDetector.swift` | `smoothNoseY` | `PostureEvaluator.cs` | `SmoothNoseY` |
| `Sources/Detectors/CameraPostureDetector.swift` | `evaluatePosture` | `PostureEvaluator.cs` | `PostureEvaluator.Evaluate` |
| `Sources/Core/PostureEngine.swift` | `PostureMonitoringState` | `PostureEngine.cs` | `PostureMonitoringState` |
| `Sources/Core/PostureEngine.swift` | `PostureConfig` | `PostureEngine.cs` | `PostureConfig` |
| `Sources/Core/PostureEngine.swift` | `processReading` | `PostureEngine.cs` | `PostureEngine.ProcessReading` |
| `Sources/Core/PostureEngine.swift` | `processAwayChange` | `PostureEngine.cs` | `PostureEngine.ProcessAwayChange` |
| `Sources/Detectors/CameraPostureDetector.swift` | `handleNoDetection` | `AwayTracker.cs` | `AwayTracker.HandleNoDetection` |

AirPods-specific types and transitions are intentionally not ported.

## P1 invariant test coverage

| Invariant | P1 evidence or deferred gate |
|---|---|
| 640x480 BGRA capture | P2 live camera gate; platform integration concern |
| 0.25 s base / 0.1 s detector-bad cadence | Constants in `PostureEngine`; P3 live throttling gate |
| Body first, first observation, nose confidence > 0.3 | P2/P3 vision adapter tests; model output concern |
| Face fallback decision order | P2/P3 vision adapter tests; model output concern |
| Bottom-origin coordinate conversion | P2/P3 vision adapter tests; preprocessing concern |
| Face `midY` and normalized width | P2/P3 face postprocessing tests |
| Four-sample calibration and aggregations | `CameraCalibrationTests` |
| Five-sample smoothing | `PostureEvaluatorTests.FiveSampleMovingAverageMatchesSwift` |
| Vertical amount and dead-zone threshold | `PostureEvaluatorTests.VerticalPositionBelowThresholdIsBad` |
| Entry/exit hysteresis at 70% | `PositionJustInsideEntryThresholdIsGood` and `ExitThresholdIsSeventyPercentOfEntryThreshold` |
| Forward threshold, 0.15 scale, and 0.5 minimum | `ForwardHeadUsesFaceWidthRatio` and `SmallForwardHeadSeverityHasMinimumPointFive` |
| Maximum combined severity | Forward and vertical evaluator assertions plus replay vectors |
| Eight bad / five good monitor frames | `EighthBadReadingEntersWarning`, `FifthGoodReadingRecovers`, and replay vectors |
| Warning onset and intensity transform | `OnsetDelay*`, `PositiveIntensityAppliesPowerTransform`, and `warning_onset_delay.json` |
| Fifteen-frame away detection | `AwayTrackerTests` and `missing_detection.json` |
| Deterministic identical replay | Every JSON vector is executed twice and the complete result is compared |
