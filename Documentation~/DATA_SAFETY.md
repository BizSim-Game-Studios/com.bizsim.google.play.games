# Data Safety

## Play Store Data Safety Form Guidance

This document describes what data flows through `com.bizsim.google.play.games` to help consumers fill out the Google Play Store Data Safety form.

## Data Collected

| Data Type | Collected | Persisted | Transmitted | Purpose |
|-----------|-----------|-----------|-------------|---------|
| Player ID | Yes | No (in-memory only) | Yes (to Google servers via PGS SDK) | Authentication |
| Display name | Yes | No (in-memory only) | Yes (to Google servers via PGS SDK) | Player profile |
| Player avatar URL | Yes | No (in-memory only) | Yes (to Google servers via PGS SDK) | Player profile |
| Server auth code | Yes (if requested) | No (in-memory only) | Yes (to your backend, if configured) | Backend verification |
| ID token | Yes (if requested) | No (in-memory only) | Yes (to your backend, if configured) | Backend verification |
| Achievement progress | Yes | No (in-memory only) | Yes (to Google servers via PGS SDK) | Game progress |
| Leaderboard scores | Yes | No (in-memory only) | Yes (to Google servers via PGS SDK) | Game progress |
| Saved game data | Yes (if Cloud Save used) | Yes (Google servers) | Yes (to Google servers via PGS SDK) | Game progress |
| Event counts | Yes (if Events used) | No (in-memory only) | Yes (to Google servers via PGS SDK) | Analytics |
| Player stats | Yes (if Stats used) | No (in-memory only) | Yes (from Google servers via PGS SDK) | Game balancing |

## How Data Flows

1. **Authentication:** The package calls Play Games Services v2 APIs via JNI. Player identity data (ID, display name, avatar) is held in memory only and not persisted locally by this package.

2. **Game progress:** Achievement unlocks, leaderboard score submissions, and saved game data are transmitted to Google's servers by the Play Games Services SDK. This package acts as a bridge -- it does not store game progress locally.

3. **Analytics:** Event submissions and player stats are exchanged with Google servers. This package does not persist these locally.

4. **Optional tokens:** Server auth codes and ID tokens are collected only when explicitly requested via configuration. They are held in memory and intended for transmission to your own backend server. This package does not transmit them automatically.

## What This Package Does NOT Do

- Does not persist player identity data locally
- Does not transmit data to BizSim servers or any third party other than Google
- Does not collect device identifiers beyond what the Play Games Services SDK handles internally
- Does not access contacts, photos, location, or other sensitive permissions

## Play Store Form Entries

Based on the data above, consumers should declare:

- **Personal info > Name:** Collected (display name from Google account, via PGS)
- **Personal info > User IDs:** Collected (Player ID)
- **App activity > In-app search history:** Not collected
- **App activity > Other user-generated content:** Collected if using Cloud Save
- **App info and performance:** Not collected by this package
- **Device or other IDs:** Not directly collected (PGS SDK may collect internally)
- **Is data encrypted in transit:** Yes (PGS SDK uses HTTPS)
- **Can users request data deletion:** Via Google account settings (not controlled by this package)
