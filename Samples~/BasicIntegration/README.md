# Basic Integration Sample

Demonstrates the full Google Play Games Services v2 integration:
authentication, achievements, leaderboards, cloud save, and player stats.

## Setup

1. Import this sample via **Window → Package Manager → BizSim Google Play Games Services → Samples → Basic Integration → Import**.
2. Open the `BasicIntegration` scene (or attach the scripts to your own scene).
3. Create a `GamesServicesConfig` asset via **BizSim → Google Play Games → Services Config**.
4. Build and run on an Android device with Google Play Games installed.

## Scripts

| Script | Purpose |
|--------|---------|
| `BasicIntegrationBootstrap.cs` | End-to-end demo: sign-in → achievements → leaderboard → stats |
| `AchievementsSample.cs` | Unlock, increment, reveal achievements, show native UI |
| `LeaderboardSample.cs` | Submit scores, load top/friends scores, show native UI |
| `CloudSaveSample.cs` | Save/load progress, conflict resolution, saved games UI |

## Testing in Editor

The package automatically uses mock providers in the Editor. Configure mock
behavior via `GamesServicesConfig.editorMock` settings — control auth
success/failure, mock player data, simulated conflicts, etc.
