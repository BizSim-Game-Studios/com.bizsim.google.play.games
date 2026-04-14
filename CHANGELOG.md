# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
