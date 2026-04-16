using System.Text;
using System.Threading;
using UnityEngine;

namespace BizSim.Google.Play.Games.Samples
{
    /// <summary>
    /// Demonstrates Cloud Save: save, load, conflict handling, and the saved games UI.
    /// </summary>
    public class CloudSaveSample : MonoBehaviour
    {
        [SerializeField] string _saveFilename = "player_progress";

        CancellationTokenSource _cts;

        void OnEnable()
        {
            _cts = new CancellationTokenSource();

            GamesServicesManager.Instance.CloudSave.OnConflictDetected += HandleConflict;
        }

        void OnDisable()
        {
            GamesServicesManager.Instance.CloudSave.OnConflictDetected -= HandleConflict;
        }

        void OnDestroy() { _cts?.Cancel(); _cts?.Dispose(); }

        IGamesCloudSaveProvider CloudSave => GamesServicesManager.Instance.CloudSave;

        public async void SaveProgress()
        {
            string json = "{\"level\":5,\"coins\":1200}";
            byte[] data = Encoding.UTF8.GetBytes(json);

            try
            {
                await CloudSave.SaveAsync(
                    _saveFilename, data,
                    new SaveGameMetadata
                    {
                        description = "Level 5 — 1200 coins",
                        playedTimeMillis = (long)(Time.realtimeSinceStartup * 1000)
                    },
                    _cts.Token);
                Debug.Log("[Games Sample] Progress saved to cloud");
            }
            catch (GamesCloudSaveException e)
            {
                Debug.LogError($"[Games Sample] Save failed: {e.Error.Type} — {e.Error.errorMessage}");
            }
        }

        public async void LoadProgress()
        {
            try
            {
                byte[] data = await CloudSave.LoadAsync(_saveFilename, _cts.Token);
                string json = Encoding.UTF8.GetString(data);
                Debug.Log($"[Games Sample] Loaded: {json}");
            }
            catch (GamesCloudSaveException e)
            {
                if (e.Error.Type == CloudSaveErrorType.SnapshotNotFound)
                    Debug.Log("[Games Sample] No save found — start fresh");
                else
                    Debug.LogError($"[Games Sample] Load failed: {e.Error.Type}");
            }
        }

        public async void ShowSavedGamesUI()
        {
            try
            {
                string selected = await CloudSave.ShowSavedGamesUIAsync(
                    "Saved Games", allowAddButton: true, allowDelete: true, maxSnapshots: 5, _cts.Token);
                Debug.Log($"[Games Sample] Selected save: {selected}");
            }
            catch (GamesCloudSaveException e)
            {
                Debug.LogError($"[Games Sample] UI error: {e.Error.Type}");
            }
        }

        async void HandleConflict(SavedGameConflict conflict)
        {
            Debug.Log($"[Games Sample] Conflict detected on '{conflict.localSnapshot.filename}'");
            Debug.Log($"  Local: {Encoding.UTF8.GetString(conflict.localData)}");
            Debug.Log($"  Server: {Encoding.UTF8.GetString(conflict.serverData)}");

            // Pick the newer save based on timestamp
            var resolution = conflict.localSnapshot.lastModifiedTimestamp >=
                             conflict.serverSnapshot.lastModifiedTimestamp
                ? ConflictResolution.UseLocal
                : ConflictResolution.UseServer;

            Debug.Log($"[Games Sample] Resolving with: {resolution}");
            await conflict.ResolveAsync(resolution);
        }
    }
}
