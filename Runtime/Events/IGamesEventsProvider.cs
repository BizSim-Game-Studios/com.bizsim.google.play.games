// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.Games
{
    public interface IGamesEventsProvider
    {
        /// <summary>False on players with no backing service (iOS, standalone, WebGL): every call is a no-op with empty results.</summary>
        bool IsSupported => true;

        Task IncrementEventAsync(string eventId, int steps = 1, CancellationToken ct = default);
        Task<GamesEvent[]> LoadEventsAsync(CancellationToken ct = default);
        Task<GamesEvent> LoadEventAsync(string eventId, CancellationToken ct = default);
        event Action<GamesEventsError> OnEventsError;
    }
}
