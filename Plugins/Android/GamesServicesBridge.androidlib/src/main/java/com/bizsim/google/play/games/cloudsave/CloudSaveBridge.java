// Copyright (c) BizSim Game Studios. All rights reserved.

package com.bizsim.google.play.games.cloudsave;

import android.app.Activity;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.Build;
import android.util.Log;

import androidx.activity.ComponentActivity;
import androidx.activity.result.ActivityResult;
import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;

import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.common.api.CommonStatusCodes;
import com.google.android.gms.games.GamesClientStatusCodes;
import com.google.android.gms.games.PlayGames;
import com.google.android.gms.games.SnapshotsClient;
import com.google.android.gms.games.snapshot.Snapshot;
import com.google.android.gms.games.snapshot.SnapshotMetadata;
import com.google.android.gms.games.snapshot.SnapshotMetadataChange;

import org.json.JSONObject;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;


public class CloudSaveBridge {
    private static final String TAG = "BizSimGames.CloudSave";
    private static final int CONFLICT_RESOLUTION_POLICY_MANUAL = -1;

    private final Activity activity;
    private final SnapshotsClient snapshotsClient;
    private final ExecutorService ioExecutor = Executors.newSingleThreadExecutor(r -> {
        Thread t = new Thread(r, "BizSimCloudSave-IO");
        t.setDaemon(true);
        return t;
    });
    private final ActivityResultLauncher<Intent> savedGamesLauncher;
    private ICloudSaveCallback callback;
    private ICloudSaveCallback savedGamesCallback;

    public CloudSaveBridge(Activity activity) {
        this.activity = activity;
        this.snapshotsClient = PlayGames.getSnapshotsClient(activity);

        this.savedGamesLauncher = ((ComponentActivity) activity)
                .getActivityResultRegistry()
                .register(
                        "bizsim_saved_games",
                        new ActivityResultContracts.StartActivityForResult(),
                        this::handleSavedGamesResult
                );

        Log.d(TAG, "CloudSaveBridge initialized with ActivityResultLauncher");
    }

    public void setCallback(ICloudSaveCallback callback) {
        this.callback = callback;
    }

    public void openSnapshot(String filename, boolean createIfNotFound) {
        Log.d(TAG, "Opening snapshot: " + filename);

        snapshotsClient.open(filename, createIfNotFound, CONFLICT_RESOLUTION_POLICY_MANUAL)
                .addOnSuccessListener(activity, dataOrConflict -> {
                    if (dataOrConflict.isConflict()) {
                        Log.w(TAG, "Conflict detected for: " + filename);
                        handleConflict(dataOrConflict.getConflict());
                    } else {
                        Snapshot snapshot = dataOrConflict.getData();
                        try {
                            String snapshotJson = serializeSnapshot(snapshot);
                            if (callback != null) {
                                callback.onSnapshotOpened(filename, snapshotJson, false);
                            }
                        } catch (Exception e) {
                            sendFailure("Failed to serialize snapshot: " + e.getMessage(), filename, e);
                        }
                    }
                })
                .addOnFailureListener(activity, e -> sendOpenFailure("Open", filename, e));
    }

    public void readSnapshot(String nativeHandle) {
        Log.d(TAG, "Read snapshot: " + nativeHandle);

        String[] parts = nativeHandle.split(":");
        if (parts.length < 2) {
            sendError(100, "Invalid snapshot handle", null);
            return;
        }

        String filename = parts[1];
        pendingConflictOp = "read";
        snapshotsClient.open(filename, false, CONFLICT_RESOLUTION_POLICY_MANUAL)
                .addOnSuccessListener(activity, dataOrConflict -> {
                    if (dataOrConflict.isConflict()) {
                        handleConflict(dataOrConflict.getConflict());
                    } else {
                        pendingConflictOp = null;
                        Snapshot snapshot = dataOrConflict.getData();
                        ioExecutor.execute(() -> {
                            try {
                                byte[] data = snapshot.getSnapshotContents().readFully();
                                if (callback != null) {
                                    callback.onSnapshotRead(filename, data);
                                }
                            } catch (Exception e) {
                                sendFailure("Read failed: " + e.getMessage(), filename, e);
                            }
                        });
                    }
                })
                .addOnFailureListener(activity, e -> sendOpenFailure("Read open", filename, e));
    }

