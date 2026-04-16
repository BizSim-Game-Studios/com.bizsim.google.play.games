using System.Threading;
using UnityEngine;

namespace BizSim.Google.Play.Games.Samples
{
    /// <summary>
    /// Entry point for the Games Services basic integration sample.
    /// Demonstrates the full authentication → feature usage flow.
    /// Attach to a GameObject in the BasicIntegration scene.
    /// </summary>
    public class BasicIntegrationBootstrap : MonoBehaviour
    {
        CancellationTokenSource _cts;

        void Start()
        {
            _cts = new CancellationTokenSource();
            RunAsync();
        }

        void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        async void RunAsync()
        {
            var mgr = GamesServicesManager.Instance;
            var ct = _cts.Token;

            // 1. Authenticate
            Debug.Log("[Games Sample] Signing in...");
            try
            {
                var player = await mgr.Auth.AuthenticateAsync(ct);
                Debug.Log($"[Games Sample] Signed in as {player.DisplayName} ({player.PlayerId})");
            }
            catch (GamesAuthException e)
            {
                Debug.LogError($"[Games Sample] Auth failed: {e.Error.Type} — {e.Error.errorMessage}");
                return;
            }

            // 2. Load achievements
            Debug.Log("[Games Sample] Loading achievements...");
            try
            {
                var achievements = await mgr.Achievements.LoadAchievementsAsync(false, ct);
                Debug.Log($"[Games Sample] Loaded {achievements.Count} achievements");
                foreach (var a in achievements)
                {
                    string progress = a.type == AchievementType.Incremental
                        ? $" ({a.currentSteps}/{a.totalSteps})"
                        : "";
                    Debug.Log($"  • {a.name}: {a.state}{progress}");
                }
            }
            catch (GamesAchievementException e)
            {
                Debug.LogWarning($"[Games Sample] Achievements error: {e.Error.Type}");
            }

            // 3. Submit a score
            Debug.Log("[Games Sample] Submitting score...");
            try
            {
                await mgr.Leaderboards.SubmitScoreAsync("leaderboard_high_score", 42000, null, ct);
                Debug.Log("[Games Sample] Score submitted successfully");
            }
            catch (GamesLeaderboardException e)
            {
                Debug.LogWarning($"[Games Sample] Leaderboard error: {e.Error.Type}");
            }

            // 4. Load top scores
            try
            {
                var scores = await mgr.Leaderboards.LoadTopScoresAsync(
                    "leaderboard_high_score", LeaderboardTimeSpan.AllTime,
                    LeaderboardCollection.Public, 10, ct);
                Debug.Log($"[Games Sample] Top {scores.Count} scores:");
                foreach (var s in scores)
                    Debug.Log($"  #{s.rank} {s.displayName}: {s.formattedScore}");
            }
            catch (GamesLeaderboardException e)
            {
                Debug.LogWarning($"[Games Sample] Load scores error: {e.Error.Type}");
            }

            // 5. Load player stats
            try
            {
                var stats = await mgr.Stats.LoadPlayerStatsAsync(false, ct);
                Debug.Log($"[Games Sample] Player stats — Sessions: {stats.numberOfSessions}, " +
                          $"Churn: {stats.churnProbability:P0}, " +
                          $"Avg session: {stats.avgSessionLengthMinutes:F1} min");
            }
            catch (GamesStatsException e)
            {
                Debug.LogWarning($"[Games Sample] Stats error: {e.Error.Type}");
            }

            Debug.Log("[Games Sample] Basic integration demo complete.");
        }
    }
}
