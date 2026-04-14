// Copyright (c) BizSim Game Studios. All rights reserved.

package com.bizsim.google.play.games.stats;

public interface IStatsCallback {
    void onStatsLoaded(String statsJson);
    void onStatsError(int errorCode, String errorMessage);
}
