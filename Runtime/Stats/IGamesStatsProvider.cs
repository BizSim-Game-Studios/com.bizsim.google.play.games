// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.Games
{
    public interface IGamesStatsProvider
    {
        /// <summary>False on players with no backing service (iOS, standalone, WebGL): every call is a no-op with empty results.</summary>
        bool IsSupported => true;

        Task<GamesPlayerStats> LoadPlayerStatsAsync(bool forceReload = false, CancellationToken ct = default);
        event Action<GamesPlayerStats> OnStatsLoaded;
        event Action<GamesStatsError> OnStatsError;
    }
}
