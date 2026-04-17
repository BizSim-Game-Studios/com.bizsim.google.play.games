// Copyright (c) BizSim Game Studios. All rights reserved.
// https://www.bizsim.com

using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BizSim.Google.Play.Games
{
    public enum LogLevel
    {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Silent = 4
    }

    /// <summary>
    /// Per-package logger. Reads <c>LogsEnabled</c> + <c>LogLevel</c> from a
    /// <see cref="GamesSettings"/> asset at first call and caches the result. Falls back
    /// to compile-time defaults + a one-shot warning if the asset is missing.
    /// </summary>
    /// <remarks>
    /// Master switch (<c>LogsEnabled</c>) is checked FIRST — it overrides <c>LogLevel</c>.
    /// When <c>LogsEnabled == false</c>, every call is a no-op regardless of severity.
    /// The legacy <see cref="ForceDebug"/> field is preserved for back-compat: when set
    /// by <c>GamesServicesManager</c> from the operational config's <c>debugMode</c>, it
    /// lowers the effective threshold to <c>Verbose</c> but does NOT bypass the master
    /// switch.
    /// </remarks>
    public static class BizSimGamesLogger
    {
        // Per-package PREFIX — never changes, never read from asset.
        // Required convention: "[BizSim.Games] " with trailing space. Enforced by BizSimLoggerPrefixTest.
        public const string Prefix = "[BizSim.Games] ";

        /// <summary>
        /// Legacy debug override. When <c>true</c>, lowers the effective <c>LogLevel</c>
        /// threshold to <c>Verbose</c> (all severities pass the level gate). Does NOT
        /// bypass the <c>LogsEnabled</c> master switch. Set by <c>GamesServicesManager</c>
        /// from <c>GamesServicesConfig.debugMode</c>.
        /// </summary>
        public static bool ForceDebug { get; set; }

        private static GamesSettings _cachedSettings;
        private static bool _loggedFallbackWarning;

        private static GamesSettings Settings
        {
            get
            {
                if (_cachedSettings != null) return _cachedSettings;
                _cachedSettings = Resources.Load<GamesSettings>(GamesSettings.ResourcesLoadKey);
                if (_cachedSettings != null) return _cachedSettings;

                if (!_loggedFallbackWarning)
                {
                    Debug.LogWarning(Prefix +
                        "Settings asset not found at Resources/BizSim/GooglePlay/GamesSettings.asset — " +
                        "falling back to compile-time defaults. Create it via BizSim → Google Play → Games Services → " +
                        "Configuration, or right-click in the Project window → Create → BizSim → Google Play → Games Settings.");
                    _loggedFallbackWarning = true;
                }
                _cachedSettings = ScriptableObject.CreateInstance<GamesSettings>();
                return _cachedSettings;
            }
        }

        public static void Verbose(string msg) { if (Should(LogLevel.Verbose)) Debug.Log(Prefix + "[V] " + msg); }
        public static void Info   (string msg) { if (Should(LogLevel.Info))    Debug.Log(Prefix + msg); }
        public static void Warning(string msg) { if (Should(LogLevel.Warning)) Debug.LogWarning(Prefix + msg); }
        public static void Error  (string msg) { if (Should(LogLevel.Error))   Debug.LogError(Prefix + msg); }

        // Master-switch (LogsEnabled) is checked FIRST — overrides LogLevel and ForceDebug.
        // When LogsEnabled == false, every call is a no-op regardless of severity.
        private static bool Should(LogLevel level)
        {
            var s = Settings;
            if (!s.LogsEnabled) return false;
            if (ForceDebug) return true;
            return (int)level >= (int)s.LogLevel;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Clears the cached <see cref="GamesSettings"/> instance so the next log call
        /// re-reads from disk. Called by the Games Configuration editor window after
        /// Apply so log-level changes take effect without a domain reload.
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedSettings = null;
            _loggedFallbackWarning = false;
        }
#endif
    }
}
