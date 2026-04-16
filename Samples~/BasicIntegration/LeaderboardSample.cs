using System.Threading;
using UnityEngine;

namespace BizSim.Google.Play.Games.Samples
{
    /// <summary>
    /// Demonstrates leaderboard score submission, top scores loading, and native UI.
    /// </summary>
    public class LeaderboardSample : MonoBehaviour
    {
        [SerializeField] string _leaderboardId = "leaderboard_high_score";
        [SerializeField] long _score = 42000;

        CancellationTokenSource _cts;

        void OnEnable() => _cts = new CancellationTokenSource();
        void OnDestroy() { _cts?.Cancel(); _cts?.Dispose(); }

        IGamesLeaderboardProvider Leaderboards => GamesServicesManager.Instance.Leaderboards;

        public async void SubmitScore()
        {
            try
            {
                await Leaderboards.SubmitScoreAsync(_leaderboardId, _score, null, _cts.Token);
                Debug.Log($"[Games Sample] Submitted score: {_score}");
            }
            catch (GamesLeaderboardException e)
            {
                Debug.LogError($"[Games Sample] Submit failed: {e.Error.Type}");
            }
        }

        public async void LoadTopScores()
        {
            try
            {
                var scores = await Leaderboards.LoadTopScoresAsync(
                    _leaderboardId, LeaderboardTimeSpan.AllTime,
                    LeaderboardCollection.Public, 25, _cts.Token);

                Debug.Log($"[Games Sample] Top {scores.Count} scores:");
                foreach (var s in scores)
                    Debug.Log($"  #{s.rank} {s.displayName}: {s.formattedScore}");
            }
            catch (GamesLeaderboardException e)
            {
                Debug.LogError($"[Games Sample] Load failed: {e.Error.Type}");
            }
        }

        public async void LoadFriendsScores()
        {
            try
            {
                var scores = await Leaderboards.LoadPlayerCenteredScoresAsync(
                    _leaderboardId, LeaderboardTimeSpan.Weekly,
                    LeaderboardCollection.Friends, 10, _cts.Token);

                Debug.Log($"[Games Sample] Friends scores ({scores.Count}):");
                foreach (var s in scores)
                    Debug.Log($"  #{s.rank} {s.displayName}: {s.formattedScore}");
            }
            catch (GamesLeaderboardException e)
            {
                Debug.LogError($"[Games Sample] Friends load failed: {e.Error.Type}");
            }
        }

        public async void ShowNativeUI()
        {
            try
            {
                await Leaderboards.ShowLeaderboardUIAsync(_leaderboardId, _cts.Token);
            }
            catch (GamesLeaderboardException e)
            {
                Debug.LogError($"[Games Sample] Show UI failed: {e.Error.Type}");
            }
        }
    }
}
