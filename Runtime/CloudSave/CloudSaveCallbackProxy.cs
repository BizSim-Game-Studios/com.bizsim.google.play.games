// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using UnityEngine;

namespace BizSim.Google.Play.Games
{
    internal class CloudSaveCallbackProxy : AndroidJavaProxy
    {
        private readonly GamesCloudSaveController _controller;

        public CloudSaveCallbackProxy(GamesCloudSaveController controller)
            : base(JniConstants.CloudSaveCallback)
        {
            _controller = controller;
        }

        void onSnapshotOpened(string filename, string snapshotJson, bool hasConflict)
        {
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onSnapshotOpened: filename='{filename}', hasConflict={hasConflict}, json={snapshotJson?.Length ?? 0} chars");
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onSnapshotOpened raw json: {snapshotJson}");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnSnapshotOpenedFromJava(filename, snapshotJson, hasConflict));
        }

        void onSnapshotRead(string filename, string dataBase64)
        {
            byte[] data = FromBase64(dataBase64);
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onSnapshotRead: filename='{filename}', dataSize={data?.Length ?? 0} bytes");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnSnapshotReadFromJava(filename, data));
        }

        void onSnapshotCommitted(string filename)
        {
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onSnapshotCommitted: filename='{filename}'");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnSnapshotCommittedFromJava(filename));
        }

        void onSnapshotDeleted(string filename)
        {
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onSnapshotDeleted: filename='{filename}'");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnSnapshotDeletedFromJava(filename));
        }

        void onSavedGamesUIResult(string selectedFilename)
        {
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onSavedGamesUIResult: selectedFilename='{selectedFilename ?? "(null)"}'");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnSavedGamesUIResultFromJava(selectedFilename));
        }

        void onConflictDetected(string localSnapshotJson, string serverSnapshotJson, string localDataBase64, string serverDataBase64)
        {
            byte[] localData = FromBase64(localDataBase64);
            byte[] serverData = FromBase64(serverDataBase64);
            BizSimGamesLogger.Warning($"[CloudSave][JNI→Unity] onConflictDetected: localJson={localSnapshotJson?.Length ?? 0} chars, serverJson={serverSnapshotJson?.Length ?? 0} chars, localData={localData?.Length ?? 0} bytes, serverData={serverData?.Length ?? 0} bytes");
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] conflict local: {localSnapshotJson}");
            BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] conflict server: {serverSnapshotJson}");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnConflictDetectedFromJava(localSnapshotJson, serverSnapshotJson, localData, serverData));
        }

        void onCloudSaveError(int errorCode, string errorMessage, string filename)
        {
            // Code 3 (SnapshotNotFound) is an expected first-run answer, not a failure —
            // Error level here made every fresh account's restore probe look broken.
            if (errorCode == 3)
                BizSimGamesLogger.Info($"[CloudSave][JNI→Unity] onCloudSaveError: code={errorCode}, message='{errorMessage}', filename='{filename}'");
            else
                BizSimGamesLogger.Error($"[CloudSave][JNI→Unity] onCloudSaveError: code={errorCode}, message='{errorMessage}', filename='{filename}'");
            UnityMainThreadDispatcher.Enqueue(() => _controller.OnCloudSaveErrorFromJava(errorCode, errorMessage, filename));
        }

        static byte[] FromBase64(string value)
        {
            return value == null ? null : Convert.FromBase64String(value);
        }
    }
}