    public void commitSnapshot(String nativeHandle, byte[] data, String description, long playedTimeMillis, byte[] coverImage) {
        Log.d(TAG, "Commit snapshot: " + nativeHandle + " (" + data.length + " bytes)");

        String[] parts = nativeHandle.split(":");
        if (parts.length < 2) {
            sendError(100, "Invalid snapshot handle", null);
            return;
        }

        String filename = parts[1];
        snapshotsClient.open(filename, true, CONFLICT_RESOLUTION_POLICY_MANUAL)
                .addOnSuccessListener(activity, dataOrConflict -> {
                    if (dataOrConflict.isConflict()) {
                        handleConflict(dataOrConflict.getConflict());
                    } else {
                        Snapshot snapshot = dataOrConflict.getData();
                        ioExecutor.execute(() -> {
                            try {
                                snapshot.getSnapshotContents().writeBytes(data);

                                SnapshotMetadataChange.Builder metaBuilder = new SnapshotMetadataChange.Builder()
                                        .setPlayedTimeMillis(playedTimeMillis);

                                if (description != null && !description.isEmpty()) {
                                    metaBuilder.setDescription(description);
                                }

                                if (coverImage != null && coverImage.length > 0) {
                                    try {
                                        Bitmap bitmap = decodeCoverImageSafe(coverImage);
                                        if (bitmap != null) {
                                            metaBuilder.setCoverImage(bitmap);
                                        }
                                    } catch (OutOfMemoryError e) {
                                        Log.e(TAG,
                                            "Cover image decode OOM (" + coverImage.length + " bytes). " +
                                            "Use max 640x360 resolution. Save continues without cover image.", e);
                                    }
                                }

                                SnapshotMetadataChange metaChange = metaBuilder.build();

                                snapshotsClient.commitAndClose(snapshot, metaChange)
                                        .addOnSuccessListener(activity, metadata -> {
                                            Log.d(TAG, "Snapshot committed: " + filename);
                                            if (callback != null) {
                                                callback.onSnapshotCommitted(filename);
                                            }
                                        })
                                        .addOnFailureListener(activity, e -> {
                                            sendFailure("Commit failed: " + e.getMessage(), filename, e);
                                        });

                            } catch (Exception e) {
                                sendFailure("Write failed: " + e.getMessage(), filename, e);
                            }
                        });
                    }
                })
                .addOnFailureListener(activity, e -> {
                    Log.e(TAG, "Failed to open snapshot for commit: " + filename, e);
                    sendFailure("Commit open failed: " + e.getMessage(), filename, e);
                });
    }

    public void deleteSnapshot(String filename) {
        Log.d(TAG, "Delete snapshot: " + filename);

        snapshotsClient.open(filename, false, CONFLICT_RESOLUTION_POLICY_MANUAL)
                .addOnSuccessListener(activity, dataOrConflict -> {
                    if (dataOrConflict.isConflict()) {
                        Log.w(TAG, "Conflict on delete open for: " + filename);
                        handleConflict(dataOrConflict.getConflict());
                    } else {
                        SnapshotMetadata metadata = dataOrConflict.getData().getMetadata();
                        snapshotsClient.delete(metadata)
                                .addOnSuccessListener(activity, deleteResult -> {
                                    Log.d(TAG, "Snapshot deleted: " + filename);
                                    if (callback != null) {
                                        callback.onSnapshotDeleted(filename);
                                    }
                                })
                                .addOnFailureListener(activity, e -> {
                                    sendFailure("Delete failed: " + e.getMessage(), filename, e);
                                });
                    }
                })
                .addOnFailureListener(activity, e -> sendOpenFailure("Delete open", filename, e));
    }

