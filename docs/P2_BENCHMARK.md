# P2 camera and inference benchmark

Date: 2026-07-25  
Machine: Windows 10 build 19045, x64  
Runtime: .NET 10.0.10, ONNX Runtime DirectML 1.24.4

## Candidate comparison

| Candidate | CPU measurement | Result |
|---|---:|---|
| RTMO-S 640x640 one-stage | 104.8–114.5 ms raw inference; preprocessing approximately 41–58 ms | Rejected for CPU throughput |
| YOLOX-Tiny 416x416 | 28.3 ms raw inference average | Selected person detector |
| RTMPose-S 256x192 | 6.8 ms raw inference average | Selected pose estimator |
| Selected two-stage pair | 35.0 ms combined raw average | Selected |
| Windows FaceDetector | Runs only when no body is found | Selected fallback |

DirectML initialization returned HRESULT `0x887A0004` on the reference machine,
so the required CPU fallback was used. This is a hardware/driver capability
result, not an inference failure.

## Live camera smoke result

Evidence: generated local artifact `artifacts/camera-smoke-20260725.json`.

- camera count: 1;
- camera initialization: 243.1 ms;
- delivered frame: 640x480 BGRA;
- status: passed;
- provider: CPU fallback;
- five processed samples completed;
- warm sample: 106.2 ms;
- subsequent complete pose/face-fallback pipeline samples: 39.0–43.8 ms;
- sustained warm throughput implied by those samples: approximately 22.8–25.6 FPS;
- face fallback returned normalized bottom-origin midpoint Y and width;
- no raw camera frame was written.

## Pose coordinate result

Evidence: generated local artifact `artifacts/image-smoke-20260725.json`.

The pinned OpenMMLab validation image produced:

- body confidence: 0.9488;
- nose confidence: 0.8881;
- normalized nose X: 0.3825;
- bottom-origin nose Y: 0.9075;
- complete pipeline: 86.0 ms including cold-path preprocessing;
- status: passed.

The reported position maps to the visible face/nose area in the 218x346 source
image. This verifies model-to-source mapping and top-origin to bottom-origin
conversion.

## P2 gate disposition

| Requirement | Evidence | Result |
|---|---|---|
| Camera initializes under 3 seconds | 243.1 ms | Pass |
| At least 10 processed FPS | 39.0–43.8 ms warm live samples | Pass |
| Nose and face outputs normalized | image and live smoke JSON | Pass |
| CPU fallback works | both smoke runs used CPU | Pass |
| Frames not persisted or transmitted | smoke runner records metrics only; no network path | Pass |
| Model redistribution clear and pinned | `models/manifest.json`, Apache-2.0 notice, hashes | Pass |
| Bottom-origin parity coordinates | nose image smoke and face live smoke | Pass |

## Version 1.0.14 lightweight-model supplement

Date: 2026-07-29

| Metric | 1.0.13 models | 1.0.14 models | Change |
|---|---:|---:|---:|
| Detector | YOLOX-Tiny | YOLOX-Nano | smaller model |
| Pose estimator | RTMPose-S | RTMPose-T | smaller model |
| Combined ONNX size | 40.22 MiB | 16.28 MiB | -59.5% |
| Five-run mean complete pipeline | 117.55 ms | 77.26 ms | -34.3% |
| Fixed-image body confidence | 0.9254 | 0.8536 | -0.0718 |
| Fixed-image nose confidence | 0.8208 | 0.8926 | +0.0718 |

Both pairs were run through the same published 1.0.13 executable, CPU
provider, preprocessing, postprocessing, and fixed OpenMMLab test image. All
five lightweight runs returned a body observation and a usable nose.

The single-image confidence comparison is a compatibility check, not an
accuracy claim. OpenMMLab reports lower reference AP for YOLOX-Nano than
YOLOX-Tiny and for RTMPose-T than RTMPose-S. Low light, distance, occlusion,
and live camera stability remain the important regression risks.
