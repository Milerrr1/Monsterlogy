#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using YG;
using YG.Insides;

namespace Monstrology.Editor
{
    public static class YandexDemoBuildTools
    {
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        private const string WebGLTemplate = "PROJECT:YandexGames";
        private const string BuildRelativePath =
            "Builds/YandexDemo/Monstrology";
        private const string PendingActionKey =
            "Monstrology.YandexDemo.PendingAction";
        private const string WebGLBridgeJslibPath =
            "Assets/Plugins/WebGL/MonstrologyWebGL.jslib";
        private const string WebGLPlatformBridgePath =
            "Assets/Monstrology/Scripts/WebGLPlatformBridge.cs";
        private const string WebGLBridgeError =
            "ERROR: Monstrology WebGL bridge is incomplete.\n" +
            "Safe-area JavaScript functions are not linked.";

        [InitializeOnLoadMethod]
        private static void ResumePendingAction()
        {
            string action = SessionState.GetString(PendingActionKey, "");
            if (string.IsNullOrEmpty(action) ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                return;
            }

            SessionState.EraseString(PendingActionKey);
            EditorApplication.delayCall += () =>
            {
                if (action == "build")
                {
                    BuildDemoZip();
                }
                else
                {
                    PrepareDemoBuild();
                }
            };
        }

        [MenuItem("Tools/Monstrology/Yandex/Prepare Demo Build")]
        public static void PrepareDemoBuild()
        {
            if (!EnsureWebGLTarget("prepare"))
            {
                return;
            }

            bool ready = PrepareDemoBuildInternal();
            Debug.Log(
                ready
                    ? "PASS: Yandex demo build settings prepared."
                    : "ERROR: Yandex demo build settings could not be prepared.");
        }

        [MenuItem("Tools/Monstrology/Yandex/Build Demo ZIP")]
        public static void BuildDemoZip()
        {
            if (!EnsureWebGLTarget("build"))
            {
                return;
            }

            if (!PrepareDemoBuildInternal())
            {
                Debug.LogError("YANDEX DEMO NOT READY");
                return;
            }

            string buildPath = GetBuildPath();
            if (!ClearBuildDirectory(buildPath))
            {
                Debug.LogError("YANDEX DEMO NOT READY");
                return;
            }

            DateTime buildStartedUtc = DateTime.UtcNow.AddSeconds(-2d);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = EditorUserBuildSettings.development
                    ? BuildOptions.Development
                    : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(
                    "ERROR: WebGL build failed: " +
                    report.summary.result);
                Debug.LogError("YANDEX DEMO NOT READY");
                return;
            }

            string archivePath = FindLatestArchive(buildStartedUtc);
            if (string.IsNullOrEmpty(archivePath))
            {
                Debug.LogError(
                    "ERROR: PluginYG2 archiving is enabled, but no new ZIP was found.");
                Debug.LogError("YANDEX DEMO NOT READY");
                return;
            }

            List<string> archiveErrors = ValidateArchive(archivePath);
            if (archiveErrors.Count > 0)
            {
                foreach (string error in archiveErrors)
                {
                    Debug.LogError("ERROR: " + error);
                }

                Debug.LogError("YANDEX DEMO NOT READY");
                return;
            }

