#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using YG;

namespace Monstrology.Editor
{
    public static class FinalDemoUpdateTools
    {
        public const string BalanceAssetPath =
            "Assets/Monstrology/Resources/FirstHourBalanceConfig.asset";
        private const string WebGLTemplate = "PROJECT:YandexGames";
        private const string TestBuildVersion = "1.0.5";

        [MenuItem("Tools/Monstrology/Apply Final Demo Update")]
        public static void ApplyFinalDemoUpdate()
        {
            EnsureBalanceAsset();
            PlayerSettings.WebGL.template = WebGLTemplate;
            PlayerSettings.bundleVersion = TestBuildVersion;
            if (YG2.infoYG != null)
            {
                YG2.infoYG.Basic.autoGRA = false;
                EditorUtility.SetDirty(YG2.infoYG);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            FirstHourBalanceSimulator.SimulateFirstHour();
            Debug.Log(
                "PASS: Apply Final Demo Update completed safely. " +
                "Save key and existing content IDs were preserved.");
            ValidateFinalDemoUpdate();
        }

        [MenuItem("Tools/Monstrology/Validate Final Demo Update")]
        public static void ValidateFinalDemoUpdate()
        {
            FinalDemoValidationReport report = BuildReport();
            report.Print();
        }

        public static void ApplyAndValidateBatch()
        {
            ApplyFinalDemoUpdate();
        }

        public static void ValidateBatch()
        {
            ValidateFinalDemoUpdate();
        }

        public static FirstHourBalanceConfig EnsureBalanceAsset()
        {
            FirstHourBalanceConfig config =
                AssetDatabase.LoadAssetAtPath<FirstHourBalanceConfig>(
                    BalanceAssetPath);
            if (config != null)
            {
                return config;
            }

            string directory = Path.GetDirectoryName(BalanceAssetPath);
            if (!string.IsNullOrEmpty(directory) &&
                !AssetDatabase.IsValidFolder(directory))
            {
                EnsureFolder(directory);
            }

            config = ScriptableObject.CreateInstance<
                FirstHourBalanceConfig>();
            AssetDatabase.CreateAsset(config, BalanceAssetPath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static FinalDemoValidationReport BuildReport()
        {
            FinalDemoValidationReport report =
                new FinalDemoValidationReport();
            string ui = Read("Assets/Monstrology/Scripts/UIManager.cs");
            string interaction = Read(
                "Assets/Monstrology/Scripts/InteractionSystem.cs");
            string adaptive = Read(
                "Assets/Monstrology/Scripts/AdaptiveUIController.cs");
            string pickup = Read(
                "Assets/Monstrology/Scripts/WorldPickup.cs");
            string collection = Read(
                "Assets/Monstrology/Scripts/CreatureCollectionManager.cs");
            string daily = Read(
                "Assets/Monstrology/Scripts/DailyRewardSystem.cs");
            string save = Read(
                "Assets/Monstrology/Scripts/SaveSystem.cs");
            string breeding = Read(
                "Assets/Monstrology/Scripts/BreedingSystem.cs");
            string template = Read(
                "Assets/WebGLTemplates/YandexGames/index.html");
            string style = Read(
                "Assets/WebGLTemplates/YandexGames/style.css");

            report.Pass(
                !EditorApplication.isCompiling &&
                CompilationPipeline.GetAssemblies().Any(assembly =>
                    assembly.name == "Assembly-CSharp"),
                "Unity runtime assembly compiles");
            report.Pass(
                EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled &&
                    scene.path == "Assets/Scenes/SampleScene.unity"),
                "SampleScene is enabled for the game");

            report.Pass(
                adaptive.Contains("Application.isMobilePlatform") &&
                adaptive.Contains("Input.touchSupported") &&
                adaptive.Contains("IsMobileUserAgent"),
                "Desktop/Mobile mode uses platform, touch and WebGL UA");
            report.Pass(
                ui.Contains("\"MobileControls\"") &&
                interaction.Contains("SetMobileMode") &&
                interaction.Contains("visible && !mobileMode") &&
                interaction.Contains(
                    "interactionEnabled && mobileMode"),
                "Desktop hides mobile controls and keeps E interaction");
            report.Pass(
                adaptive.Contains("Screen.safeArea") &&
                ui.Contains("\"SafeAreaRoot\""),
                "Runtime UI applies safe area");
            report.Pass(
                template.Contains("viewport-fit=cover") &&
                template.Contains("visualViewport") &&
                style.Contains("100dvh") &&
                style.Contains("overflow: hidden"),
                "iPhone/WebGL viewport fills available area without scroll");
            report.Pass(
                ui.Contains("RequestFullscreen") &&
                Read(
                    "Assets/Plugins/WebGL/MonstrologyWebGL.jslib")
                    .Contains("requestFullscreen"),
                "Fullscreen is requested only by the UI action");
            report.Pass(
                Read(
                    "Assets/Monstrology/Scripts/YandexGamesDebugOverlay.cs")
                    .Contains("\"DBG\""),
                "Development diagnostics can be collapsed");

            report.Pass(
                pickup.Contains("sprite.bounds.size") &&
                pickup.Contains("CalculateWorldVisualScale") &&
                pickup.Contains(
                    "pickupType == WorldPickupType.Accessory"),
                "Wardrobe world pickups are normalized by sprite bounds");
            report.Pass(
                pickup.Contains("circle.radius = Mathf.Clamp") &&
                !pickup.Contains("transform.localScale = baseScale"),
                "World pickup collider follows normalized visual size");

            report.Pass(
                !ui.Contains("AddPauseButton(card.transform, \"Exit\"") &&
                !ui.Contains("Application.Quit"),
                "Pause menu has no Exit button or Application.Quit");
            report.Pass(
                ui.Contains("bottomBarRoot.SetAsLastSibling") &&
                ui.Contains("HideStandardPanels();") &&
                ui.Contains("ActiveMainPanelCount"),
                "Main tabs switch directly and keep one panel active");

            report.Pass(
                collection.Contains("HandleCreatureRegistered") &&
                collection.Contains("RestoreMissingDiscoveredPets") &&
                collection.Contains("CurrentPetMigrationVersion"),
                "First discovery creates a pet and old discoveries migrate");
            report.Pass(
                collection.Contains("GetPetBySpecies(speciesId)") &&
                collection.Contains("MergeDuplicateSpecies"),
                "Repeated discoveries cannot create duplicate species pets");
            report.Pass(
                collection.Contains("char.IsControl") &&
                collection.Contains("MaxCustomNameLength"),
                "Pet names are sanitized and length-limited");

            report.Pass(
                daily.Contains("DailyRewardFirstLaunchRegistered") &&
                daily.Contains("UtcNow.Date <= firstLaunch.Date"),
                "Daily reward is blocked on first launch and same-day restart");
            report.Pass(
                save.Contains("saveRevision") &&
                save.Contains("updatedAtUtc") &&
                save.Contains("ImportIfNewer") &&
                save.Contains("BackupKey"),
                "Save conflict metadata and local backup are present");

            bool storageInstalled = Directory.Exists(ToAbsolute(
                "Assets/PluginYourGames/Modules/Storage"));
            bool authorizationInstalled = Directory.Exists(ToAbsolute(
                "Assets/PluginYourGames/Modules/Authorization"));
            report.Require(
                storageInstalled && authorizationInstalled,
                "PluginYG2 Storage and Authorization modules are installed",
                "Real cross-device cloud sync is unavailable. " +
                "Local PlayerPrefs fallback remains active.");

            GameContent content = DemoContentFactory.Create();
            DemoGameplayContentRepair.Repair(content);
            EvolutionFallbackBuilder.EnsureFallbackChains(content);
            EvolutionProgressUtility.ApplyCumulativeThresholds(content);
            List<SpeciesEvolutionData> evolutions =
                content.speciesEvolutions
                    .Where(value => value != null)
                    .ToList();
            report.Pass(
                evolutions.All(evolution =>
                {
                    int stage =
                        EvolutionProgressUtility.GetSpeciesStage(
                            content,
                            evolution.baseSpeciesId);
                    return evolution.requiredCopies ==
                           EvolutionProgressUtility
                               .GetThresholdForTransition(stage);
                }),
                "Evolution thresholds are cumulative 10/25/60/120");
            report.Pass(
                !breeding.Contains("ConsumeCreatureCopies(") &&
                breeding.Contains("GetLifetimeCreatureCount"),
                "Evolution does not spend collected copies");

            FirstHourBalanceConfig balance =
                AssetDatabase.LoadAssetAtPath<FirstHourBalanceConfig>(
                    BalanceAssetPath);
            report.Pass(
                balance != null &&
                balance.maxEmptyStreak == 2 &&
                balance.maxSameResourceStreak == 3,
                "First-hour balance asset contains pity limits");
            report.Pass(
                File.Exists(ToAbsolute(
                    "Assets/Monstrology/Scripts/FirstSessionDirector.cs")) &&
                File.Exists(ToAbsolute(
                    "Assets/Monstrology/Documentation/FIRST_HOUR_BALANCE_REPORT.md")),
                "FirstSessionDirector and simulation report exist");

            report.Pass(
                PlayerSettings.WebGL.template == WebGLTemplate,
                "Yandex Games WebGL template is selected");
            report.Pass(
                YG2.infoYG != null && !YG2.infoYG.Basic.autoGRA,
                "Auto GRA is disabled for guarded manual Game Ready");
            report.Pass(
                PlayerSettings.bundleVersion == TestBuildVersion,
                "Test build version is " + TestBuildVersion);

            string regression = Read(
                "Assets/Monstrology/Scripts/DemoGameplayContentRepair.cs");
            report.Pass(
                regression.Contains("forest_boots") &&
                Read(
                    "Assets/Monstrology/Scripts/YandexGamesBridge.cs")
                    .Contains("EnergyRewardId = \"energy_3\"") &&
                Read(
                    "Assets/Monstrology/Scripts/FollowPetController.cs")
                    .Contains("HasWorldRarityLabel"),
                "Previously fixed demo regressions remain guarded");
            return report;
        }

        private static string Read(string relativePath)
        {
            string path = ToAbsolute(relativePath);
            return File.Exists(path)
                ? File.ReadAllText(path)
                : string.Empty;
        }

        private static string ToAbsolute(string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    relativePath));
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Replace('\\', '/').Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }

