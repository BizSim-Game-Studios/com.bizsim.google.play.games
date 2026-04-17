// Copyright (c) BizSim Game Studios. All rights reserved.

using UnityEngine;

namespace BizSim.Google.Play.Games
{
    /// <summary>
    /// Project-wide settings asset for the Games Services package. Editable via
    /// <c>BizSim → Google Play → Games Services → Configuration</c> editor window per
    /// <c>CROSS-PACKAGE-INVARIANTS.md §12</c>. The asset lives at
    /// <c>Assets/Resources/BizSim/GooglePlay/GamesSettings.asset</c>.
    /// </summary>
    /// <remarks>
    /// Distinct from the operational <c>GamesServicesConfig</c> ScriptableObject (service
    /// toggles, mock behavior, JNI timeouts). <c>GamesSettings</c> holds only the four
    /// cross-package base fields (LogsEnabled, LogLevel, UseMockInDevelopmentBuild,
    /// EnableAnalyticsByDefault) required by every <c>com.bizsim.google.play.*</c> package.
    /// The two assets MAY be consolidated in a future 2.0.0 release.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "BizSim/Google Play/Games Settings",
        fileName = "GamesSettings",
        order = 0)]
    public sealed class GamesSettings : ScriptableObject
    {
        // Path constants per CROSS-INVARIANTS §12.5 — keep the two in sync.
        public const string ResourcesLoadKey  = "BizSim/GooglePlay/GamesSettings";
        public const string AssetDatabasePath = "Assets/Resources/" + ResourcesLoadKey + ".asset";

        [Header("Logging")]
        [Tooltip("Master switch — when false, BizSimGamesLogger is a no-op for this package regardless of LogLevel.")]
        public bool LogsEnabled = true;

        [Tooltip("Minimum severity printed when LogsEnabled is true.")]
        public LogLevel LogLevel = LogLevel.Info;

        [Header("Editor / Development")]
        [Tooltip("If true, builds with DEVELOPMENT_BUILD use MockProvider instead of the real JNI provider. Release builds always use the real provider regardless.")]
        public bool UseMockInDevelopmentBuild = false;

        [Header("Analytics")]
        [Tooltip("If true, the controller registers a default Firebase analytics adapter at Awake (when BIZSIM_FIREBASE is defined).")]
        public bool EnableAnalyticsByDefault = false;
    }
}
