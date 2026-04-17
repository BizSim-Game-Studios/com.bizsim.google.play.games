// Copyright (c) BizSim Game Studios. All rights reserved.

namespace BizSim.Google.Play.Games
{
    /// <summary>
    /// Package version information.
    /// Auto-updated during release process.
    /// </summary>
    internal static class PackageVersion
    {
        public const string Current = "1.3.0";
        public const string ReleaseDate = "2026-04-17";

        // === Canonical K8 fields (Plan G) ===
        public const string NativeSdkVersion       = "21.0.0";
        public const string NativeSdkLabel         = "Play Games Services v2";
        public const string NativeSdkArtifactCoord = "com.google.android.gms:play-services-games-v2:21.0.0";

        // === Legacy alias (deprecated; removed in 2.0.0 per ADR-009) ===
        [System.Obsolete("Use NativeSdkVersion. Removed in 2.0.0 per ADR-009.", error: false)]
        public const string PgsV2SdkVersion = NativeSdkVersion;
    }
}