    public sealed class FinalDemoValidationReport
    {
        private readonly List<Entry> entries = new List<Entry>();
        public bool HasErrors
        {
            get { return entries.Any(entry => entry.Level == "ERROR"); }
        }

        public void Pass(bool condition, string message)
        {
            entries.Add(new Entry(
                condition ? "PASS" : "ERROR",
                message));
        }

        public void Require(
            bool condition,
            string message,
            string failureDetails)
        {
            entries.Add(new Entry(
                condition ? "PASS" : "ERROR",
                condition ? message : message + ". " + failureDetails));
        }

        public void Warning(bool condition, string message)
        {
            entries.Add(new Entry(
                condition ? "PASS" : "WARNING",
                message));
        }

        public void Print()
        {
            foreach (Entry entry in entries)
            {
                string text = entry.Level + ": " + entry.Message;
                if (entry.Level == "ERROR")
                {
                    Debug.LogError(text);
                }
                else if (entry.Level == "WARNING")
                {
                    Debug.LogWarning(text);
                }
                else
                {
                    Debug.Log(text);
                }
            }

            Debug.Log(
                HasErrors
                    ? "FINAL DEMO UPDATE NOT READY"
                    : "FINAL DEMO UPDATE READY");
        }

        private sealed class Entry
        {
            public readonly string Level;
            public readonly string Message;

            public Entry(string level, string message)
            {
                Level = level;
                Message = message;
            }
        }
    }
}
#endif
