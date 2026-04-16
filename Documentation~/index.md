# BizSim Google Play Games Services

Last reviewed: 2026-04-16

## Overview

BizSim Google Play Games Services is a Unity package wrapping Google Play Games Services v2 (play-services-games-v2:21.0.0). It provides a modular async C# API for six feature areas: Authentication, Achievements, Leaderboards, Cloud Save (Saved Games), Events, and Player Stats.

The package is ISocialPlatform-independent -- it does not use Unity's built-in social interface. Each feature area has its own provider interface, enabling per-feature mock testing in the Unity Editor without a device.

## Table of Contents

| File | Description |
|------|-------------|
| [getting-started.md](getting-started.md) | Installation, Google Play Console setup, first authentication |
| [api-reference.md](api-reference.md) | Complete API for all 6 services with signatures, parameters, and events |
| [configuration.md](configuration.md) | GamesServicesConfig asset and Editor window walkthrough |
| [architecture.md](architecture.md) | JNI bridge, mock providers, threading, error handling |
| [troubleshooting.md](troubleshooting.md) | Common issues, error codes, platform-specific fixes |
| [DATA_SAFETY.md](DATA_SAFETY.md) | Play Store Data Safety form guidance |

### Additional Guides

| File | Description |
|------|-------------|
| [SIDEKICK-GUIDE.md](SIDEKICK-GUIDE.md) | Sidekick Tier 1 and 2 integration, metadata, validator |
| [QUALITY-CHECKLIST.md](QUALITY-CHECKLIST.md) | Google Play Games quality requirements checklist |
| [LEVEL-UP-PROGRAM.md](LEVEL-UP-PROGRAM.md) | Level Up program tiers, benefits, and migration guide |

## Service Overview

| Service | Static Accessor | Interface |
|---------|----------------|-----------|
| Auth | `GamesServicesManager.Auth` | `IGamesAuthProvider` |
| Achievements | `GamesServicesManager.Achievements` | `IGamesAchievementProvider` |
| Leaderboards | `GamesServicesManager.Leaderboards` | `IGamesLeaderboardProvider` |
| Cloud Save | `GamesServicesManager.CloudSave` | `IGamesCloudSaveProvider` |
| Events | `GamesServicesManager.Events` | `IGamesEventsProvider` |
| Player Stats | `GamesServicesManager.Stats` | `IGamesStatsProvider` |

## Links

- [README](../README.md) -- quick-start experience
- [CHANGELOG](../CHANGELOG.md) -- version history
- [GitHub Repository](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games)
