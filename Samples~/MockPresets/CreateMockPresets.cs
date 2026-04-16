#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.Games.Samples
{
    /// <summary>
    /// Editor menu action that creates pre-configured GamesServicesConfig
    /// assets for common testing scenarios.
    /// </summary>
    public static class CreateMockPresets
    {
        const string OutputFolder = "Assets/Samples/GamesServicesPresets";

        [MenuItem("BizSim/Google Play/Games/Create Mock Presets")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                string parent = "Assets/Samples";
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets", "Samples");
                AssetDatabase.CreateFolder(parent, "GamesServicesPresets");
            }

            CreateAuthenticatedPreset();
            CreateGuestPreset();
            CreateErrorPreset();
            CreateCloudConflictPreset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Games Sample] Created 4 mock presets in {OutputFolder}");
        }

        static void CreateAuthenticatedPreset()
        {
            var config = ScriptableObject.CreateInstance<GamesServicesConfig>();
            config.name = "Preset_Authenticated";

            var mock = config.editorMock;
            mock.authSucceeds = true;
            mock.mockPlayerId = "player_premium_001";
            mock.mockDisplayName = "ProGamer42";
            mock.authDelaySeconds = 0.3f;
            mock.mockUnlockedCount = 8;
            mock.mockScore = 150000;
            mock.mockChurnProbability = 0.05f;
            config.editorMock = mock;

            AssetDatabase.CreateAsset(config, $"{OutputFolder}/Preset_Authenticated.asset");
        }

        static void CreateGuestPreset()
        {
            var config = ScriptableObject.CreateInstance<GamesServicesConfig>();
            config.name = "Preset_Guest";

            var mock = config.editorMock;
            mock.authSucceeds = false;
            mock.mockAuthErrorType = AuthErrorType.UserCancelled;
            mock.authDelaySeconds = 0.2f;
            config.editorMock = mock;

            AssetDatabase.CreateAsset(config, $"{OutputFolder}/Preset_Guest.asset");
        }

        static void CreateErrorPreset()
        {
            var config = ScriptableObject.CreateInstance<GamesServicesConfig>();
            config.name = "Preset_NetworkError";

            var mock = config.editorMock;
            mock.authSucceeds = false;
            mock.mockAuthErrorType = AuthErrorType.NoConnection;
            mock.authDelaySeconds = 2.0f;
            mock.mockSimulateErrors = true;
            config.editorMock = mock;

            AssetDatabase.CreateAsset(config, $"{OutputFolder}/Preset_NetworkError.asset");
        }

        static void CreateCloudConflictPreset()
        {
            var config = ScriptableObject.CreateInstance<GamesServicesConfig>();
            config.name = "Preset_CloudConflict";

            var mock = config.editorMock;
            mock.authSucceeds = true;
            mock.mockPlayerId = "player_conflict_test";
            mock.mockDisplayName = "ConflictTester";
            mock.mockSimulateConflict = true;
            mock.mockSimulateErrors = false;
            config.editorMock = mock;

            config.enableCloudSave = true;

            AssetDatabase.CreateAsset(config, $"{OutputFolder}/Preset_CloudConflict.asset");
        }
    }
}
#endif
