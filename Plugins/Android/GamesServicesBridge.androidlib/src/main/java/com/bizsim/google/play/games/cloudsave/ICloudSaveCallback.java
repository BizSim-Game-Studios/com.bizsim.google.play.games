// Copyright (c) BizSim Game Studios. All rights reserved.

package com.bizsim.google.play.games.cloudsave;

public interface ICloudSaveCallback {
    void onSnapshotOpened(String filename, String snapshotJson, boolean hasConflict);
    void onSnapshotRead(String filename, String dataBase64);
    void onSnapshotCommitted(String filename);
    void onSnapshotDeleted(String filename);
    void onSavedGamesUIResult(String selectedFilename);
    void onConflictDetected(String localSnapshotJson, String serverSnapshotJson, String localDataBase64, String serverDataBase64);
    void onCloudSaveError(int errorCode, String errorMessage, String filename);
}
