// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using System.Threading.Tasks;

namespace BizSim.Google.Play.Games
{
    /// <summary>
    /// Represents a conflict between local and cloud saves.
    /// Game must choose a resolution strategy via ResolveAsync.
    /// </summary>
    public class SavedGameConflict
    {
        /// <summary>
        /// The call that met the conflict.
        /// </summary>
        public CloudSaveOperation operation;

        /// <summary>
        /// Local snapshot (current device data).
        /// </summary>
        public SnapshotHandle localSnapshot;

        /// <summary>
        /// Server snapshot (cloud data).
        /// </summary>
        public SnapshotHandle serverSnapshot;

        /// <summary>
        /// Local save data (byte array).
        /// </summary>
        public byte[] localData;

        /// <summary>
        /// Server save data (byte array).
        /// </summary>
        public byte[] serverData;

        /// <summary>
        /// Resolves the conflict by choosing a resolution strategy.
        /// Game must call this from OnConflictDetected event handler.
        /// </summary>
        public Func<ConflictResolution, Task> ResolveAsync { get; internal set; }
    }

    /// <summary>
    /// Cloud save call that can meet a conflict.
    /// </summary>
    public enum CloudSaveOperation
    {
        Open = 0,
        Read = 1,
        Commit = 2,
        Delete = 3
    }

    /// <summary>
    /// Conflict resolution strategy. Open and read go on with the chosen save. A commit or delete keeps the server copy and
    /// fails with <see cref="GamesCloudSaveConflictException"/>, whatever is chosen, except a commit answered with
    /// <see cref="UseLocalAndWrite"/>.
    /// </summary>
    public enum ConflictResolution
    {
        /// <summary>
        /// Use local device data (discard cloud data).
        /// </summary>
        UseLocal = 0,

        /// <summary>
        /// Use cloud data (discard local changes).
        /// </summary>
        UseServer = 1,

        /// <summary>
        /// Merge both (game-specific logic required).
        /// Not implemented in this version - falls back to UseLocal.
        /// </summary>
        UseManual = 2,

        /// <summary>
        /// Commit only: write this save over the server copy. Choose it only after the player decided so.
        /// On an open or read it means UseLocal; a delete fails as with any other answer.
        /// </summary>
        UseLocalAndWrite = 3
    }
}
