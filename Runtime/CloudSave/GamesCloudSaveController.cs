// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BizSim.Google.Play.Games
{
    internal class GamesCloudSaveController : JniBridgeBase, IGamesCloudSaveProvider
    {
        private int ConflictTimeoutSeconds
        {
            get
            {
                var config = GamesServicesManager.Config;
                return config != null ? config.conflictTimeoutSeconds : 60;
            }
        }

        private CloudSaveCallbackProxy _callbackProxy;

        private TaskCompletionSource<SnapshotHandle> _openTcs;
        private TaskCompletionSource<byte[]> _readTcs;
        private TaskCompletionSource<bool> _commitTcs;
        private TaskCompletionSource<bool> _deleteTcs;
        private TaskCompletionSource<string> _showUITcs;
        private int _conflictsWaiting; // the game is deciding: the pending call does not time out

        private readonly Dictionary<string, Texture2D> _coverImageCache = new();

        public event Action<SnapshotHandle> OnSnapshotOpened;
        public event Action<string> OnSnapshotCommitted;
        public event Action<SavedGameConflict> OnConflictDetected;
        public event Action<GamesCloudSaveError> OnCloudSaveError;

        protected override string JavaClassName => JniConstants.CloudSaveBridge;

        protected override AndroidJavaProxy CreateCallbackProxy()
        {
            _callbackProxy = new CloudSaveCallbackProxy(this);
            return _callbackProxy;
        }

        public GamesCloudSaveController()
        {
            InitializeBridge();
        }

        public async Task<SnapshotHandle> OpenSnapshotAsync(string filename, bool createIfNotFound = true, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            BizSimGamesLogger.Info($"[CloudSave] OpenSnapshotAsync START: filename='{filename}', createIfNotFound={createIfNotFound}");
            var tcs = TcsGuard.Replace(ref _openTcs);

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                CallBridge("openSnapshot", filename, createIfNotFound);
                BizSimGamesLogger.Info($"[CloudSave] OpenSnapshotAsync: JNI bridge called, awaiting response...");
                var result = await tcs.Task.WithJniTimeout(tcs, ct: ct, hold: () => _conflictsWaiting > 0);
                BizSimGamesLogger.Info($"[CloudSave] OpenSnapshotAsync DONE: filename='{result.filename}', hasConflict={result.hasConflict}, nativeHandle={result.nativeHandle != null}");
                return result;
            }
        }

        public async Task<byte[]> ReadSnapshotAsync(SnapshotHandle handle, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            BizSimGamesLogger.Info($"[CloudSave] ReadSnapshotAsync START: filename='{handle.filename}'");
            var tcs = TcsGuard.Replace(ref _readTcs);

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                CallBridge("readSnapshot", handle.nativeHandle);
                BizSimGamesLogger.Info("[CloudSave] ReadSnapshotAsync: JNI bridge called, awaiting data...");
                // Open and Commit have carried WithJniTimeout since they were written; Read
                // never did, and it is the one the restore path depends on. Without it a
                // bridge callback that never arrives leaves the caller waiting until the
                // controller is disposed, which reaches the game as an OperationCanceled
                // long after the fact instead of a Timeout it can report and retry.
                // Safe at the shared 30s budget because the download happens in
                // openSnapshot - readSnapshot only hands over contents already on device.
                var data = await tcs.Task.WithJniTimeout(tcs, ct: ct, hold: () => _conflictsWaiting > 0);
                BizSimGamesLogger.Info($"[CloudSave] ReadSnapshotAsync DONE: {data?.Length ?? 0} bytes");
                return data;
            }
        }

        public async Task CommitSnapshotAsync(SnapshotHandle handle, byte[] data, string description = null,
            long playedTimeMillis = 0, byte[] coverImage = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var config = GamesServicesManager.Config;
            if (config != null && config.requireCloudSaveMetadata)
            {
                if (string.IsNullOrEmpty(description))
                    BizSimGamesLogger.Warning("[CloudSave] requireCloudSaveMetadata is true but description is empty.");
                if (playedTimeMillis <= 0)
                    BizSimGamesLogger.Warning("[CloudSave] requireCloudSaveMetadata is true but playedTimeMillis is 0.");
            }

            var tcs = TcsGuard.Replace(ref _commitTcs);

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                CallBridge("commitSnapshot", handle.nativeHandle, data,
                    description ?? "", playedTimeMillis, coverImage);
                await tcs.Task.WithJniTimeout(tcs, ct: ct, hold: () => _conflictsWaiting > 0);
            }
        }

        public async Task DeleteSnapshotAsync(string filename, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var tcs = TcsGuard.Replace(ref _deleteTcs);

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                CallBridge("deleteSnapshot", filename);
                await tcs.Task.WithJniTimeout(tcs, ct: ct, hold: () => _conflictsWaiting > 0);
            }
        }

        public async Task<string> ShowSavedGamesUIAsync(string title = "Saved Games", bool allowAddButton = false,
            bool allowDelete = true, int maxSnapshots = 5, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            BizSimGamesLogger.Info($"[CloudSave] ShowSavedGamesUIAsync START: title='{title}', allowAdd={allowAddButton}, allowDelete={allowDelete}, maxSnapshots={maxSnapshots}");
            var tcs = TcsGuard.Replace(ref _showUITcs);

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                CallBridge("showSavedGamesUI", title, allowAddButton, allowDelete, maxSnapshots);
                BizSimGamesLogger.Info("[CloudSave] ShowSavedGamesUIAsync: JNI bridge called, awaiting UI result...");
                var result = await tcs.Task;
                BizSimGamesLogger.Info($"[CloudSave] ShowSavedGamesUIAsync DONE: selectedFilename='{result ?? "(null)"}'");
                return result;
            }
        }

        // A commit opens the slot itself, so a save has no open of its own: every conflict meets the commit rule
        // (server copy kept, GamesCloudSaveConflictException). The handle has the format the Java bridge gives out.
        private static SnapshotHandle SlotHandle(string filename) =>
            new SnapshotHandle { filename = filename, nativeHandle = "snapshot:" + filename };

        public async Task SaveAsync(string filename, byte[] data, string description = null, CancellationToken ct = default)
        {
            var config = GamesServicesManager.Config;
            if (config != null && config.requireCloudSaveMetadata)
                BizSimGamesLogger.Warning("[CloudSave] Using SaveAsync without metadata. " +
                    "For Sidekick Tier 2 compliance, use the SaveAsync(filename, data, SaveGameMetadata) overload.");

            await CommitSnapshotAsync(SlotHandle(filename), data, description, 0, null, ct);
        }

        public async Task SaveAsync(string filename, byte[] data, SaveGameMetadata metadata, CancellationToken ct = default)
        {
            ValidateMetadata(metadata);

            await CommitSnapshotAsync(SlotHandle(filename), data, metadata?.description,
                metadata?.playedTimeMillis ?? 0, metadata?.coverImage, ct);
        }

        public async Task<Texture2D> DownloadCoverImageAsync(string coverImageUri, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(coverImageUri))
                return null;

            if (_coverImageCache.TryGetValue(coverImageUri, out var cached) && cached != null)
                return cached;

            using var request = UnityWebRequestTexture.GetTexture(coverImageUri);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                BizSimGamesLogger.Error($"[CloudSave] Cover image download failed: {request.error}");
                return null;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            _coverImageCache[coverImageUri] = texture;
            return texture;
        }

        public void ReleaseCoverImage(Texture2D texture)
        {
            if (texture == null) return;

            string keyToRemove = null;
            foreach (var kvp in _coverImageCache)
            {
                if (kvp.Value == texture)
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }

            if (keyToRemove != null)
                _coverImageCache.Remove(keyToRemove);

            UnityEngine.Object.Destroy(texture);
        }

        public void ReleaseAllCoverImages()
        {
            foreach (var kvp in _coverImageCache)
            {
                if (kvp.Value != null)
                    UnityEngine.Object.Destroy(kvp.Value);
            }
            _coverImageCache.Clear();
        }

        private void ValidateMetadata(SaveGameMetadata metadata)
        {
            var config = GamesServicesManager.Config;
            if (config == null || !config.requireCloudSaveMetadata)
                return;

            if (string.IsNullOrEmpty(metadata?.description))
                BizSimGamesLogger.Warning("[CloudSave] Sidekick Tier 2 requires save description.");

            if (metadata == null || metadata.playedTimeMillis <= 0)
                BizSimGamesLogger.Warning("[CloudSave] Sidekick Tier 2 requires playedTimeMillis > 0.");

            if (metadata?.coverImage == null || metadata.coverImage.Length == 0)
                BizSimGamesLogger.Warning("[CloudSave] Sidekick Tier 2 recommends cover image.");

            if (metadata?.coverImage != null && metadata.coverImage.Length > 800 * 1024)
                BizSimGamesLogger.Error("[CloudSave] Cover image exceeds 800KB hard limit (" +
                    (metadata.coverImage.Length / 1024) + "KB). Commit will fail. " +
                    "Resize to 640x360 or lower.");
            else if (metadata?.coverImage != null && metadata.coverImage.Length > 512 * 1024)
                BizSimGamesLogger.Warning("[CloudSave] Cover image is " +
                    (metadata.coverImage.Length / 1024) + "KB. Recommended max 512KB for safety margin " +
                    "(Google limit: 800KB). Consider resizing to 640x360.");
        }

        public async Task<byte[]> LoadAsync(string filename, CancellationToken ct = default)
        {
            BizSimGamesLogger.Info($"[CloudSave] LoadAsync START: filename='{filename}'");
            try
            {
                var handle = await OpenSnapshotAsync(filename, false, ct);
                BizSimGamesLogger.Info($"[CloudSave] LoadAsync: snapshot opened, reading data...");
                var data = await ReadSnapshotAsync(handle, ct);
                BizSimGamesLogger.Info($"[CloudSave] LoadAsync DONE: {data?.Length ?? 0} bytes loaded");
                return data;
            }
            catch (GamesCloudSaveException ex) when (ex.Error.Type == CloudSaveErrorType.SnapshotNotFound)
            {
                BizSimGamesLogger.Info($"[CloudSave] LoadAsync: Snapshot '{filename}' not found, returning null");
                return null;
            }
        }

        internal void OnSnapshotOpenedFromJava(string filename, string snapshotJson, bool hasConflict)
        {
            try
            {
                BizSimGamesLogger.Info($"[CloudSave] OnSnapshotOpenedFromJava: dispatched to main thread, filename='{filename}', openTcs={(_openTcs != null ? "exists" : "NULL")}");
                var handle = JsonUtility.FromJson<SnapshotHandle>(snapshotJson);
                handle.hasConflict = hasConflict;

                OnSnapshotOpened?.Invoke(handle);
                bool wasSet = _openTcs?.TrySetResult(handle) ?? false;
                BizSimGamesLogger.Info($"[CloudSave] OnSnapshotOpenedFromJava: TrySetResult={wasSet}");
            }
            catch (Exception ex)
            {
                BizSimGamesLogger.Error($"[CloudSave] OnSnapshotOpenedFromJava ERROR: {ex.Message}");
                _openTcs?.TrySetException(ex);
            }
        }

        internal void OnSnapshotReadFromJava(string filename, byte[] data)
        {
            _readTcs?.TrySetResult(data);
        }

        internal void OnSnapshotCommittedFromJava(string filename)
        {
            OnSnapshotCommitted?.Invoke(filename);
            _commitTcs?.TrySetResult(true);
        }

        internal void OnSnapshotDeletedFromJava(string filename)
        {
            _deleteTcs?.TrySetResult(true);
        }

        internal void OnSavedGamesUIResultFromJava(string selectedFilename)
        {
            BizSimGamesLogger.Info($"[CloudSave] OnSavedGamesUIResultFromJava: selectedFilename='{selectedFilename ?? "(null)"}', showUITcs={(_showUITcs != null ? "exists" : "NULL")}");
            bool wasSet = _showUITcs?.TrySetResult(selectedFilename) ?? false;
            BizSimGamesLogger.Info($"[CloudSave] OnSavedGamesUIResultFromJava: TrySetResult={wasSet}");
        }

        [Serializable, UnityEngine.Scripting.Preserve]
        private class ConflictInfo
        {
            public string conflictKey;
            public string conflictOperation;
        }

        internal void OnConflictDetectedFromJava(string localSnapshotJson, string serverSnapshotJson,
            byte[] localData, byte[] serverData)
        {
            try
            {
                var info = JsonUtility.FromJson<ConflictInfo>(serverSnapshotJson);
                bool answered = false;
                bool waiting = false;

                void Answer(ConflictResolution resolution)
                {
                    if (answered) return;
                    answered = true;
                    if (waiting && _conflictsWaiting > 0) _conflictsWaiting--;
                    if (Bridge == null) return; // disposed

                    // UseManual is not implemented and means UseLocal.
                    var choice = resolution == ConflictResolution.UseManual
                        ? ConflictResolution.UseLocal
                        : resolution;
                    CallBridge("resolveConflict", choice.ToString(), info.conflictKey);
                }

                var conflict = new SavedGameConflict
                {
                    operation = ParseOperation(info.conflictOperation),
                    localSnapshot = JsonUtility.FromJson<SnapshotHandle>(localSnapshotJson),
                    serverSnapshot = JsonUtility.FromJson<SnapshotHandle>(serverSnapshotJson),
                    localData = localData,
                    serverData = serverData,
                    ResolveAsync = resolution =>
                    {
                        Answer(resolution);
                        return Task.CompletedTask;
                    }
                };

                // On a conflict the Java bridge calls onConflictDetected INSTEAD of the answer the
                // pending call waits for, so every conflict must be resolved or that call hangs.
                // A subscriber answers with ResolveAsync; without one, or with no answer in
                // conflictTimeoutSeconds, UseServer is chosen.
                //
                // UseServer, never the newer timestamp: a fresh install auto-saves within minutes,
                // so its empty save is newer than the months-old real one, and "newest wins" would
                // discard the progress the player is trying to recover. For the same reason a commit
                // or delete keeps the server copy and fails (OnConflictRejectedFromJava).
                var handler = OnConflictDetected;
                int timeoutSeconds = handler != null ? ConflictTimeoutSeconds : 0;

                if (timeoutSeconds > 0)
                {
                    waiting = true;
                    _conflictsWaiting++;
                }

                try
                {
                    handler?.Invoke(conflict);
                }
                catch (Exception ex)
                {
                    BizSimGamesLogger.Error($"[CloudSave] OnConflictDetected handler failed: {ex.Message}");
                }

                if (timeoutSeconds > 0)
                    _ = AnswerAfterTimeout(Answer, timeoutSeconds);
                else
                    Answer(ConflictResolution.UseServer);
            }
            catch (Exception ex)
            {
                BizSimGamesLogger.Error($"Conflict handling error: {ex.Message}");
            }
        }

        private static CloudSaveOperation ParseOperation(string name) =>
            Enum.TryParse(name, out CloudSaveOperation operation) ? operation : CloudSaveOperation.Open;

        internal void OnConflictRejectedFromJava(string operationName, string filename, string serverSnapshotJson)
        {
            var operation = ParseOperation(operationName);
            var error = new GamesCloudSaveError((int)CloudSaveErrorType.Conflict,
                $"{operation} met a save conflict; the server copy was kept", filename);
            SnapshotHandle server = null;
            try { server = JsonUtility.FromJson<SnapshotHandle>(serverSnapshotJson); }
            catch (Exception ex) { BizSimGamesLogger.Error($"[CloudSave] Server snapshot parse failed: {ex.Message}"); }

            var exception = new GamesCloudSaveConflictException(error, operation, server);
            if (operation == CloudSaveOperation.Delete)
                _deleteTcs?.TrySetException(exception);
            else
                _commitTcs?.TrySetException(exception);
            OnCloudSaveError?.Invoke(error);
        }

        private static async Task AnswerAfterTimeout(Action<ConflictResolution> answer, int timeoutSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            answer(ConflictResolution.UseServer);
        }

        internal void OnCloudSaveErrorFromJava(int errorCode, string errorMessage, string filename)
        {
            string detail = $"[CloudSave] OnCloudSaveErrorFromJava: code={errorCode}, msg='{errorMessage}', file='{filename}', pending TCS: open={_openTcs != null}, read={_readTcs != null}, commit={_commitTcs != null}, delete={_deleteTcs != null}, showUI={_showUITcs != null}";
            if (errorCode == 3)
                BizSimGamesLogger.Info(detail);
            else
                BizSimGamesLogger.Error(detail);
            var error = new GamesCloudSaveError(errorCode, errorMessage, filename);
            OnCloudSaveError?.Invoke(error);

            var exception = new GamesCloudSaveException(error);
            _openTcs?.TrySetException(exception);
            _readTcs?.TrySetException(exception);
            _commitTcs?.TrySetException(exception);
            _deleteTcs?.TrySetException(exception);
            _showUITcs?.TrySetException(exception);
        }

        protected override void OnDispose()
        {
            _conflictsWaiting = 0;
            _openTcs?.TrySetCanceled();
            _readTcs?.TrySetCanceled();
            _commitTcs?.TrySetCanceled();
            _deleteTcs?.TrySetCanceled();
            _showUITcs?.TrySetCanceled();

            ReleaseAllCoverImages();
            _callbackProxy = null;

            try { Bridge?.Call("shutdown"); }
            catch (System.Exception) { }
        }
    }
}
