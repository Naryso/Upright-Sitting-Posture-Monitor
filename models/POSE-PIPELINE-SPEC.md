# Selected pose inference specification

## YOLOX-Nano HumanArt person detector

1. Accept an in-memory BGRA camera frame.
2. Resize with bilinear interpolation by
   `min(416 / sourceHeight, 416 / sourceWidth)`.
3. Place the resized BGR image at the upper-left of a 416x416 canvas.
4. Fill unused right/bottom pixels with `(114, 114, 114)`.
5. Run the NCHW float32 tensor without mean/std scaling.
6. Select the highest detection score strictly greater than `0.7`.

## RTMPose-T Body17 pose estimator

1. Expand the detected person box by `1.25`.
2. Adjust the crop to the model aspect ratio `192 / 256`.
3. Affine-resample to 192x256 with bilinear interpolation.
4. Convert BGR to RGB and normalize with mean
   `(123.675, 116.28, 103.53)` and std `(58.395, 57.12, 57.375)`.
5. Decode COCO keypoint index `0` from `simcc_x` and `simcc_y`, using the
   average peak value as nose confidence and split ratio `2.0`.
6. Map the nose back through the expanded person box.
7. Normalize to the source frame and convert to bottom-origin coordinates:
   `yFromBottom = 1 - yFromTop`.

The pose path supplies no face width. If a body observation exists but its nose
confidence is not strictly greater than `0.3`, the face fallback must not run.

## Face fallback

When the person detector has no body observation, the app runs the local Windows
`FaceDetector` on an in-memory bitmap. The first returned face supplies its
normalized bounding-box midpoint Y and width. Camera frames are not persisted.
