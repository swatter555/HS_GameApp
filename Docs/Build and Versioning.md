# Windows builds and versioning

The first testing version is **0.1.0-alpha.1**, Standalone build number **1**. It identifies an incomplete testing milestone, not a release candidate or a promise that saves, AI, logistics and every planned feature are finished.

## Version authorities

- `PlayerSettings.bundleVersion` in `ProjectSettings/ProjectSettings.asset` is the single product-version authority. Runtime code reads `Application.version`; session exception logs include it. Do not introduce a second hardcoded game-version string in UI or code.
- `buildNumber.Standalone` in the same file is a monotonically increasing artifact counter. Unity exposes this shared Standalone field through `PlayerSettings.macOS.buildNumber`; it is not a Windows file-format or save version.
- `ProjectSettings/ProjectVersion.txt` pins the exact Unity Editor and revision. `Packages/manifest.json` and `packages-lock.json` pin dependencies. Installations on a developer's machine do not change the project's approved version.
- `GameData.SAVE_VERSION`, the supported save floor, map version and other content contracts are independent. A product/Editor version change must not silently bump, migrate or retire any of those formats.

## Advancing the game version

Use `MAJOR.MINOR.PATCH-stage.N`, following the ordering `alpha` → `beta` → `rc` → an unqualified release. The first planned public release is `1.0.0`; only Robert's release acceptance authorizes that designation.

| Change | Example |
|---|---|
| Next distinct testing delivery of this milestone | `0.1.0-alpha.1` → `0.1.0-alpha.2` |
| New pre-release feature milestone | `0.1.0-alpha.N` → `0.2.0-alpha.1` |
| Broad feature-complete testing, when actually achieved | `1.0.0-beta.1` |
| Candidate for release, when acceptance gates are met | `1.0.0-rc.1` |
| Accepted first release | `1.0.0` |
| Released defect fix / feature release | `1.0.1` / `1.1.0` |

Increment the Standalone artifact counter for each new build retained or handed to testers. Do not reuse an identifier for changed binaries. Failed local build attempts need not advance the public version; record them honestly and do not label them delivered. Ordinary source commits do not automatically change the product version. Fixes within a pre-release milestone can advance its stage number; after release, use PATCH for compatible fixes and MINOR for compatible additions. Reserve MAJOR for a deliberately new major product/compatibility boundary. Save/content compatibility still requires its own explicit policy regardless of these numbers.

Name an output, for example, `HS_Game_0.1.0-alpha.1_b0001_win64`. Keep build outputs outside tracked source. Record product version, artifact counter, commit, whether any local input was uncommitted, exact Editor/URP versions, target/backend, date, tests, known limitations and artifact checksum together. Tag the exact source commit only when a corresponding accepted artifact exists; do not pre-tag an unbuilt version.

## Desktop baseline

- Windows x86-64 Player; MainMenu then BattleScene; PC quality selected.
- PC quality explicitly references `Assets/Settings/Forward Renderer.asset`; Mobile intentionally inherits the global renderer and remains excluded from Standalone.
- VSync every display refresh (`vSyncCount=1`). There is no new runtime frame-limit override: Unity's desktop VSync owns pacing. A future VSync-off option must supply an intentional frame limit rather than silently enabling uncapped rendering.
- Native-resolution borderless fullscreen; 1600×900 windowed fallback; resizable window; fullscreen switching enabled. The player can retain prior Unity display preferences, so a clean-user check is required to verify first-launch defaults.
- Pause when unfocused, remain visible behind other windows. Verify Alt-Tab during animation/audio/loading.
- Preserve full source texture resolution, render scale 1, the current anti-aliasing setup and the protected layer-7 renderer feature. Settings defaults do not constitute a new in-game graphics/options menu.
- Keep Mono and the .NET Standard profile until a separately verified backend change is justified. Keep Pipeline disabled in builds and development debugging/profiler flags off for representative testing artifacts.

## Editor ownership and upgrade process

Robert delegated HS Game Unity-version management to the Lead Agent on 2026-09-27, including installing, selecting and verifying an appropriate supported Editor. This does not authorize changes to other projects, automatic upgrades on every release, or unrelated experimental Editor authoring.

1. Choose a supported stable LTS patch after checking its release notes, known issues, URP/package compatibility and this project's rendering/input/serialization dependencies. Never use `latest` as the committed project pin or adopt alpha/beta Editors for the testing baseline.
2. Retain the old Editor. Preserve existing source, asset GUIDs and unrelated local work; make a focused checkpoint and a recoverable snapshot before migration. Trial the migration in an isolated checkout where practical.
3. Import with the candidate Editor, review automatic source/asset/package changes, resolve compile errors, and run the full EditorTests suite. Preserve JsonPolicy, serialized identifiers and layer-7 rendering.
4. Build Windows and exercise MainMenu/Khost, representative national units, terrain, fonts, camera zoom, all facings, helicopter frames, audio, window resizing/fullscreen switching and shutdown. Inspect logs and content inclusion; investigate new errors or visual regressions.
5. Adopt the candidate by committing the exact Editor pin and necessary dependency/migration changes with their verification record. If the trial fails, keep the previous approved baseline and record the blocker. Do not claim installing the Editor completed the project migration.
6. Coordinate the exact game pin and any shared-contract consequences with HS Editor/HS_AI through the established correspondence workflow. Their Editor versions are not silently changed by an HS Game decision.

### Windows installation paths

Use a short Editor installation root such as `C:/Unity/Editors/<exact-version>`. On September 27, 2026, installing `6000.6.3f1` below the longer Documents/Game Development path omitted bundled files whose full names exceeded the legacy Windows path limit, despite Windows long-path support being enabled. The missing rendering-bridge assembly definition and material helpers caused package compiler errors; the project cache inherited the incomplete files.

If this recurs, compare the affected cache with the exact Editor's bundled package, preserve the existing project, and reinstall the same signed release under the short root. Verify its release checksum/signature, original source and metadata, then regenerate or restore the affected caches from the complete matching package. Do not patch vendor code, invent replacement GUIDs, or treat the CLI's structural Editor check as a full package-file audit. Register the repaired installation and keep the preceding working Editor available. Recompile and rerun tests before claiming recovery.

The Editor-only button-wiring audit compares `(target object, method name)` directly when detecting duplicate listeners. It does not use Unity's retired `GetInstanceID()` API or persist Editor object identities.

Atlas verification must also respect Unity object ownership: `SpriteAtlas.GetSprite` can return a persistent imported asset in the newer Editor. Test cleanup destroys only nonpersistent temporary sprites; it must never enable asset destruction to silence a cleanup error.

## Packaging gates

Read the active vault TODO before any external distribution. Existing open gates include the enemy-reveal cheat, legacy asset/identifier cleanup, provisional French frames, the StreamingAssets OOB backup, generated terrain arrays and unfinished player-save guarantees. Unity test success alone does not close those gates.

Generate ignored terrain arrays with the repository-map setup steps and verify them in the player. Test minimum hardware and display configurations in the actual build. Store build/upgrade records in the vault's `HS Game/Build Records` folder; keep exact build contracts in this versioned document.
