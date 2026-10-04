# Public Repository Safety Audit

Date: 2026-10-04. Audited development revision: `43a2f70`.

**PUBLICATION STATUS: BLOCKED.** This is the first, explicitly requested
safety-audit stage. No repository has been created, pushed or made public.
The project has no Git remote. Repository URL and Release URL are not available.

## Scope and method

- All 59 reachable commits and all local refs, not only HEAD. Gitleaks 8.30.1
  scanned Git changes with `--log-opts=--all`, default rules, 100% redaction.
- Full-content supplemental inventory of all 791 reachable Git blobs (45,140,622 bytes), with
  paths and object sizes. Commit messages and local Git config were also
  scanned through Gitleaks stdin; author/committer metadata was reviewed.
- Seven local unreachable blobs (1,463 bytes combined) were separately
  scanned; no Gitleaks findings. They are not ordinary push targets.
- Working directory scanning includes ignored Build, Artifacts and Unity
  cache directories. The supplemental text pass read 3,364 local text files
  without read errors; directory symlinks are not followed. No sensitive-named
  .env/private-key/keystore file was found in the local inventory.
- Directory scan: 2,517 generic-api-key findings. All were reviewed against
  original content: 2,439 game localization identifiers, 78 Mono public
  strong-name key mappings. None is a confirmed credential.
- History scan: 97 generic-api-key findings, all localization identifiers
  (`key`, `nameKey`, `deathKey`, `intelKey`), not authentication keys.
- Gitleaks misclassified 78 `Compat.browser` text files as compressed data.
  Each was included in a supplementary stdin scan: zero findings. Commit
  metadata/local Git config supplementary scan also returned zero findings.
- No provider-specific token, SSH private key, cloud credential or confirmed
  secret was identified by those scans. This is a findings-based conclusion,
  not proof that every possible credential format is detectable.

The scanner executable was downloaded from the official Gitleaks release
and verified against that release's SHA-256 checksum. Raw reports, detailed
privacy findings and scanner files stay under ignored `Artifacts/PublicAudit/`.
Secret values, email addresses and usernames are not reproduced here.

## Publication blockers

| Finding | Risk | Action before public |
| --- | --- | --- |
| Local absolute project/tool paths in historical verification documents and manifests | Publishes local machine layout; links are not usable on GitHub | Sanitize the current public-facing documents and build manifests; decide whether existing historical path disclosure is acceptable. Removing HEAD tracking does not remove old blobs. Do not silently rewrite history. |
| Author/committer identity contains one non-GitHub-noreply email identity | Git history publicly exposes the identity; it is not an API secret | Confirm that the existing author name/email is intended for public attribution. Changing future Git config does not change earlier commits. |
| Windows runtime attribution/package review incomplete | Source assets and redistributable runtime binaries have different conditions | Review the final zip's actual dependencies and include font/runtime notices; test the extracted zip and record SHA-256 before releasing. |
| Portfolio README/media/release/GitHub settings gate has not been performed | Does not yet satisfy the requested portfolio release gate | Complete release preparation after resolving audit findings. |

The verified local-path inventory contains 82 historical blob versions and
40 currently tracked files. Examples at audited commit `43a2f70` include
`Artifacts/team-balance-build-manifest.json`, `Artifacts/agent-inspection-build-manifest.json`,
`Docs/PROJECT_MOVE.md`, and `Tools/build.ps1`. These are containing-commit
examples, not claims about the first introduction of each path. Detailed
file/line/commit records stay in local `Artifacts/PublicAudit/privacy-review.json`.

User-profile paths were found in ignored local logs, not in reachable tracked
blob history. They must remain local; do not upload those logs. The earlier
preliminary concern about historical user-profile paths was not confirmed.
Binary email-pattern matches in the font and PNG are not evidence of a visible
personal email; the separate Git author email is the actual identity decision.