    public void showSavedGamesUI(String title, boolean allowAddButton, boolean allowDelete, int maxSnapshots) {
        Log.d(TAG, "Show saved games UI");

        savedGamesCallback = callback;

        snapshotsClient.getSelectSnapshotIntent(title, allowAddButton, allowDelete, maxSnapshots)
                .addOnSuccessListener(activity, intent -> {
                    if (intent != null) {
                        savedGamesLauncher.launch(intent);
                    } else {
                        Log.w(TAG, "getSelectSnapshotIntent returned null intent — UI not available");
                        ICloudSaveCallback cb = savedGamesCallback;
                        savedGamesCallback = null;
                        if (cb != null) {
                            cb.onSavedGamesUIResult(null);
                        }
                    }
                })
                .addOnFailureListener(activity, e -> {
                    savedGamesCallback = null;
                    sendFailure("UI failed: " + e.getMessage(), null, e);
                });
    }

    private void handleSavedGamesResult(ActivityResult result) {
        ICloudSaveCallback cb = savedGamesCallback;
        savedGamesCallback = null;

        if (cb == null) {
            Log.w(TAG, "Saved games result received but no callback registered");
            return;
        }

        try {
            Intent data = result.getData();
            if (result.getResultCode() == Activity.RESULT_OK && data != null) {
                if (data.hasExtra(SnapshotsClient.EXTRA_SNAPSHOT_METADATA)) {
                    SnapshotMetadata metadata;
                    if (Build.VERSION.SDK_INT >= 33) {
                        metadata = data.getParcelableExtra(
                            SnapshotsClient.EXTRA_SNAPSHOT_METADATA, SnapshotMetadata.class);
                    } else {
                        metadata = data.getParcelableExtra(
                            SnapshotsClient.EXTRA_SNAPSHOT_METADATA);
                    }
                    if (metadata != null) {
                        cb.onSavedGamesUIResult(metadata.getUniqueName());
                    } else {
                        cb.onSavedGamesUIResult(null);
                    }
                } else if (data.hasExtra(SnapshotsClient.EXTRA_SNAPSHOT_NEW)) {
                    cb.onSavedGamesUIResult("__NEW__");
                } else {
                    cb.onSavedGamesUIResult(null);
                }
            } else {
                cb.onSavedGamesUIResult(null);
            }
        } catch (Exception e) {
            Log.e(TAG, "handleSavedGamesResult error", e);
            cb.onSavedGamesUIResult(null);
        }
    }

    private volatile SnapshotsClient.SnapshotConflict lastConflict;

    // Which call raised the conflict. resolveConflict always answered with onSnapshotOpened,
    // which on the C# side completes _openTcs and nothing else - so a conflict raised during
    // a READ left _readTcs pending until its 30s JNI timeout fired. That is the shape behind
    // the error_type=Canceled / error_type=Timeout restores with no preceding attempt.
    private volatile String pendingConflictOp;

    private void handleConflict(SnapshotsClient.SnapshotConflict conflict) {
        this.lastConflict = conflict;

        ioExecutor.execute(() -> {
            try {
                Snapshot conflictSnapshot = conflict.getConflictingSnapshot();
                Snapshot serverSnapshot = conflict.getSnapshot();

                String localJson = serializeSnapshot(conflictSnapshot);
                String serverJson = serializeSnapshot(serverSnapshot);

                byte[] localData = conflictSnapshot.getSnapshotContents().readFully();
                byte[] serverData = serverSnapshot.getSnapshotContents().readFully();

                if (callback != null) {
                    callback.onConflictDetected(localJson, serverJson, localData, serverData);
                }
            } catch (Exception e) {
                Log.e(TAG, "Failed to handle conflict", e);
                sendFailure("Conflict handling failed: " + e.getMessage(), null, e);
            }
        });
    }

