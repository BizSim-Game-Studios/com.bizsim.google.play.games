// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.Games
{
    // Null-object providers for players that are neither Android nor the Editor (iOS, standalone, WebGL).
    // IsSupported is false, every call completes without doing anything and results are empty/null.
    // Events use empty accessors so nothing is stored or raised.

    internal sealed class UnsupportedAuthProvider : IGamesAuthProvider
    {
        public bool IsSupported => false;
        public event Action<GamesPlayer> OnAuthenticationSuccess { add { } remove { } }
        public event Action<GamesAuthError> OnAuthenticationFailed { add { } remove { } }
        public bool IsAuthenticated => false;
        public GamesPlayer CurrentPlayer => null;
        public Task<GamesPlayer> AuthenticateAsync(CancellationToken cancellationToken = default) => Task.FromResult<GamesPlayer>(null);
        public Task<bool> CheckAuthenticatedAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<string> RequestServerSideAccessAsync(string serverClientId, bool forceRefresh = false, CancellationToken cancellationToken = default)
            => Task.FromResult<string>(null);
        public Task<GamesAuthResponse> RequestServerSideAccessWithScopesAsync(string serverClientId, bool forceRefresh,
            List<GamesAuthScope> scopes, CancellationToken cancellationToken = default)
            => Task.FromResult<GamesAuthResponse>(null);
    }

    internal sealed class UnsupportedAchievementProvider : IGamesAchievementProvider
    {
        public bool IsSupported => false;
        public Task UnlockAchievementAsync(string achievementId, CancellationToken ct = default) => Task.CompletedTask;
        public Task IncrementAchievementAsync(string achievementId, int steps, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevealAchievementAsync(string achievementId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ShowAchievementsUIAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<GamesAchievement>> LoadAchievementsAsync(bool forceReload = false, CancellationToken ct = default)
            => Task.FromResult(new List<GamesAchievement>());
        public Task UnlockMultipleAsync(List<string> achievementIds, CancellationToken ct = default) => Task.CompletedTask;
        public event Action<string> OnAchievementUnlocked { add { } remove { } }
        public event Action<string, int> OnAchievementIncremented { add { } remove { } }
        public event Action<string> OnAchievementRevealed { add { } remove { } }
        public event Action<GamesAchievementError> OnAchievementError { add { } remove { } }
    }

    internal sealed class UnsupportedLeaderboardProvider : IGamesLeaderboardProvider
    {
        public bool IsSupported => false;
        public Task SubmitScoreAsync(string leaderboardId, long score, string scoreTag = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task ShowLeaderboardUIAsync(string leaderboardId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ShowAllLeaderboardsUIAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<GamesLeaderboardEntry>> LoadTopScoresAsync(string leaderboardId,
            LeaderboardTimeSpan timeSpan = LeaderboardTimeSpan.AllTime, LeaderboardCollection collection = LeaderboardCollection.Public,
            int maxResults = 25, CancellationToken ct = default)
            => Task.FromResult(new List<GamesLeaderboardEntry>());
        public Task<List<GamesLeaderboardEntry>> LoadPlayerCenteredScoresAsync(string leaderboardId,
            LeaderboardTimeSpan timeSpan = LeaderboardTimeSpan.AllTime, LeaderboardCollection collection = LeaderboardCollection.Public,
            int maxResults = 25, CancellationToken ct = default)
            => Task.FromResult(new List<GamesLeaderboardEntry>());
        public event Action<string, long> OnScoreSubmitted { add { } remove { } }
        public event Action<string, List<GamesLeaderboardEntry>> OnScoresLoaded { add { } remove { } }
        public event Action<GamesLeaderboardError> OnLeaderboardError { add { } remove { } }
    }

    internal sealed class UnsupportedCloudSaveProvider : IGamesCloudSaveProvider
    {
        public bool IsSupported => false;
        public Task<SnapshotHandle> OpenSnapshotAsync(string filename, bool createIfNotFound = true, CancellationToken ct = default)
            => Task.FromResult<SnapshotHandle>(null);
        public Task<byte[]> ReadSnapshotAsync(SnapshotHandle handle, CancellationToken ct = default) => Task.FromResult<byte[]>(null);
        public Task CommitSnapshotAsync(SnapshotHandle handle, byte[] data, string description = null,
            long playedTimeMillis = 0, byte[] coverImage = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteSnapshotAsync(string filename, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> ShowSavedGamesUIAsync(string title = "Saved Games", bool allowAddButton = false,
            bool allowDelete = true, int maxSnapshots = 5, CancellationToken ct = default) => Task.FromResult<string>(null);
        public Task SaveAsync(string filename, byte[] data, string description = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveAsync(string filename, byte[] data, SaveGameMetadata metadata, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Texture2D> DownloadCoverImageAsync(string coverImageUri, CancellationToken ct = default) => Task.FromResult<Texture2D>(null);
        public void ReleaseCoverImage(Texture2D texture) { }
        public void ReleaseAllCoverImages() { }
        public Task<byte[]> LoadAsync(string filename, CancellationToken ct = default) => Task.FromResult<byte[]>(null);
        public event Action<SnapshotHandle> OnSnapshotOpened { add { } remove { } }
        public event Action<string> OnSnapshotCommitted { add { } remove { } }
        public event Action<SavedGameConflict> OnConflictDetected { add { } remove { } }
        public event Action<GamesCloudSaveError> OnCloudSaveError { add { } remove { } }
    }

    internal sealed class UnsupportedStatsProvider : IGamesStatsProvider
    {
        public bool IsSupported => false;
        public event Action<GamesPlayerStats> OnStatsLoaded { add { } remove { } }
        public event Action<GamesStatsError> OnStatsError { add { } remove { } }
        public Task<GamesPlayerStats> LoadPlayerStatsAsync(bool forceReload = false, CancellationToken ct = default)
            => Task.FromResult<GamesPlayerStats>(default);
    }

    internal sealed class UnsupportedEventsProvider : IGamesEventsProvider
    {
        public bool IsSupported => false;
        public Task IncrementEventAsync(string eventId, int steps = 1, CancellationToken ct = default) => Task.CompletedTask;
        public Task<GamesEvent[]> LoadEventsAsync(CancellationToken ct = default) => Task.FromResult(new GamesEvent[0]);
        public Task<GamesEvent> LoadEventAsync(string eventId, CancellationToken ct = default) => Task.FromResult<GamesEvent>(default);
        public event Action<GamesEventsError> OnEventsError { add { } remove { } }
    }
}
