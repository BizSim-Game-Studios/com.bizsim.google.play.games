// Copyright (c) BizSim Game Studios. All rights reserved.

using UnityEditor;

namespace BizSim.Google.Play.Games.Editor
{
    /// <summary>
    /// Registers the BIZSIM_GAMES_INSTALLED scripting define on domain reload.
    /// </summary>
    [InitializeOnLoad]
    static class GamesEditorInit
    {
        static GamesEditorInit()
        {
            BizSim.Google.Play.Editor.Core.BizSimDefineManager.AddDefine(
                "BIZSIM_GAMES_INSTALLED",
                BizSim.Google.Play.Editor.Core.BizSimDefineManager.GetRelevantPlatforms());
        }
    }
}