    public void resolveConflict(String resolution, String nativeHandle) {
        Log.d(TAG, "Resolve conflict: " + resolution);

        if (lastConflict == null) {
            sendError(100, "No conflict to resolve", null);
            return;
        }

        String conflictId = lastConflict.getConflictId();
        Snapshot resolvedSnapshot;

        if ("UseLocal".equalsIgnoreCase(resolution) || "Local".equalsIgnoreCase(resolution)) {
            resolvedSnapshot = lastConflict.getConflictingSnapshot();
        } else {
            resolvedSnapshot = lastConflict.getSnapshot();
        }

        snapshotsClient.resolveConflict(conflictId, resolvedSnapshot)
                .addOnSuccessListener(activity, dataOrConflict -> {
                    lastConflict = null;
                    if (dataOrConflict.isConflict()) {
                        Log.w(TAG, "Recursive conflict detected after resolution");
                        handleConflict(dataOrConflict.getConflict());
                    } else {
                        Snapshot snapshot = dataOrConflict.getData();
                        String op = pendingConflictOp;
                        pendingConflictOp = null;
                        try {
                            String filename = snapshot.getMetadata().getUniqueName();

                            // A read that hit a conflict wants the CONTENTS, not a handle.
                            // Answering with onSnapshotOpened completes the open task and
                            // leaves the read waiting for a callback that never comes.
                            if ("read".equals(op)) {
                                ioExecutor.execute(() -> {
                                    try {
                                        byte[] data = snapshot.getSnapshotContents().readFully();
                                        if (callback != null) {
                                            callback.onSnapshotRead(filename, data);
                                        }
                                    } catch (Exception e) {
                                        sendFailure("Post-resolve read failed: " + e.getMessage(), filename, e);
                                    }
                                });
                                return;
                            }

                            String snapshotJson = serializeSnapshot(snapshot);
                            if (callback != null) {
                                callback.onSnapshotOpened(filename, snapshotJson, false);
                            }
                        } catch (Exception e) {
                            sendFailure("Post-resolve serialize failed: " + e.getMessage(), null, e);
                        }
                    }
                })
                .addOnFailureListener(activity, e -> {
                    Log.e(TAG, "Failed to resolve conflict", e);
                    lastConflict = null;
                    sendFailure("Resolve failed: " + e.getMessage(), null, e);
                });
    }

    private static final int MAX_COVER_WIDTH = 640;
    private static final int MAX_COVER_HEIGHT = 360;

    private Bitmap decodeCoverImageSafe(byte[] coverImage) {
        BitmapFactory.Options boundsOptions = new BitmapFactory.Options();
        boundsOptions.inJustDecodeBounds = true;
        BitmapFactory.decodeByteArray(coverImage, 0, coverImage.length, boundsOptions);

        int width = boundsOptions.outWidth;
        int height = boundsOptions.outHeight;

        if (width <= 0 || height <= 0) {
            Log.e(TAG, "Cover image has invalid dimensions (" + width + "x" + height + ")");
            return null;
        }

        int inSampleSize = 1;
        if (width > MAX_COVER_WIDTH || height > MAX_COVER_HEIGHT) {
            int halfWidth = width / 2;
            int halfHeight = height / 2;
            while ((halfWidth / inSampleSize) >= MAX_COVER_WIDTH
                    && (halfHeight / inSampleSize) >= MAX_COVER_HEIGHT) {
                inSampleSize *= 2;
            }
            Log.w(TAG, "Cover image " + width + "x" + height +
                    " exceeds " + MAX_COVER_WIDTH + "x" + MAX_COVER_HEIGHT +
                    ", downsampling with inSampleSize=" + inSampleSize);
        }

        BitmapFactory.Options decodeOptions = new BitmapFactory.Options();
        decodeOptions.inSampleSize = inSampleSize;
        return BitmapFactory.decodeByteArray(coverImage, 0, coverImage.length, decodeOptions);
    }

    private String serializeSnapshot(Snapshot snapshot) throws Exception {
        SnapshotMetadata metadata = snapshot.getMetadata();
        JSONObject obj = new JSONObject();

        obj.put("filename", metadata.getUniqueName());
        obj.put("nativeHandle", "snapshot:" + metadata.getUniqueName());
        obj.put("lastModifiedTimestamp", metadata.getLastModifiedTimestamp());
        obj.put("playedTimeMillis", metadata.getPlayedTime());
        obj.put("description", metadata.getDescription());

        android.net.Uri coverUri = metadata.getCoverImageUri();
        if (coverUri != null) {
            obj.put("coverImageUri", coverUri.toString());
        }

        return obj.toString();
    }

