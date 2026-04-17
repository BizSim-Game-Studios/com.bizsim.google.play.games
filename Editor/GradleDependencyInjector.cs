// Copyright (c) BizSim Game Studios. All rights reserved.

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.Google.Play.Games.Editor
{
    /// <summary>
    /// Legacy Gradle-template injector for Play Games Services v2.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Deprecated as of 1.3.0.</b> Dependency resolution now runs through the External
    /// Dependency Manager for Unity (EDM4U), declared in <c>Editor/Dependencies.xml</c> and
    /// pinned via <c>package.json</c> (<c>com.google.external-dependency-manager: 1.2.187</c>).
    /// Consumers should run <b>Assets → External Dependency Manager → Android Resolver →
    /// Force Resolve</b> after install; that is the supported path from 1.3.0 onward.
    /// </para>
    /// <para>
    /// This class is kept for consumers who cannot install EDM4U (air-gapped environments,
    /// legacy CI pipelines). It still injects <c>com.google.android.gms:play-services-games-v2:21.0.0</c>
    /// into <c>mainTemplate.gradle</c> at build time. When EDM4U is also present, both paths
    /// are idempotent (the Gradle injector short-circuits when the dependency string is already
    /// present in the template), so there is no double-include risk.
    /// </para>
    /// <para>
    /// Scheduled for removal in <b>2.0.0</b> per ADR-009.
    /// </para>
    /// </remarks>
    [System.Obsolete("Gradle-template injection superseded by EDM4U (Editor/Dependencies.xml). Kept as fallback for air-gapped environments. Removed in 2.0.0 per ADR-009.", error: false)]
    public class GradleDependencyInjector : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string DependencyLine = "    implementation 'com.google.android.gms:play-services-games-v2:21.0.0'";
        private const string GoogleRepoLine = "        google()";
        private const string MavenCentralLine = "        mavenCentral()";

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            Debug.Log("[GamesServices] Injecting Gradle dependencies...");

            string pluginsDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/Plugins/Android");
            string mainPath = Path.Combine(pluginsDir, "mainTemplate.gradle");
            string settingsPath = Path.Combine(pluginsDir, "settingsTemplate.gradle");

            // Ensure templates exist (copy from Unity defaults if missing)
            EnsureGradleTemplate("mainTemplate.gradle", pluginsDir);

            if (IsUnity2022OrNewer())
            {
                EnsureGradleTemplate("settingsTemplate.gradle", pluginsDir);
            }

            // Inject dependencies
            if (File.Exists(settingsPath))
            {
                InjectRepositoriesToSettings(settingsPath);
                InjectDependenciesToMain(mainPath);
                Debug.Log("[GamesServices] Dependencies injected (settingsTemplate + mainTemplate)");
            }
            else
            {
                InjectDependencies(mainPath);
                Debug.Log("[GamesServices] Dependencies injected (mainTemplate only)");
            }
        }

        /// <summary>
        /// Ensures a Gradle template file exists. If missing, copies from Unity's defaults.
        /// </summary>
        private void EnsureGradleTemplate(string templateFileName, string targetDir)
        {
            string targetPath = Path.Combine(targetDir, templateFileName);

            // Already exists — nothing to do
            if (File.Exists(targetPath))
                return;

            // Find Unity's default template
            string unityDefaultDir = Path.Combine(
                EditorApplication.applicationContentsPath,
                "PlaybackEngines/AndroidPlayer/Tools/GradleTemplates");
            string defaultPath = Path.Combine(unityDefaultDir, templateFileName);

            if (!File.Exists(defaultPath))
            {
                Debug.LogWarning($"[GamesServices] Unity default template not found: {defaultPath}");
                return;
            }

            // Create target directory if needed
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Copy default template
            File.Copy(defaultPath, targetPath);
            Debug.Log($"[GamesServices] Created {templateFileName} from Unity defaults");

            // Sync the EditorUserBuildSettings flag
            if (templateFileName == "mainTemplate.gradle")
                EditorUserBuildSettings.SetPlatformSettings("Android", "customMainGradleTemplate", "true");
            else if (templateFileName == "settingsTemplate.gradle")
                EditorUserBuildSettings.SetPlatformSettings("Android", "customSettingsGradleTemplate", "true");

            // Refresh so Unity picks up the new file
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private void InjectRepositoriesToSettings(string path)
        {
            if (!File.Exists(path)) return;

            string content = File.ReadAllText(path);

            if (content.Contains("google()")) return;

            if (content.Contains("dependencyResolutionManagement"))
            {
                content = content.Replace(
                    "dependencyResolutionManagement {",
                    "dependencyResolutionManagement {\n    repositories {\n" +
                    GoogleRepoLine + "\n" +
                    MavenCentralLine + "\n" +
                    "    }"
                );
            }
            else
            {
                content += "\ndependencyResolutionManagement {\n    repositories {\n" +
                           GoogleRepoLine + "\n" +
                           MavenCentralLine + "\n" +
                           "    }\n}\n";
            }

            File.WriteAllText(path, content);
        }

        private void InjectDependenciesToMain(string path)
        {
            if (!File.Exists(path)) return;

            string content = File.ReadAllText(path);

            if (content.Contains("play-services-games-v2")) return;

            if (content.Contains("dependencies {"))
            {
                content = content.Replace(
                    "dependencies {",
                    "dependencies {\n" + DependencyLine
                );
            }
            else
            {
                content += "\ndependencies {\n" + DependencyLine + "\n}\n";
            }

            File.WriteAllText(path, content);
        }

        private void InjectDependencies(string path)
        {
            if (!File.Exists(path)) return;

            string content = File.ReadAllText(path);

            // Inject repositories
            if (!content.Contains("google()") && content.Contains("allprojects {"))
            {
                content = content.Replace(
                    "allprojects {",
                    "allprojects {\n    repositories {\n" +
                    GoogleRepoLine + "\n" +
                    MavenCentralLine + "\n" +
                    "    }"
                );
            }

            // Inject dependency
            if (!content.Contains("play-services-games-v2") && content.Contains("dependencies {"))
            {
                content = content.Replace(
                    "dependencies {",
                    "dependencies {\n" + DependencyLine
                );
            }

            File.WriteAllText(path, content);
        }

        private bool IsUnity2022OrNewer()
        {
            return Application.unityVersion.StartsWith("2022") ||
                   Application.unityVersion.StartsWith("2023") ||
                   Application.unityVersion.StartsWith("6000");
        }
    }
}
