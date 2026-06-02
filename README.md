# BizSim Google Play Games Services Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.3.1-orange.svg)](CHANGELOG.md)

Unity bridge for [Google Play Games Services v2](https://developer.android.com/games/pgs/unity/unity-start) (play-services-games-v2:21.0.0).
Modular async Java-to-C# wrapper for authentication, achievements, leaderboards, saved games, events, and player stats, with an editor mock provider.

> **⚠️ Unofficial package.** This is a community-built Unity bridge for Google Play Games Services v2. It is **not** an official Google product.

## Features

- **Authentication** — Silent + manual sign-in with PGS v2, server-side access tokens
- **Achievements** — Unlock, increment, reveal, batch operations, local caching
- **Leaderboards** — Submit scores, load top/player-centered rankings, scoretags
- **Cloud Save** — Transaction-based saved games with metadata, cover image, conflict resolution
- **Events** — Batched event tracking with 5-second flush interval, pause/quit flush
- **Player Stats** — Churn prediction, spend probability, engagement metrics
- **Mock provider** — ScriptableObject presets covering authenticated, guest, and error scenarios for editor + non-Android builds
- **Analytics adapter** — Optional `IGamesAnalyticsAdapter` with a Firebase implementation guarded by `BIZSIM_FIREBASE`
- **UniTask support** — Optional extension assembly guarded by `BIZSIM_UNITASK`
- **Editor integration** — Auto-registered `BIZSIM_GAMES_INSTALLED` define via `editor.core`

## Installation

This package depends on Google's [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver), which is published to the OpenUPM scoped registry. Add EDM4U's registry to your project's `Packages/manifest.json` once, then add this package as a Git URL — UPM will auto-install EDM4U on first import.

**Step 1 — Add the OpenUPM scoped registry (one-time per project):**

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

If you already have other OpenUPM-distributed packages, you may already have this registry — just add `com.google.external-dependency-manager` to the existing `scopes` array.

**Step 2 — Install this package via Git URL:**

```json
{
  "dependencies": {
    "com.bizsim.google.play.games": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games.git#v1.3.1"
  }
}
```

After the package imports, EDM4U is automatically resolved by UPM — no manual `.unitypackage` import required. EDM4U then resolves the Android Maven dependencies declared in `Editor/Dependencies.xml` (`com.google.android.gms:play-services-games-v2:21.0.0` and `com.google.android.gms:play-services-tasks:18.4.1`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

## Quick Start

1. Get your resources XML from [Google Play Console](https://play.google.com/console) and run the setup window: **BizSim → Google Play → Games Services → Setup**. Paste the XML and click "Setup".

2. Initialize and authenticate:
   ```csharp
   using BizSim.Google.Play.Games;

   void Start()
   {
       GamesServicesManager.Initialize();
   }

   async void AuthenticateUser()
   {
       var player = await GamesServicesManager.Auth.AuthenticateAsync();
       Debug.Log($"Welcome {player.DisplayName}!");
   }
   ```

3. Unlock an achievement and submit a leaderboard score:
   ```csharp
   await GamesServicesManager.Achievements.UnlockAchievementAsync("achievement_first_win");
   await GamesServicesManager.Leaderboards.SubmitScoreAsync("leaderboard_high_score", 12345);
   ```

4. Save game with required metadata (see Google Play compliance note below):
   ```csharp
   var handle = await GamesServicesManager.CloudSave.OpenSnapshotAsync("slot1", true);
   await GamesServicesManager.CloudSave.CommitSnapshotAsync(
       handle,
       data:              SerializeGameState(),
       description:       "Level 5, 1500 coins",   // required by Quality Checklist 6.1
       playedTimeMillis:  GetPlayTime(),            // required
       coverImage:        CaptureScreenshot()       // required
   );
   ```

## Google Play Compliance Requirements

**Before publishing, you MUST comply with Google Play Games Services policies.** Failure to comply may result in app rejection or removal from Google Play Store.

| Policy | Key Requirements |
|--------|-----------------|
| [Quality Checklist](https://developer.android.com/games/pgs/quality) | Manual sign-in button if auto-auth fails; 10+ achievements (4+ achievable in 1 hour); achievement icons 512×512 PNG; saved games MUST include cover image, description, and timestamp |
| [Branding Guidelines](https://developer.android.com/games/pgs/branding) | Use Google Play game controller icon for all UI entry points; NEVER suppress welcome-back or achievement-unlock pop-ups; NEVER include "Google Play Games" in your game title |
| [Data Collection](https://developer.android.com/games/pgs/data-collection) | Friends data: 30-day maximum retention; friends data ONLY for friends-list UI (not analytics or advertising); complete the Google Play Data Safety form accurately |
| [Terms of Service](https://developer.android.com/games/pgs/terms) | Never submit false gameplay data; never send multiplayer invites without user approval; never use player data for advertising; never share friends data with third parties |

## Requirements

- Unity 6000.0 or later
- Android target platform
- **[EDM4U](https://github.com/googlesamples/unity-jar-resolver) (External Dependency Manager for Unity)** — auto-resolved via OpenUPM scoped registry (see Installation)
- Google Play Games Services v2 21.0.0 (resolved automatically via `Editor/Dependencies.xml`)

## Google Play Data Safety

### Data Collected

This package does **not** collect user data on its own. Google Play Games Services processes player identity and gameplay data (scores, achievements, saved games) via Google's own infrastructure. Any data submitted through PGS APIs is sent directly to Google's servers per [Google's privacy policy](https://policies.google.com/privacy).

Friends list data fetched through the API must be retained for a maximum of 30 days and used only for friends-list UI features, per Google's Data Collection policy.

### Data NOT Collected or Shared

- **No personal data** is collected by this package's own code
- **No data is shared** with third parties by this package
- **No network calls** are made by this package directly (PGS SDK handles all communication)

### Play Console Data Safety Form

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469) in Google Play Console:

1. **Data types**: Reflect data submitted via PGS APIs (scores, display name, etc.) — this is first-party data to Google Play, not "shared with third parties"
2. **Collection purpose**: Game features / functionality
3. **Friends data**: If used, declare 30-day retention and in-game-only purpose

## License

This package's C# and Java source code is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

## Third-Party Licenses

This package does **not** bundle any Google SDK binaries. The native Android dependencies are resolved at build time by [EDM4U](https://github.com/googlesamples/unity-jar-resolver) from the Google Maven repository (`maven.google.com`):

| Dependency | Version | License |
|-----------|---------|---------|
| `com.google.android.gms:play-services-games-v2` | 21.0.0 | [Android SDK License Agreement](https://developer.android.com/studio/terms) |
| `com.google.android.gms:play-services-tasks` | 18.4.1 | [Android SDK License Agreement](https://developer.android.com/studio/terms) |

For full third-party license details, see [NOTICES.md](NOTICES.md).