    // SNAPSHOT_NOT_FOUND with createIfNotFound=false is an expected "no save exists"
    // answer, not an internal error: it maps to error code 3 (SnapshotNotFound in the
    // C# contract) at Info level. Everything else stays code 100. commitSnapshot never
    // routes here because it opens with createIfNotFound=true and cannot 404.
    private void sendOpenFailure(String operation, String filename, Exception e) {
        int status = (e instanceof ApiException) ? ((ApiException) e).getStatusCode() : 0;
        if (status == GamesClientStatusCodes.SNAPSHOT_NOT_FOUND) {
            Log.i(TAG, operation + ": snapshot not found: " + filename);
            sendError(3, "Snapshot not found", filename);
        } else {
            Log.e(TAG, operation + " failed for: " + filename, e);
            sendFailure(operation + " failed: " + e.getMessage(), filename, e);
        }
    }

    /**
     * Maps a Play Games failure onto the typed vocabulary the C# side already declares in
     * GamesCloudSaveError.cs (-1 ApiNotAvailable, 1 UserNotAuthenticated, 2 NetworkError,
     * 3 SnapshotNotFound, 4 ConflictTimeout, 5 DataTooLarge, 100 InternalError).
     *
     * Every failure used to leave here as 100, so that vocabulary was never produced on a real
     * device and the client could not tell a signed-out session from a dead network. It told the
     * player to check their internet either way. Measured 2026-08-21: Russia produced 82% of the
     * world's cloud-save errors in the clean window and every one of those players got the
     * connectivity message.
     *
     * Constants verified against the shipped jars rather than from memory:
     * play-services-basement-18.10.0 (CommonStatusCodes) and play-services-games-v2-21.0.0
     * (GamesClientStatusCodes). Anything not listed stays 100 deliberately - a wrong
     * classification is worse than an honest "internal error", and the C# contract maps an
     * unknown code to Unknown rather than to a wrong branch.
     */
    private int classify(Exception e) {
        if (!(e instanceof ApiException)) {
            return 100;
        }

        switch (((ApiException) e).getStatusCode()) {
            case GamesClientStatusCodes.SNAPSHOT_NOT_FOUND:
                return 3;

            case CommonStatusCodes.SIGN_IN_REQUIRED:
            case CommonStatusCodes.INVALID_ACCOUNT:
            case CommonStatusCodes.RESOLUTION_REQUIRED:
            case GamesClientStatusCodes.CONSENT_REQUIRED:
                return 1;

            case CommonStatusCodes.NETWORK_ERROR:
            case GamesClientStatusCodes.NETWORK_ERROR_NO_DATA:
            case GamesClientStatusCodes.NETWORK_ERROR_OPERATION_FAILED:
                return 2;

            case CommonStatusCodes.API_NOT_CONNECTED:
            case CommonStatusCodes.SERVICE_VERSION_UPDATE_REQUIRED:
            case CommonStatusCodes.SERVICE_DISABLED:
            case CommonStatusCodes.CONNECTION_SUSPENDED_DURING_CALL:
            case CommonStatusCodes.RECONNECTION_TIMED_OUT:
            case CommonStatusCodes.RECONNECTION_TIMED_OUT_DURING_UPDATE:
                return -1;

            case CommonStatusCodes.TIMEOUT:
            case GamesClientStatusCodes.OPERATION_IN_FLIGHT:
            case GamesClientStatusCodes.SNAPSHOT_CONFLICT_MISSING:
                return 4;

            default:
                return 100;
        }
    }

    /**
     * The exception-carrying counterpart of sendError. Use this wherever a Throwable is in scope;
     * sendError(100, ...) is now reserved for the three internal states that have no exception
     * behind them (an invalid snapshot handle, and resolving a conflict that is not there).
     */
    private void sendFailure(String errorMessage, String filename, Exception e) {
        sendError(classify(e), errorMessage, filename);
    }

    private void sendError(int errorCode, String errorMessage, String filename) {
        if (callback != null) {
            callback.onCloudSaveError(errorCode, errorMessage, filename);
        }
    }

    public void shutdown() {
        savedGamesLauncher.unregister();
        ioExecutor.shutdownNow();
        lastConflict = null;
        savedGamesCallback = null;
        callback = null;
    }
}