            Debug.Log("PASS: Demo ZIP created: " + archivePath);
            ValidateDemoBuild();
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(archivePath);
            }
        }

        [MenuItem("Tools/Monstrology/Yandex/Validate Demo Build")]
        public static void ValidateDemoBuild()
        {
            List<ValidationEntry> entries = new List<ValidationEntry>();

            ValidatePlugin(entries);
            ValidateBridge(entries);
            ValidateBuildSettings(entries);
            ValidateArchiveState(entries);
            ValidateCompilation(entries);

            bool hasErrors = false;
            foreach (ValidationEntry entry in entries)
            {
                string message = entry.level + ": " + entry.message;
                if (entry.level == "ERROR")
                {
                    hasErrors = true;
                    Debug.LogError(message);
                }
                else if (entry.level == "WARNING")
                {
                    Debug.LogWarning(message);
                }
                else
                {
                    Debug.Log(message);
                }
            }

            Debug.Log(
                hasErrors
                    ? "YANDEX DEMO NOT READY"
                    : "YANDEX DEMO READY");
        }

        private static bool PrepareDemoBuildInternal()
        {
            string webGLBridgeError;
            if (!ValidateWebGLBridgeFiles(out webGLBridgeError))
            {
                Debug.LogError(webGLBridgeError);
                return false;
            }

            if (!File.Exists(ToAbsolutePath(MainScenePath)))
            {
                Debug.LogError(
                    "Main scene was not found: " + MainScenePath);
                return false;
            }

            if (PlatformSettings.currentPlatformBaseName != "YandexGames")
            {
                Debug.LogError(
                    "PluginYG2 platform must be YandexGames. Current: " +
                    PlatformSettings.currentPlatformBaseName);
                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.LogError(
                    "WebGL is not the active build target yet.");
                return false;
            }

            YG.EditorScr.DefineSymbols.UpdateDefineSymbols();
            PlayerSettings.WebGL.template = WebGLTemplate;
            EditorUserBuildSettings.development = true;

            InfoYG settings = YG2.infoYG;
            settings.Basic.archivingBuild = true;
            EditorUtility.SetDirty(settings);

            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>
                {
                    new EditorBuildSettingsScene(MainScenePath, true)
                };

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (string.Equals(
                        scene.path,
                        MainScenePath,
                        StringComparison.OrdinalIgnoreCase) ||
                    IsPluginTestScene(scene.path))
                {
                    continue;
                }

                scenes.Add(scene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Directory.CreateDirectory(GetBuildPath());
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool EnsureWebGLTarget(string pendingAction)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                return true;
            }

            SessionState.SetString(PendingActionKey, pendingAction);
            bool switching = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.WebGL,
                BuildTarget.WebGL);
            if (!switching)
            {
                SessionState.EraseString(PendingActionKey);
                Debug.LogError("Could not switch the project to WebGL.");
                return false;
            }

            Debug.Log(
                "Switching to WebGL. The Yandex demo action will resume " +
                "after script compilation.");
            return false;
        }

        private static void ValidatePlugin(List<ValidationEntry> entries)
        {
            string versionPath =
                ToAbsolutePath("Assets/PluginYourGames/Version.txt");
            Add(
                entries,
                File.Exists(versionPath),
                "PluginYG2 found" +
                (File.Exists(versionPath)
                    ? " (" + File.ReadAllText(versionPath).Trim() + ")"
                    : string.Empty));

            Add(
                entries,
                PlatformSettings.currentPlatformBaseName == "YandexGames",
                "PluginYG2 platform is Yandex Games");

            bool rewardedFound = Directory.Exists(
                ToAbsolutePath(
                    "Assets/PluginYourGames/Modules/RewardedAdv"));
            Add(entries, rewardedFound, "RewardedAdv module found");

            bool interstitialFound = Directory.Exists(
                ToAbsolutePath(
                    "Assets/PluginYourGames/Modules/InterstitialAdv"));
            Add(
                entries,
                interstitialFound,
                "InterstitialAdv module found",
                false);

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.WebGL);
            Add(
                entries,
                defines.Contains("YandexGamesPlatform_yg"),
                "YandexGamesPlatform_yg define is active");
            Add(
                entries,
                !rewardedFound || defines.Contains("RewardedAdv_yg"),
                "RewardedAdv_yg define is active");
            Add(
                entries,
                !interstitialFound || defines.Contains("InterstitialAdv_yg"),
                "InterstitialAdv_yg define is active",
                false);
        }

        private static void ValidateBridge(List<ValidationEntry> entries)
        {
            string webGLBridgeError;
            bool webGLBridgeValid =
                ValidateWebGLBridgeFiles(out webGLBridgeError);
            Add(
                entries,
                webGLBridgeValid,
                webGLBridgeValid
                    ? "Monstrology WebGL bridge exports and dependencies are linked"
                    : webGLBridgeError);

            Type bridgeType = typeof(YandexGamesBridge);
            string[] requiredMethods =
            {
                "IsSDKReady",
                "ShowRewardedAd",
                "ShowInterstitialAd",
                "NotifyGameReady",
                "NotifyGameplayStarted",
                "NotifyGameplayStopped",
                "SaveProgress",
                "SavePlatformProgress"
            };
            bool methodsFound = requiredMethods.All(method =>
                bridgeType.GetMethods(
                        BindingFlags.Instance | BindingFlags.Public)
                    .Any(info => info.Name == method));
            Add(
                entries,
                methodsFound,
                "YandexGamesBridge public API is connected");

            string bridgeSource = ReadProjectFile(
                "Assets/Monstrology/Scripts/YandexGamesBridge.cs");
            string uiSource = ReadProjectFile(
                "Assets/Monstrology/Scripts/UIManager.cs");
            Add(
                entries,
                bridgeSource.Contains(
                    "public const string EnergyRewardId = \"energy_3\";") &&
                uiSource.Contains(
                    "ShowRewardedAd(YandexGamesBridge.EnergyRewardId, () =>") &&
                uiSource.Contains(
                    "game.AddEnergy(rewardedEnergyAmount);") &&
                uiSource.Contains("bridge.SaveProgress();"),
                "energy_3 is granted and saved only inside the reward callback");
            Add(
                entries,
                uiSource.Contains("rewardEnergyButton.interactable = false") &&
                uiSource.Contains("RewardedRequestFinished"),
                "Rewarded energy button blocks repeated requests");
            Add(
                entries,
                bridgeSource.Contains("using YG;") &&
                bridgeSource.Contains("return YG2.isSDKEnabled;") &&
                bridgeSource.Contains("YG2.RewardedAdvShow(") &&
                bridgeSource.Contains("YG2.InterstitialAdvShow();"),
                "YandexGamesBridge is the PluginYG2 access point");
            Add(
                entries,
                bridgeSource.Contains(
                    "YG2.onOpenAnyAdv += HandleAdvertisementOpened;") &&
                bridgeSource.Contains(
                    "YG2.onOpenAnyAdv -= HandleAdvertisementOpened;") &&
                bridgeSource.Contains(
                    "YG2.onCloseAnyAdv += HandleAdvertisementClosed;") &&
                bridgeSource.Contains(
                    "YG2.onCloseAnyAdv -= HandleAdvertisementClosed;") &&
                bridgeSource.Contains(
                    "YG2.onPauseGame += HandlePluginPause;") &&
                bridgeSource.Contains(
                    "YG2.onPauseGame -= HandlePluginPause;"),
                "Advertisement pause events are subscribed and unsubscribed");
            Add(
                entries,
                !YG2.infoYG.Basic.autoGRA,
                "Auto GRA is disabled for manual post-loading Game Ready");
            Add(
                entries,
                CountOccurrences(
                    bridgeSource,
                    "YG2.GameReadyAPI();") == 1,
                "GameReady has a single guarded call site");
            Add(
                entries,
                CountOccurrences(
                    bridgeSource,
                    "YG2.GameplayStart();") == 1 &&
                CountOccurrences(
                    bridgeSource,
                    "YG2.GameplayStop();") == 1 &&
                !bridgeSource.Contains("private void Update()"),
                "Gameplay markup changes only on state transitions");
        }

        public static bool ValidateWebGLBridgeFiles(out string error)
        {
            error = WebGLBridgeError;
            string jslibPath = ToAbsolutePath(WebGLBridgeJslibPath);
            string csharpPath = ToAbsolutePath(WebGLPlatformBridgePath);
            if (!File.Exists(jslibPath) || !File.Exists(csharpPath))
            {
                return false;
            }

            string jslib = File.ReadAllText(jslibPath);
            string csharp = File.ReadAllText(csharpPath);
            string[] exports =
            {
                "Monstrology_IsMobileUserAgent",
                "Monstrology_RequestFullscreen",
                "Monstrology_GetSafeInsetLeft",
                "Monstrology_GetSafeInsetRight",
                "Monstrology_GetSafeInsetTop",
                "Monstrology_GetSafeInsetBottom"
            };
            string[] safeInsetExports =
            {
                "Monstrology_GetSafeInsetLeft",
                "Monstrology_GetSafeInsetRight",
                "Monstrology_GetSafeInsetTop",
                "Monstrology_GetSafeInsetBottom"
            };

            bool helperFound = Regex.IsMatch(
                jslib,
                @"\$MonstrologyGetSafeInset\s*:\s*function\s*\(");
            bool dependencyCountValid =
                CountOccurrences(jslib, "__deps") == 4;
            bool dependenciesFound = safeInsetExports.All(exportName =>
                Regex.IsMatch(
                    jslib,
                    Regex.Escape(exportName) +
                    @"__deps\s*:\s*\[\s*[""']" +
                    @"\$MonstrologyGetSafeInset[""']\s*\]"));
            bool exportsMatch = exports.All(exportName =>
                Regex.IsMatch(
                    jslib,
                    @"\b" + Regex.Escape(exportName) +
                    @"\s*:\s*function\s*\(") &&
                Regex.IsMatch(
                    csharp,
                    @"extern\s+(?:int|float)\s+" +
                    Regex.Escape(exportName) +
                    @"\s*\("));

            if (!helperFound ||
                !dependencyCountValid ||
                !dependenciesFound ||
                !exportsMatch)
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void ValidateBuildSettings(
            List<ValidationEntry> entries)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Add(
                entries,
                scenes.Length > 0 &&
                scenes[0].enabled &&
                scenes[0].path == MainScenePath,
                "Main scene is first in Scenes In Build");
            Add(
                entries,
                scenes.All(scene => !IsPluginTestScene(scene.path)),
                "PluginYG2 test scenes are absent from Scenes In Build");
            Add(
                entries,
                PlayerSettings.WebGL.template == WebGLTemplate,
                "Yandex Games WebGL template is selected");
            Add(
                entries,
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL,
                "WebGL is the active build target");
            Add(
                entries,
                Directory.Exists(GetBuildPath()),
                "Build output folder exists");
        }

        private static void ValidateArchiveState(
            List<ValidationEntry> entries)
        {
            string archivePath = FindLatestArchive(DateTime.MinValue);
            if (string.IsNullOrEmpty(archivePath))
            {
                Add(entries, false, "Demo ZIP exists");
                return;
            }

            Add(entries, true, "Demo ZIP exists: " + archivePath);
            List<string> errors = ValidateArchive(archivePath);
            Add(
                entries,
                errors.Count == 0,
                errors.Count == 0
                    ? "ZIP structure and file names are valid"
                    : string.Join("; ", errors));
        }

        private static void ValidateCompilation(
            List<ValidationEntry> entries)
        {
            Add(
                entries,
                !EditorApplication.isCompiling,
                "Unity script compilation is complete");
            Add(
                entries,
                CompilationPipeline.GetAssemblies().Any(assembly =>
                    assembly.name == "Assembly-CSharp"),
                "Runtime assembly is loaded");
        }

        private static List<string> ValidateArchive(string archivePath)
        {
            List<string> errors = new List<string>();
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                string[] names = archive.Entries
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .ToArray();
                if (!names.Contains("index.html"))
                {
                    errors.Add("index.html is not in the ZIP root");
                }

                if (!names.Any(name =>
                        name.StartsWith(
                            "Build/",
                            StringComparison.Ordinal)))
                {
                    errors.Add("Build/ is not in the ZIP root");
                }

                if (!names.Any(name =>
                        name.StartsWith(
                            "TemplateData/",
                            StringComparison.Ordinal)))
                {
                    errors.Add("TemplateData/ is not in the ZIP root");
                }

                string invalidName = names.FirstOrDefault(
                    HasInvalidArchiveName);
                if (!string.IsNullOrEmpty(invalidName))
                {
                    errors.Add(
                        "ZIP contains a space or non-ASCII name: " +
                        invalidName);
                }
            }

            return errors;
        }

        private static bool HasInvalidArchiveName(string path)
        {
            foreach (char character in path)
            {
                if (character == ' ' || character > 127)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FindLatestArchive(DateTime builtAfterUtc)
        {
            string directory = Path.GetDirectoryName(GetBuildPath());
            if (string.IsNullOrEmpty(directory) ||
                !Directory.Exists(directory))
            {
                return string.Empty;
            }

            return Directory.GetFiles(
                    directory,
                    "Monstrology*.zip",
                    SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(file => file.LastWriteTimeUtc >= builtAfterUtc)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .FirstOrDefault() ?? string.Empty;
        }

        private static bool ClearBuildDirectory(string buildPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                .FullName;
            string allowedRoot = Path.GetFullPath(
                Path.Combine(projectRoot, "Builds", "YandexDemo"));
            string resolvedBuildPath = Path.GetFullPath(buildPath);
            if (!resolvedBuildPath.StartsWith(
                    allowedRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError(
                    "Refusing to clear unexpected build path: " +
                    resolvedBuildPath);
                return false;
            }

            if (Directory.Exists(resolvedBuildPath))
            {
                Directory.Delete(resolvedBuildPath, true);
            }

            Directory.CreateDirectory(resolvedBuildPath);
            return true;
        }

        private static bool IsPluginTestScene(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.Replace('\\', '/').StartsWith(
                       "Assets/PluginYourGames/",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string GetBuildPath()
        {
            return ToAbsolutePath(BuildRelativePath);
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                .FullName;
            return Path.GetFullPath(
                Path.Combine(
                    projectRoot,
                    projectRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
        }

        private static string ReadProjectFile(string path)
        {
            string absolutePath = ToAbsolutePath(path);
            return File.Exists(absolutePath)
                ? File.ReadAllText(absolutePath)
                : string.Empty;
        }

        private static int CountOccurrences(
            string source,
            string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(
                       value,
                       index,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static void Add(
            List<ValidationEntry> entries,
            bool passed,
            string message,
            bool required = true)
        {
            entries.Add(
                new ValidationEntry
                {
                    level = passed
                        ? "PASS"
                        : required ? "ERROR" : "WARNING",
                    message = message
                });
        }

        private sealed class ValidationEntry
        {
            public string level;
            public string message;
        }
    }
}
#endif
