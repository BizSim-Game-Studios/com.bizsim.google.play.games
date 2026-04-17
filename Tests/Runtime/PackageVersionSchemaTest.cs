using NUnit.Framework;
using BizSim.Google.Play.Games;

namespace BizSim.Google.Play.Games.Tests
{
    /// <summary>K8 PackageVersion schema drift guard (Plan G).</summary>
    public class PackageVersionSchemaTest
    {
        [Test]
        public void NativeSdkFields_ArePopulated()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkVersion));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkLabel));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkArtifactCoord));
        }

        [Test]
        public void NativeSdkArtifactCoord_EndsWithVersion()
        {
            Assert.IsTrue(PackageVersion.NativeSdkArtifactCoord.EndsWith(":" + PackageVersion.NativeSdkVersion));
        }

        [Test]
        public void NativeSdkFields_MatchExpectedGamesValues()
        {
            Assert.AreEqual("21.0.0", PackageVersion.NativeSdkVersion);
            Assert.AreEqual("Play Games Services v2", PackageVersion.NativeSdkLabel);
            Assert.AreEqual("com.google.android.gms:play-services-games-v2:21.0.0", PackageVersion.NativeSdkArtifactCoord);
        }

#pragma warning disable CS0618
        [Test]
        public void LegacyAlias_PgsV2_ResolvesToSameValue()
        {
            Assert.AreEqual(PackageVersion.NativeSdkVersion, PackageVersion.PgsV2SdkVersion);
        }
#pragma warning restore CS0618
    }
}
