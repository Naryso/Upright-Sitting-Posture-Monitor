# Open-source licensing

Upright! - Sitting Posture Monitor is open-source software distributed under
the MIT License.

## What the project license covers

The root `LICENSE` covers the Windows source code, tests, documentation, and
project-authored assets in this repository. It permits personal, academic, and
commercial use; modification; redistribution; sublicensing; and sale, provided
that the copyright and permission notice remain with copies or substantial
portions of the software.

The software is provided without warranty. Upright is a wellness reminder, not
a medical device or a clinical measurement of thoracic kyphosis.

## Upstream Dorso attribution

The Windows implementation is adapted from Dorso, Copyright (c) 2025 Posturr
Contributors, which is also distributed under the MIT License. The upstream
copyright notice is retained in the root `LICENSE`, `NOTICE`, and the unmodified
upstream license copy at `licenses/UPSTREAM-MIT-LICENSE.txt`.

## Third-party components

The MIT license for Upright does not replace licenses belonging to dependencies
or model files. Their original terms remain in force:

- MMPose YOLOX-Nano HumanArt and RTMPose-T Body17 models: Apache License 2.0;
- Microsoft ONNX Runtime: MIT License plus its third-party notices;
- Microsoft DirectML: Microsoft license terms plus its third-party notices;
- self-contained .NET runtime: MIT License plus its third-party notices.

See `THIRD_PARTY_NOTICES.md`, `models/manifest.json`, `models/licenses`, and
`licenses` for the corresponding attribution and full texts.

## Binary releases

Do not remove `LICENSE`, `NOTICE`, `THIRD_PARTY_NOTICES.md`, or the `licenses`
directory from a binary release. These files are part of the distributable
package.

## Contributions

Unless a contributor explicitly states otherwise, a contribution intentionally
submitted for inclusion in Upright is provided under the project's MIT License.

## Name and logo

The MIT License grants copyright permissions for the software and
project-authored assets. It does not grant trademark rights in the Upright name
or logo or imply endorsement by Upright, Dorso, Posturr, OpenMMLab, or
Microsoft.
