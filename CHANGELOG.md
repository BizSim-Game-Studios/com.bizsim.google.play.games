# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2026-04-17

### Fixed
- **`PackageVersion.cs` finally contains the K8 canonical const fields declared in the 1.2.0 CHANGELOG.** v1.2.0 commit skipped the `PackageVersion.cs` edit due to a tool-call race (file-not-read-yet aborted the write, retry was not performed). The 1.2.0 tag has correct `package.json.version` and `CHANGELOG` entry but `PackageVersion.Current` stayed at `"1.1.0"` and the three new `NativeSdk*` consts were missing. This PATCH ships the actual const additions. **Consumers on 1.2.0 should upgrade to 1.2.1.**

## [1.2.0] - 2026-04-17

### Added
- **K8 PackageVersion schema unification (Plan G).** Three new `public const string` fields on `PackageVersion`: `NativeSdkVersion` (`"21.0.0"`), `NativeSdkLabel` (`"Play Games Services v2"`), `NativeSdkArtifactCoord` (`"com.google.android.gms:play-services-games-v2:21.0.0"`). The new `NativeSdkLabel` disambiguates Games (GMS family, `com.google.android.gms:*`) from sibling Play Core packages (review/appupdate/assetdelivery, `com.google.android.play:*`) — fixes the dashboard showing a misleading "Play Core: 21.0.0" label for this package. See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/06-package-version-schema.md`.
- `PackageVersionSchemaTest` drift guard.

### Deprecated
- `PackageVersion.PgsV2SdkVersion` — now `[Obsolete]` alias of `NativeSdkVersion`. Removed in 2.0.0 per ADR-009.

## [1.1.0] - 2026-04-17

### Added
- Real sample scripts for all features under `Samples~/` (Auth, Achievements, Leaderboards, Cloud Save, Events, Player Stats). Each sample is a runnable `BasicIntegration` scene demonstrating the typical flow without requiring a configured Play Console.
- `Tests/Runtime/BizSim.Google.Play.Games.Tests` test assembly (previously missing) plus `BizSimLoggerPrefixTest` drift guard asserting `BizSimGamesLogger.Prefix` matches the workspace-mandated `"[BizSim.Games] "` literal per CROSS-PACKAGE-INVARIANTS §12.3.

### Fixed
- `BizSimGamesLogger.Prefix` corrected from `"[PlayGames]"` to `"[BizSim.Games] "` (with trailing space) per CROSS-PACKAGE-INVARIANTS §12.3 / google-play-bridge-pattern.md §6.1. K1.1 compliance restored. Visibility adjusted `private` → `internal` so the new test assembly can read it via `InternalsVisibleTo`. Four call sites (`Verbose/Info/Warning/Error` methods) had explicit post-prefix spaces that compensated for the previous no-trailing-space literal; those explicit spaces are removed so output format remains `[BizSim.Games] <message>` (single space) rather than `[BizSim.Games]  <message>` (double space).

## [1.0.2] - 2026-04-16

### Added
- `GamesConfiguration` EditorWindow under BizSim > Google Play > Games > Configuration with Settings, Firebase, and Links tabs
- `GamesEditorInit` registers `BIZSIM_GAMES_INSTALLED` scripting define via `BizSimDefineManager` on domain reload

### Fixed
- Add missing `.meta` files for `GamesConfiguration.cs` and `GamesEditorInit.cs`

## [1.0.1] - 2026-04-15

### Fixed
- Relaxed runtime asmdef `includePlatforms` from `["Android", "Editor"]` to `[]`
  to fix a consumer-side `CS0246: The type or namespace name 'BizSim' could not
  be found` regression that appeared during Addressables content build on Android
  target. The Editor compile pass resolved the auto-reference correctly, but the
  Player script compile pass did not — a known Unity issue when `autoReferenced`
  library assemblies are platform-gated at the asmdef level.

  Runtime platform safety is preserved by the existing `#if UNITY_ANDROID && !UNITY_EDITOR`
  guards around every JNI call site; non-Android builds continue to route through
  `Mock<Api>Provider` per CROSS-PACKAGE-INVARIANTS §4.

  No API surface change. Consumers with existing `using BizSim.Google.Play.Games;`
  imports require no action — the fix is transparent on the next package install.

## [1.0.0] - 2026-04-14

### Added

- Initial release of `com.bizsim.google.play.games` — Unity bridge for Google Play Games Services v2.
- Modular async API covering Authentication, Achievements, Leaderboards, Saved Games (Cloud Save), Events, and Player Stats.
- `ISocialPlatform`-independent — does not depend on Unity's legacy social API surface.
- Java JNI bridge with `.androidlib` subproject and ProGuard keep rules.
- Mock provider for editor testing without a device, with pre-configured scenarios (authenticated, guest, error).
- Optional Firebase Analytics integration via `BIZSIM_FIREBASE` versionDefine.
- `Samples~/BasicIntegration` — authentication, achievements, and leaderboard submission in a single scene.
- `Samples~/MockPresets` — pre-configured mock scenarios.
- Tests under `Tests/Editor/` and `Tests/Runtime/`.

### Notes

- This is the first release under the new `com.bizsim.google.play.*` family naming. The previous incarnation (`com.bizsim.gplay.games`) at version 1.15.9 is archived and no longer maintained. Consumers must manually switch to the new package id — there is no automated migration.
- Floor: Unity 6.0 LTS (`6000.0`).
