using System.Threading;
using UnityEngine;

namespace BizSim.Google.Play.Games.Samples
{
    /// <summary>
    /// Demonstrates achievement unlock, increment, reveal, and UI operations.
    /// Call methods from UI buttons or game logic.
    /// </summary>
    public class AchievementsSample : MonoBehaviour
    {
        [SerializeField] string _standardAchievementId = "achievement_first_trade";
        [SerializeField] string _incrementalAchievementId = "achievement_ten_trades";

        CancellationTokenSource _cts;

        void OnEnable() => _cts = new CancellationTokenSource();
        void OnDestroy() { _cts?.Cancel(); _cts?.Dispose(); }

        IGamesAchievementProvider Achievements => GamesServicesManager.Instance.Achievements;

        public async void UnlockStandard()
        {
            try
            {
                await Achievements.UnlockAchievementAsync(_standardAchievementId, _cts.Token);
                Debug.Log($"[Games Sample] Unlocked: {_standardAchievementId}");
            }
            catch (GamesAchievementException e)
            {
                Debug.LogError($"[Games Sample] Unlock failed: {e.Error.Type}");
            }
        }

        public async void IncrementProgress()
        {
            try
            {
                await Achievements.IncrementAchievementAsync(_incrementalAchievementId, 1, _cts.Token);
                Debug.Log($"[Games Sample] Incremented: {_incrementalAchievementId}");
            }
            catch (GamesAchievementException e)
            {
                Debug.LogError($"[Games Sample] Increment failed: {e.Error.Type}");
            }
        }

        public async void ShowUI()
        {
            try
            {
                await Achievements.ShowAchievementsUIAsync(_cts.Token);
            }
            catch (GamesAchievementException e)
            {
                Debug.LogError($"[Games Sample] Show UI failed: {e.Error.Type}");
            }
        }
    }
}
