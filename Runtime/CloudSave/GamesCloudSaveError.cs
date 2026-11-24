// Copyright (c) BizSim Game Studios. All rights reserved.

using System;

namespace BizSim.Google.Play.Games
{
    [Serializable]
    public class GamesCloudSaveError
    {
        public int errorCode;
        public string errorMessage;
        public string filename;

        public CloudSaveErrorType Type => errorCode switch
        {
            -1 => CloudSaveErrorType.ApiNotAvailable,
            1 => CloudSaveErrorType.UserNotAuthenticated,
            2 => CloudSaveErrorType.NetworkError,
            3 => CloudSaveErrorType.SnapshotNotFound,
            4 => CloudSaveErrorType.ConflictTimeout,
            5 => CloudSaveErrorType.DataTooLarge,
            6 => CloudSaveErrorType.Conflict,
            100 => CloudSaveErrorType.InternalError,
            _ => CloudSaveErrorType.Unknown
        };

        public GamesCloudSaveError(int code, string message, string filename = null)
        {
            this.errorCode = code;
            this.errorMessage = message;
            this.filename = filename;
        }

        public override string ToString() =>
            $"[CloudSaveError {errorCode}] {Type}: {errorMessage}" +
            (string.IsNullOrEmpty(filename) ? "" : $" (File: {filename})");
    }

    /// <summary>
    /// Cloud save error codes mapped from Google Play Games SnapshotsClient.
    /// </summary>
    public enum CloudSaveErrorType
    {
        /// <summary>Unclassified error not matching any known code.</summary>
        Unknown = 0,

        /// <summary>Google Play Games API is not available on this device (code -1).</summary>
        ApiNotAvailable = -1,

        /// <summary>User is not signed in. Call GamesServicesManager.Auth.SignInAsync() first (code 1).</summary>
        UserNotAuthenticated = 1,

        /// <summary>Network error — device is offline or Google servers unreachable (code 2).</summary>
        NetworkError = 2,

        /// <summary>Snapshot filename does not exist and createIfNotFound was false (code 3).</summary>
        SnapshotNotFound = 3,

        /// <summary>Conflict resolution timed out — player did not choose within deadline (code 4).</summary>
        ConflictTimeout = 4,

        /// <summary>Snapshot data exceeds 3MB limit per save slot (code 5).</summary>
        DataTooLarge = 5,

        /// <summary>A commit or delete met a conflict; the server copy was kept and nothing was written (code 6).</summary>
        Conflict = 6,

        /// <summary>Internal error in Google Play Games SDK (code 100).</summary>
        InternalError = 100
    }

    public class GamesCloudSaveException : GamesException
    {
        public GamesCloudSaveError Error { get; }

        public GamesCloudSaveException(GamesCloudSaveError error)
            : base(error?.errorCode ?? 0, $"Cloud save failed: {error}")
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }
    }

    /// <summary>
    /// A commit or delete met a save conflict. The conflict is resolved with the server copy and nothing was written or
    /// deleted. Show the player the server save, then save again only if they choose to.
    /// </summary>
    public sealed class GamesCloudSaveConflictException : GamesCloudSaveException
    {
        /// <summary>Handle and metadata (time, played time, description) of the server copy that was kept.</summary>
        public SnapshotHandle ServerSnapshot { get; }

        public CloudSaveOperation Operation { get; }

        public GamesCloudSaveConflictException(GamesCloudSaveError error, CloudSaveOperation operation, SnapshotHandle serverSnapshot)
            : base(error)
        {
            Operation = operation;
            ServerSnapshot = serverSnapshot;
        }
    }
}
