# Windows portfolio distribution

Release: [v0.3.0-portfolio](https://github.com/oliverchenOVO/ember-agent-rpg/releases/tag/v0.3.0-portfolio). Packaging verification completed on 2026-10-04; the prerelease remains owner-only while the repository is private. Public launch is blocked on the presentation gates in [repository settings](PUBLIC_REPOSITORY_SETTINGS.md).

## Version and security remediation

Gameplay source: `43a2f70` (Team Balance). The package uses the existing Windows Release build; portfolio documentation commits do not change gameplay. Unity 6000.2.0f1 is below the fixed version in [Unity's CVE-2025-59489 advisory](https://unity.com/security/sept-2025-01). The original build stays local. A separate copy was patched using the [official Unity Application Patcher 1.3.3](https://unity.com/security/sept-2025-01/remediation).

The patcher executable's Authenticode signature is valid and identifies Unity Technologies SF. It returned exit 0. Only `UnityPlayer.dll` changed; its SHA-1 appears in Unity's official patched list. No patcher tool, Editor, source or account file is shipped in the zip.

- Original runtime SHA-256: `ab4fa75a60575f13299a29c8d6ecca0a30a7cd7996bb73ea010436f9f5e5476f`
- Patched runtime SHA-256: `d65f4bc3ba5415b1787effe7e9a7c194de0169071ef2f350855278bd8a88fca4`
- Unchanged gameplay assembly SHA-256: `4a75a1e04a804bbdc3639554d4d87c784602b8fa05e2e5a909f096bcc1f2a60d`

## Package contents

`EMBER-Windows-x64.zip` contains Ember.exe, Ember_Data, UnityPlayer.dll, UnityCrashHandler64.exe, MonoBleedingEdge, D3D12, README_FIRST.txt, COPYRIGHT.md, THIRD_PARTY_NOTICES.md, the complete Noto font OFL, and the matching Unity Windows Mono Player third-party notice PDF. No source, PDB, logs, save files or telemetry are included. Unity runtime components retain their own terms.

- Zip size: 47,341,444 bytes.
- Zip SHA-256: `8461c758236a226e0aa8735272ffcba5689aedcb220ac6f0783df0532d4e46dc`
- Archive CRC integrity check passed; extraction performed into a fresh directory.

## Extracted Player verification

| Bounded check | Result | Wall time | Captures |
| --- | --- | ---: | ---: |
| Knowledge / Book / Boss / skills | Exit 0, layout issues 0 | 17.3s | 13 |
| Pressure warnings / active / broken / combined / refuge | Exit 0, layout issues 0 | 18.5s | 12 |

Both logs contain no recorded exception or JobTempAlloc warning. Representative zh-TW Book and combat screens were visually inspected. This validates the packaged runtime in these short samples; it does not prove universal compatibility, natural tower completion or long-session stability. [Machine-readable package evidence](../Evidence/portfolio-package-validation.json).

**Not run:** latest full Balance Gate, 5000-seed simulation, 120-minute soak. Historical Gate FAIL remains recorded. The Release is a portfolio prerelease, not a production-ready certification.
