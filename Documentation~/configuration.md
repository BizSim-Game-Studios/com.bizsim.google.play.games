# Configuration

## GamesServicesConfig Asset

The package uses a `GamesServicesConfig` ScriptableObject for project-wide defaults. The asset is located at:

```
Assets/Resources/BizSim/GooglePlay/GamesServicesConfig.asset
```

If the asset does not exist, it is auto-created the first time you open the Configuration window.

### Settings Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master switch for all log output |
| `LogLevel` | `LogLevel` | `Info` | Minimum log level: Verbose, Info, Warning, Error, Silent |
| `UseMockInDevelopmentBuild` | `bool` | `false` | Use mock provider in Development Builds |
| `EnableAnalyticsByDefault` | `bool` | `true` | Whether the analytics adapter fires events by default |
| `AutoSignIn` | `bool` | `true` | Attempt automatic sign-in on controller initialization |
| `RequestServerAuthCode` | `bool` | `false` | Request a server auth code during sign-in for backend verification |
| `RequestIdToken` | `bool` | `false` | Request an ID token during sign-in |
| `ForceRefreshToken` | `bool` | `false` | Force token refresh on each sign-in attempt |

## Configuration Editor Window

Open via **BizSim > Google Play > Games Services > Configuration**.

### Settings Panel

The window draws each field from the `GamesServicesConfig` asset using `SerializedObject` and `EditorGUILayout.PropertyField`. Three buttons are available:

- **Apply** -- saves changes to the asset and calls `BizSimGamesLogger.InvalidateCache()` so log-level changes take effect immediately
- **Revert** -- discards unsaved changes and reloads from disk
- **Reset to defaults** -- restores all fields to their default values

### Google Play Console Setup

The Configuration window also provides guidance for:
1. Setting up the OAuth 2.0 client ID in the Google Play Console
2. Configuring achievements and leaderboards
3. Enabling the Play Games Services API for your project

### Per-Instance Overrides

`GamesAuthController` has `[SerializeField]` fields that mirror the Settings asset. When you place the controller on a GameObject manually, per-instance values override the asset defaults.

## Sidekick Readiness

The package includes a **Sidekick Readiness** window (**BizSim > Google Play > Games Services > Sidekick Readiness**) that checks whether the project meets the requirements for Play Games Sidekick integration:

- Google Play Games plugin detected
- Auth controller configured
- Achievement and leaderboard IDs defined
- Server auth code enabled (if backend integration needed)

## Feature-Specific Configuration

### Achievements

No additional configuration required beyond the Settings asset. Achievement IDs are passed at call time via `SubmitAchievementAsync(achievementId)`.

### Leaderboards

No additional configuration required. Leaderboard IDs are passed at call time via `SubmitScoreAsync(leaderboardId, score)`.

### Cloud Save (Saved Games)

Cloud Save requires the "Saved Games" option to be enabled in the Google Play Console for your app. The package reads this setting at runtime and returns `FeatureNotSupported` if disabled.

### Events

Events require event IDs to be configured in the Google Play Console. The package submits event increments via `IncrementEventAsync(eventId, steps)`.

### Player Stats

No additional configuration required. Stats are read-only and fetched via `GetPlayerStatsAsync()`.
