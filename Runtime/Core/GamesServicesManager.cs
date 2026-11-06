// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using UnityEngine;

namespace BizSim.Google.Play.Games
{
    /// <summary>
    /// Central entry point for all Google Play Games Services features.
    /// Auto-initializes via [RuntimeInitializeOnLoadMethod] before any scene loads.
    /// On Android, creates JNI bridge controllers. In Editor, creates mock providers.
    /// </summary>
    [DefaultExecutionOrder(-999)]
    public class GamesServicesManager : MonoBehaviour
    {
        private static volatile GamesServicesManager _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting;

        private GamesServicesConfig _config;
        private IGamesAuthProvider _authProvider;
        private IGamesAchievementProvider _achievementsProvider;
        private IGamesLeaderboardProvider _leaderboardsProvider;
        private IGamesCloudSaveProvider _cloudSaveProvider;
        private IGamesStatsProvider _statsProvider;
        private IGamesEventsProvider _eventsProvider;
        private bool _started;

        /// <summary>
        /// True once the providers exist. Always true right after startup unless
        /// <see cref="GamesServicesConfig.deferStart"/> is set, in which case it turns true
        /// when <see cref="StartServices"/> runs.
        /// </summary>
        public static bool IsStarted => _instance != null && _instance._started;

        /// <summary>Raised once, on the main thread, after the providers were created.</summary>
        public static event Action Started;

        /// <summary>
        /// Creates the providers (on Android this initializes the Play Games SDK, which begins
        /// the automatic sign-in). Only needed with <see cref="GamesServicesConfig.deferStart"/>;
        /// idempotent. Call it from the main thread, e.g. after the age answer is known.
        /// </summary>
        public static void StartServices()
        {
            var manager = Instance;
            if (manager != null) manager.StartInternal();
        }

        public static GamesServicesManager Instance
        {
            get
            {
                if (_isQuitting) return null;

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            Initialize();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>Active configuration. Loaded from Resources/GamesServicesConfig or defaults.</summary>
        public static GamesServicesConfig Config => Instance?._config;

        /// <summary>Current Sidekick readiness tier based on enabled features.</summary>
        public static SidekickTier SidekickStatus => SidekickReadiness.Evaluate(Instance?._config);

        /// <summary>Authentication provider — sign in, server auth codes, scoped access.</summary>
        public static IGamesAuthProvider Auth => Instance?._authProvider;

        /// <summary>Achievements provider — unlock, increment, reveal, load, batch unlock.</summary>
        public static IGamesAchievementProvider Achievements => Instance?._achievementsProvider;

        /// <summary>Leaderboards provider — submit scores, load top/centered scores, show UI.</summary>
        public static IGamesLeaderboardProvider Leaderboards => Instance?._leaderboardsProvider;

        /// <summary>Cloud save provider — open/read/commit snapshots, conflict resolution, cover images.</summary>
        public static IGamesCloudSaveProvider CloudSave => Instance?._cloudSaveProvider;

        /// <summary>Player stats provider — session length, churn probability, spend percentile.</summary>
        public static IGamesStatsProvider Stats => Instance?._statsProvider;

        /// <summary>Events provider — increment events with batching, load event data.</summary>
        public static IGamesEventsProvider Events => Instance?._eventsProvider;

        public IGamesAuthProvider AuthProvider => _authProvider;
        public IGamesAchievementProvider AchievementsProvider => _achievementsProvider;
        public IGamesLeaderboardProvider LeaderboardsProvider => _leaderboardsProvider;
        public IGamesCloudSaveProvider CloudSaveProvider => _cloudSaveProvider;
        public IGamesStatsProvider StatsProvider => _statsProvider;
        public IGamesEventsProvider EventsProvider => _eventsProvider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null || _isQuitting) return;

            _isQuitting = false;
            Application.quitting += () => _isQuitting = true;

            var go = new GameObject("[GamesServicesManager]");
            _instance = go.AddComponent<GamesServicesManager>();
            DontDestroyOnLoad(go);

            BizSimGamesLogger.Info("GamesServicesManager initialized");
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            _instance = this;
            ResolveConfig();

            if (_config.deferStart)
            {
                BizSimGamesLogger.Info("Start deferred: call GamesServicesManager.StartServices()");
                return;
            }

            StartInternal();
        }

        private void StartInternal()
        {
            if (_started) return;
            _started = true;
            InitializeServices();
            Started?.Invoke();
        }