There is no confirmed secret requiring revocation or a history rewrite at
this stage. If any real credential is later found, stop publication, revoke
or rotate it first, identify its file and commit, and only then plan a
security-motivated history cleanup. Do not upload raw local audit outputs.

## Third-party asset review

| Category | Repository evidence | Assessment |
| --- | --- | --- |
| Fonts | Noto Sans CJK TC 2.004 OTF; embedded Adobe copyright and OFL 1.1; adjacent OFL text | Redistributable under OFL conditions; retain notices in source and downloadable build. |
| Models / Blender output | RootcrownMask.fbx and `Tools/create_mask.py` | Generation script creates geometry locally; no downloaded model found. |
| Bosses, scenes, interactables, VFX | Presentation C# generates primitives, meshes, materials and particles | No vendored paid model/texture found in the reachable asset inventory. |
| Textures / icons | Game screenshots and Unity primitive/default textures | No external texture/icon pack found; do not claim Unity resources as author-owned assets. |
| Sounds / music | `EmberAudio.cs` / `WitnessGame.cs` generate AudioClip samples | No tracked external audio/music file found. |
| Shaders | Shader.Find uses Unity built-in shaders | No separately vendored shader source found; Unity runtime terms still apply. |
| Libraries / packages | Eight manifest modules, one additional locked imageconversion module; all built-in 1.0.0 | No third-party registry/Asset Store dependency found in manifest/lock. Do not publish installed Unity caches. |
| Windows runtime DLLs | Local ignored Builds contain Unity/Mono/graphics runtime files | Actual release-file notices review pending. No blanket redistribution clearance given. |

See [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md) for source links,
font hash and preservation requirements. No forbidden paid asset was found
in the repository inventory; the final packaged binary audit remains separate.

## Size and tracking review

Only one historical blob exceeds 10 MiB:

| Path | Bytes | MiB | Tracked at audit HEAD | In history | >25 / >50 / >100 MiB |
| --- | ---: | ---: | --- | --- | --- |
| UnityProject/Assets/Resources/Fonts/NotoSansCJKtc-Regular.otf | 16,435,884 | 15.67 | Yes | Yes | No / No / No |

No historical EXE, PDB, zip or video blob exceeds these thresholds. Local
Builds occupy approximately 3.57 GB and Artifacts approximately 3.11 GB
(decimal bytes, measured before audit outputs). These belong on local disk,
not in a source push. Several small JSONL/CSV evidence files are currently
tracked; their size is acceptable, but public selection/privacy review is
still needed. Windows zip belongs in GitHub Releases.

## Changes made in this stage

- Added `COPYRIGHT.md`: the requested All Rights Reserved wording, with
  third-party exceptions. No open-source license added.
- Added `THIRD_PARTY_NOTICES.md` and this audit report.
- Added `Tools/audit-public-repository.py` for repeatable object-size,
  privacy and local-file inventory.
- Expanded `.gitignore` for local audit output, MemoryCaptures, PDB/dump/trace
  files, environment files and common private-key/keystore extensions.
- No tracked evidence was deleted or untracked. No local Build, telemetry,
  log or capture was deleted. No Git history was rewritten.
- No collaborator invitation, external PR merge, permission change or remote
  publication occurred. Main protection and GitHub feature settings are
  **not configured**, because no target repository is connected.

## Reproduction and limits

Run `python Tools/audit-public-repository.py` from the repository root.
It writes only ignored local reports. Large text files use 1 MiB windows
with 4 KiB overlap instead of loading raw telemetry into RAM. Extremely
long patterns crossing more than that overlap, encrypted data, and unknown
credential formats remain scanner limitations. Source inventory is not a
legal opinion or a certification of asset ownership or Unity tier eligibility.

Do not convert the publication gate to READY solely because secret scan
hits are false positives. Resolve privacy decisions, binary notices and all
portfolio/release/settings gates first. The latest game Balance Gate,
5000-seed rerun and 120-minute soak remain pending as documented in
[Team Balance validation](TEAM_BALANCE_VALIDATION.md).
