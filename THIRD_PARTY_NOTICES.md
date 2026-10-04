# Third-party notices

Audit date: 2026-10-04. This inventory distinguishes repository assets from
dependencies installed by Unity and binaries included in a Windows Player.
The project's copyright notice does not replace any third-party license.

## Noto Sans CJK TC

- File: `UnityProject/Assets/Resources/Fonts/NotoSansCJKtc-Regular.otf`
- Embedded family/version: Noto Sans CJK TC, Version 2.004.
- Embedded copyright: © 2014–2021 Adobe (http://www.adobe.com/).
- Source: [official Noto CJK repository](https://github.com/notofonts/noto-cjk).
- License: SIL Open Font License 1.1; full text in
  [OFL.txt](UnityProject/Assets/Resources/Fonts/OFL.txt).
- Upstream reference: [Noto Sans license](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE).
- SHA-256: `dce08bd4fd91aa8aa76ed8fea4b694c2dfb8550f67871e326843212ddbeb88b4`.

OFL permits redistribution subject to its conditions, including preservation
of the copyright notice and license. Keep the font under OFL, and ship the
notice and complete OFL text with the downloadable Player. Do not relicense
this font as an original EMBER asset.

## Unity dependencies

`UnityProject/Packages/manifest.json` declares Unity built-in audio, IMGUI,
JSON serialization, physics, particles, animation, UI and screen-capture
modules (1.0.0). The lock file additionally resolves built-in imageconversion
(1.0.0). No Asset Store package or third-party registry dependency was found
in these manifests.

Only dependency declarations belong in the source repository. Do not publish
the Unity Editor, Library cache, PackageCache or license/account files.
Unity Runtime distribution as part of a game remains subject to the
[Unity Editor Software Terms](https://unity.com/legal/editor-terms-of-service/software)
and the author's applicable subscription terms. This audit does not verify
the author's subscription or financial tier eligibility.

Windows Player binaries contain Unity, Mono/.NET and graphics runtime
components. They are not original project code. The portfolio package includes the matching [Unity Windows Mono Player notices](https://unity.com/releases/editor/whats-new/6000.2.0f1) as a complete PDF, including Unity's declared Mono and Direct3D components. It also includes this file and the complete font OFL notice. The Unity Application Patcher is an internal audit tool and is not distributed. File inventory and extracted-package verification are recorded in `Docs/DISTRIBUTION.md`; no Unity Editor/cache/account files are shipped.

## Project-generated content

`RootcrownMask.fbx` has a repository generation script at
`Tools/create_mask.py`, which creates mesh geometry in Blender and exports
the result. Blender is a development tool, not a vendored dependency. Its
license does not automatically determine the license of this output mesh.

The scene, Boss, skill and refuge presentation code generates geometry and
effects at runtime. Original project code and generated presentation remain
subject to [COPYRIGHT.md](COPYRIGHT.md). Screenshots in Docs are game captures,
not purchased stock imagery. See the safety audit for the complete historical
asset inventory and any unresolved provenance checks.

## Audit tools

Gitleaks was downloaded locally for the publication safety audit, with its
release checksum verified. The scanner executable and raw audit results are
excluded from the repository and release; it is not a game dependency.
