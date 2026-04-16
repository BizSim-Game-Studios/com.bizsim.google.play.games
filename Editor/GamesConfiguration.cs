// Copyright (c) BizSim Game Studios. All rights reserved.

using BizSim.Google.Play.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.Games.Editor
{
    /// <summary>
    /// Configuration window for the BizSim Google Play Games Services package.
    /// Accessible via: BizSim > Google Play > Games > Configuration
    /// </summary>
    public class GamesConfiguration : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab;
        private static readonly string[] TabNames = { "Settings", "Firebase", "Links" };

        [MenuItem("BizSim/Google Play/Games/Configuration", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<GamesConfiguration>("Games Configuration");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, TabNames);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawSettingsTab(); break;
                case 1: DrawFirebaseTab(); break;
                case 2: DrawLinksTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Games Services Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Package Version:", PackageVersion.Current);
            EditorGUILayout.LabelField("Play Games SDK:", PackageVersion.PgsV2SdkVersion);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "For detailed Games Services setup, use:\nBizSim > Google Play > Games Services > Setup > Android Setup",
                MessageType.Info);

            if (GUILayout.Button("Open Setup Wizard", GUILayout.Height(28)))
                GamesServicesSetupWindow.ShowWindow();
        }

        private void DrawFirebaseTab()
        {
            EditorGUILayout.LabelField("Firebase Integration", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            bool firebaseInstalled = PackageDetector.IsFirebaseAnalyticsInstalled();
            bool definePresent = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();

            EditorGUILayout.LabelField("Firebase SDK:", firebaseInstalled ? "Installed" : "Not Found");
            EditorGUILayout.LabelField("BIZSIM_FIREBASE:", definePresent ? "Active" : "Not Set");
        }

        private void DrawLinksTab()
        {
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("GitHub Repository"))
                Application.OpenURL("https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games");
            if (GUILayout.Button("Play Games Services Documentation"))
                Application.OpenURL("https://developer.android.com/games/pgs");
            if (GUILayout.Button("Report Issue"))
                Application.OpenURL("https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games/issues");
        }
    }
}
