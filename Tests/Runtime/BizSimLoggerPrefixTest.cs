using NUnit.Framework;
using BizSim.Google.Play.Games;

namespace BizSim.Google.Play.Games.Tests
{
    public class BizSimLoggerPrefixTest
    {
        [Test]
        public void Prefix_IsExactlyBizSimGames()
        {
            Assert.AreEqual("[BizSim.Games] ", BizSimGamesLogger.Prefix,
                "Per CROSS-PACKAGE-INVARIANTS.md §12.3 and google-play-bridge-pattern.md §6.1, the per-package log prefix is a hard convention. Expected '[BizSim.Games] ' with trailing space.");
        }
    }
}
