# Mock Config Presets

Pre-configured `GamesServicesConfig` assets for common testing scenarios.

## Usage

1. Import this sample via **Window → Package Manager → BizSim Google Play Games Services → Samples → Mock Config Presets → Import**.
2. Run **BizSim → Google Play → Games → Create Mock Presets** from the Unity menu.
3. Find the generated assets in `Assets/Samples/GamesServicesPresets/`.
4. Drag a preset onto `GamesServicesManager` or set it as the active config.

## Presets

| Preset | Auth | Behavior |
|--------|------|----------|
| `Preset_Authenticated` | Succeeds | Premium player, 8 achievements unlocked, high score 150k, low churn |
| `Preset_Guest` | Fails (UserCancelled) | Simulates user declining sign-in |
| `Preset_NetworkError` | Fails (NoConnection) | 2s delay, all API calls return errors |
| `Preset_CloudConflict` | Succeeds | Cloud Save operations trigger conflict resolution flow |
