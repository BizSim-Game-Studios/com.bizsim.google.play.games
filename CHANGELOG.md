# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
