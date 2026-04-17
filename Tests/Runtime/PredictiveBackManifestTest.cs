using System.IO;
using NUnit.Framework;

namespace BizSim.Google.Play.Games.Tests
{
    /// <summary>
    /// C5.2 drift guard (Plan E). For games, expected value is "true" — all
    /// PGS v2 dialogs are system-managed and handle predictive-back internally.
    /// </summary>
    public class PredictiveBackManifestTest
    {
        private const string ManifestPath =
            "Packages/com.bizsim.google.play.games/Plugins/Android/GamesServicesBridge.androidlib/src/main/AndroidManifest.xml";

        private const string FallbackPath =
            "Plugins/Android/GamesServicesBridge.androidlib/src/main/AndroidManifest.xml";

        private static string ReadManifest()
        {
            if (File.Exists(ManifestPath)) return File.ReadAllText(ManifestPath);
            if (File.Exists(FallbackPath)) return File.ReadAllText(FallbackPath);
            Assert.Inconclusive("Manifest not found at " + ManifestPath + " or " + FallbackPath);
            return null;
        }

        [Test]
        public void Manifest_DeclaresPredictiveBackCallback_True()
        {
            var xml = ReadManifest();
            Assert.IsTrue(xml.Contains("enableOnBackInvokedCallback=\"true\""),
                "Per C5.2, games .androidlib manifest must declare " +
                "android:enableOnBackInvokedCallback=\"true\".");
        }
    }
}