        private void InitializeServices()
        {
            if (_config.debugMode)
                BizSimGamesLogger.ForceDebug = true;

            #if UNITY_ANDROID && !UNITY_EDITOR
                BizSimGamesLogger.Info("Platform: Android (JNI Bridge)");
                if (_config.enableAuth)
                    _authProvider = TryCreateProvider(() => (IGamesAuthProvider)new GamesAuthController(), () => new UnsupportedAuthProvider(), "Auth");
                if (_config.enableAchievements)
                    _achievementsProvider = TryCreateProvider(() => (IGamesAchievementProvider)new GamesAchievementController(), () => new UnsupportedAchievementProvider(), "Achievements");
                if (_config.enableLeaderboards)
                    _leaderboardsProvider = TryCreateProvider(() => (IGamesLeaderboardProvider)new GamesLeaderboardController(), () => new UnsupportedLeaderboardProvider(), "Leaderboards");
                if (_config.enableCloudSave)
                    _cloudSaveProvider = TryCreateProvider(() => (IGamesCloudSaveProvider)new GamesCloudSaveController(), () => new UnsupportedCloudSaveProvider(), "CloudSave");
                if (_config.enableStats)
                    _statsProvider = TryCreateProvider(() => (IGamesStatsProvider)new GamesStatsController(), () => new UnsupportedStatsProvider(), "Stats");
                if (_config.enableEvents)
                    _eventsProvider = TryCreateProvider(() => (IGamesEventsProvider)new GamesEventsController(), () => new UnsupportedEventsProvider(), "Events");
            #elif UNITY_EDITOR
                BizSimGamesLogger.Info("Platform: Editor (Mock Provider)");
                var mockData = _config.editorMock;
                if (_config.enableAuth)
                    _authProvider = new MockAuthProvider(mockData);
                if (_config.enableAchievements)
                    _achievementsProvider = new MockAchievementProvider(mockData);
                if (_config.enableLeaderboards)
                    _leaderboardsProvider = new MockLeaderboardProvider(mockData);
                if (_config.enableCloudSave)
                    _cloudSaveProvider = new MockCloudSaveProvider(mockData);
                if (_config.enableStats)
                    _statsProvider = new MockStatsProvider(mockData);
                if (_config.enableEvents)
                    _eventsProvider = new MockEventsProvider(mockData);
            #else
                // iOS, standalone, WebGL: no mock data in a player, no service behind the calls.
                BizSimGamesLogger.Info("Platform: unsupported (no-op providers)");
                if (_config.enableAuth)
                    _authProvider = new UnsupportedAuthProvider();
                if (_config.enableAchievements)
                    _achievementsProvider = new UnsupportedAchievementProvider();
                if (_config.enableLeaderboards)
                    _leaderboardsProvider = new UnsupportedLeaderboardProvider();
                if (_config.enableCloudSave)
                    _cloudSaveProvider = new UnsupportedCloudSaveProvider();
                if (_config.enableStats)
                    _statsProvider = new UnsupportedStatsProvider();
                if (_config.enableEvents)
                    _eventsProvider = new UnsupportedEventsProvider();
            #endif
        }

        private static T TryCreateProvider<T>(Func<T> factory, Func<T> fallback, string name)
        {
            try
            {
                return factory();
            }
            catch (Exception ex)
            {
                BizSimGamesLogger.Warning($"Failed to initialize {name} provider, falling back to unsupported: {ex.Message}");
                return fallback();
            }
        }

        private void ResolveConfig()
        {
            _config = Resources.Load<GamesServicesConfig>("GamesServicesConfig");

            if (_config == null)
            {
                BizSimGamesLogger.Warning("No GamesServicesConfig found in Resources. " +
                    "Using default config with all services enabled.");
                _config = ScriptableObject.CreateInstance<GamesServicesConfig>();
            }

            if (_config.hideFlags == HideFlags.None && !IsPersistedAsset(_config))
                _config.hideFlags = HideFlags.HideAndDontSave;
        }

        private static bool IsPersistedAsset(ScriptableObject obj)
        {
            #if UNITY_EDITOR
                return UnityEditor.AssetDatabase.Contains(obj);
            #else
                return false;
            #endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _eventsProvider is GamesEventsController eventsController)
                eventsController.FlushPendingIncrements();
        }

        private void OnApplicationQuit()
        {
            if (_eventsProvider is GamesEventsController eventsController)
                eventsController.FlushPendingIncrements();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                (_authProvider as IDisposable)?.Dispose();
                (_achievementsProvider as IDisposable)?.Dispose();
                (_leaderboardsProvider as IDisposable)?.Dispose();
                (_cloudSaveProvider as IDisposable)?.Dispose();
                (_statsProvider as IDisposable)?.Dispose();
                (_eventsProvider as IDisposable)?.Dispose();

                _instance = null;
            }
        }
    }
}
