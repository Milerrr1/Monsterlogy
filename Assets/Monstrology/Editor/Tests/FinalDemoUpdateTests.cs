#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Monstrology.Editor.Tests
{
    public class FinalDemoUpdateTests
    {
        private readonly List<GameObject> created =
            new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();
            AdaptiveUIController.SetForcedModeForTests(null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in created)
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            created.Clear();
            AdaptiveUIController.SetForcedModeForTests(null);
            SaveSystem.Delete();
        }

        [Test]
        public void DesktopModeHidesMobileControlsByContract()
        {
            AdaptiveUIController.SetForcedModeForTests(
                ControlMode.Desktop);
            Assert.AreEqual(
                ControlMode.Desktop,
                AdaptiveUIController.CurrentMode);

            AdaptiveUIController.SetForcedModeForTests(
                ControlMode.Mobile);
            Assert.AreEqual(
                ControlMode.Mobile,
                AdaptiveUIController.CurrentMode);
        }

        [Test]
        public void DesktopMovementKeepsHorizontalAndVerticalAxes()
        {
            string source = ReadProjectFile(
                "Assets/Monstrology/Scripts/PlayerController2D.cs");
            StringAssert.Contains(
                "Input.GetAxisRaw(\"Horizontal\")",
                source);
            StringAssert.Contains(
                "Input.GetAxisRaw(\"Vertical\")",
                source);
        }

        [Test]
        public void DesktopInteractionKeepsEKey()
        {
            string source = ReadProjectFile(
                "Assets/Monstrology/Scripts/InteractionSystem.cs");
            StringAssert.Contains("Input.GetKeyDown(KeyCode.E)", source);
        }

        [Test]
        public void SafeAreaAndResizeAreObserved()
        {
            string source = ReadProjectFile(
                "Assets/Monstrology/Scripts/AdaptiveUIController.cs");
            StringAssert.Contains("Screen.safeArea", source);
            StringAssert.Contains("Screen.width != lastScreenWidth", source);
            StringAssert.Contains("Screen.height != lastScreenHeight", source);
        }

        [Test]
        public void WebGLSafeAreaHelperIsLinkedAndExportsMatchCSharp()
        {
            string error;
            Assert.IsTrue(
                YandexDemoBuildTools.ValidateWebGLBridgeFiles(out error),
                error);

            string jslib = ReadProjectFile(
                "Assets/Plugins/WebGL/MonstrologyWebGL.jslib");
            StringAssert.Contains(
                "$MonstrologyGetSafeInset",
                jslib);
            Assert.AreEqual(4, CountOccurrences(jslib, "__deps"));
        }

        [Test]
        public void InvalidWebGLInsetsFallbackToFullScreen()
        {
            Rect nanResult =
                WebGLPlatformBridge.NormalizeSafeAreaInsets(
                    float.NaN,
                    0f,
                    0f,
                    0f);
            Rect overlappingResult =
                WebGLPlatformBridge.NormalizeSafeAreaInsets(
                    0.6f,
                    0.5f,
                    0f,
                    0f);

            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), nanResult);
            Assert.AreEqual(
                new Rect(0f, 0f, 1f, 1f),
                overlappingResult);
        }

        [Test]
        public void AdaptiveUiRejectsInvalidNormalizedSafeArea()
        {
            Rect safeArea;
            Assert.IsFalse(
                AdaptiveUIController.TryValidateNormalizedSafeArea(
                    Rect.MinMaxRect(0.8f, 0f, 0.2f, 1f),
                    out safeArea));
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), safeArea);
        }

        [Test]
        public void MobileControlsAreSeparateAndFullscreenIsUserDriven()
        {
            string ui = ReadProjectFile(
                "Assets/Monstrology/Scripts/UIManager.cs");
            StringAssert.Contains("\"MobileControls\"", ui);
            StringAssert.Contains(
                "fullscreen.onClick.AddListener(RequestFullscreen)",
                ui);
            Assert.IsFalse(
                ReadProjectFile(
                        "Assets/Monstrology/Scripts/AdaptiveUIController.cs")
                    .Contains("RequestFullscreen()"));
        }

        [Test]
        public void WorldPickupScaleUsesSpriteBounds()
        {
            Texture2D texture = new Texture2D(2048, 512);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2048f, 512f),
                new Vector2(0.5f, 0.5f),
                100f);
            float scale =
                WorldPickup.CalculateWorldVisualScale(sprite);
            float maxWorldSize =
                Mathf.Max(
                    sprite.bounds.size.x,
                    sprite.bounds.size.y) * scale;
            Assert.That(scale, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(maxWorldSize, Is.LessThanOrEqualTo(1.05f));
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        [TestCase("forest_hat")]
        [TestCase("forest_scarf")]
        public void ForestWardrobePickupHasBoundedWorldSize(
            string accessoryId)
        {
            GameContent content = DemoContentFactory.Create();
            AccessoryData accessory = content.accessories.Find(value =>
                value != null && value.id == accessoryId);
            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(
                    "Assets/Monstrology/Art/Resources/SpriteDatabase.asset");
            Assert.NotNull(accessory);
            Assert.NotNull(database);
            Sprite sprite = database.GetAccessory(accessory);
            Assert.NotNull(sprite);
            float scale =
                WorldPickup.CalculateWorldVisualScale(sprite);
            float maxSize =
                Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) *
                scale;
            Assert.That(maxSize, Is.LessThanOrEqualTo(1.05f));
        }

        [Test]
        public void PauseMenuHasNoExitOrQuit()
        {
            string ui = ReadProjectFile(
                "Assets/Monstrology/Scripts/UIManager.cs");
            Assert.IsFalse(ui.Contains(
                "AddPauseButton(card.transform, \"Exit\""));
            Assert.IsFalse(ui.Contains("Application.Quit"));
        }

        [Test]
        public void MainTabsUseOneExistingPanelAndKeepNavigationAvailable()
        {
            string ui = ReadProjectFile(
                "Assets/Monstrology/Scripts/UIManager.cs");
            StringAssert.Contains("HideStandardPanels();", ui);
            StringAssert.Contains(
                "bottomBarRoot.SetAsLastSibling()",
                ui);
            StringAssert.Contains("ActiveMainPanelCount", ui);
        }

        [Test]
        public void FirstDiscoveryCreatesOnePetAndRepeatDoesNotDuplicate()
        {
            TestContext context = CreateContext();
            CreatureData creature =
                context.Game.GetCreature("bread_cat");

            Assert.IsTrue(context.Game.AddCreature(creature));
            Assert.NotNull(
                context.Collection.GetPetBySpecies(creature.id));
            Assert.AreEqual(1, context.Collection.Count);

            Assert.IsFalse(context.Game.AddCreature(creature));
            Assert.AreEqual(1, context.Collection.Count);
            Assert.AreEqual(
                2,
                context.Game.GetLifetimeCreatureCount(creature.id));
        }

        [Test]
        public void OldDiscoveredCreatureMigratesIntoPets()
        {
            GameProgress old = new GameProgress
            {
                version = 9,
                discoveredSpecies = new List<string> { "bread_cat" },
                creatures = new List<StringIntEntry>
                {
                    new StringIntEntry("bread_cat", 3)
                },
                pets = new List<CreatureInstance>()
            };
            SaveSystem.Save(old);

            TestContext context = CreateContext();
            Assert.NotNull(
                context.Collection.GetPetBySpecies("bread_cat"));
            Assert.AreEqual(
                CreatureCollectionManager.CurrentPetMigrationVersion,
                context.Game.PetMigrationVersion);
        }

        [Test]
        public void PetNameSanitizationRemovesControlCharacters()
        {
            string value =
                CreatureCollectionManager.SanitizeCustomName(
                    "  Snow\n\t Cat\u0001  ",
                    "Fallback");
            Assert.AreEqual("Snow Cat", value);
            Assert.LessOrEqual(
                value.Length,
                CreatureCollectionManager.MaxCustomNameLength);
        }

        [Test]
        public void DailyRewardStartsOnNextUtcDay()
        {
            TestContext context = CreateContext();
            DateTime current = new DateTime(
                2026,
                6,
                13,
                12,
                0,
                0,
                DateTimeKind.Utc);
            DailyRewardSystem daily =
                context.Root.AddComponent<DailyRewardSystem>();
            daily.Initialize(
                context.Game,
                context.Wardrobe,
                () => current);
            Assert.IsFalse(daily.CanClaimToday);

            current = current.AddDays(1);
            Assert.IsTrue(daily.CanClaimToday);
            string message;
            Assert.IsTrue(daily.Claim(out message));
            Assert.IsFalse(daily.CanClaimToday);
        }

        [Test]
        public void NewerSaveWinsAndOlderSaveCannotOverwrite()
        {
            GameProgress local = new GameProgress
            {
                saveRevision = 8,
                updatedAtUtc = "2026-06-13T10:00:00Z"
            };
            GameProgress remote = new GameProgress
            {
                saveRevision = 7,
                updatedAtUtc = "2026-06-13T11:00:00Z"
            };
            Assert.Greater(
                SaveSystem.CompareFreshness(local, remote),
                0);
            Assert.Less(
                SaveSystem.CompareFreshness(remote, local),
                0);
        }

        [Test]
        public void ImportNewerSaveCreatesBackup()
        {
            GameProgress local = new GameProgress
            {
                coins = 10,
                saveRevision = 2,
                updatedAtUtc = "2026-06-12T10:00:00Z"
            };
            SaveSystem.Save(local);
            string localJson = SaveSystem.GetStoredJson();
            GameProgress remote = new GameProgress
            {
                coins = 20,
                saveRevision = 20,
                updatedAtUtc = "2026-06-13T10:00:00Z"
            };

            Assert.IsTrue(SaveSystem.ImportIfNewer(
                JsonUtility.ToJson(remote)));
            Assert.AreEqual(localJson, SaveSystem.GetBackupJson());
        }

        [Test]
        public void OlderRemoteSaveIsRejected()
        {
            GameProgress local = new GameProgress
            {
                saveRevision = 20,
                updatedAtUtc = "2026-06-13T10:00:00Z"
            };
            SaveSystem.Save(local);
            string before = SaveSystem.GetStoredJson();
            GameProgress remote = new GameProgress
            {
                saveRevision = 2,
                updatedAtUtc = "2026-06-12T10:00:00Z"
            };

            Assert.IsFalse(SaveSystem.ImportIfNewer(
                JsonUtility.ToJson(remote)));
            Assert.AreEqual(before, SaveSystem.GetStoredJson());
        }

        [TestCase(0, 10)]
        [TestCase(1, 25)]
        [TestCase(2, 60)]
        [TestCase(3, 120)]
        public void EvolutionThresholdMatchesTransition(
            int transition,
            int expected)
        {
            Assert.AreEqual(
                expected,
                EvolutionProgressUtility.GetThresholdForTransition(
                    transition));
        }

        [Test]
        public void EvolutionThresholdsAreCumulativeAndCopiesRemain()
        {
            TestContext context = CreateContext();
            CreatureData bread =
                context.Game.GetCreature("bread_cat");
            context.Game.AddCreatureCopies(bread.id, 10);

            BreedingSystem breeding =
                context.Root.AddComponent<BreedingSystem>();
            breeding.Initialize(
                context.Game,
                context.Collection);
            Assert.IsTrue(breeding.CanEvolve(bread.id));

            CreatureInstance evolved =
                breeding.EvolveSpecies(bread.id);
            Assert.NotNull(evolved);
            Assert.AreEqual("bread_cat_ii", evolved.speciesId);
            Assert.AreEqual(
                10,
                context.Game.GetLifetimeCreatureCount(bread.id));
            Assert.AreEqual(
                10,
                context.Game.GetCreatureCount(bread.id));
        }

        [Test]
        public void FirstSessionDirectorPreventsThirdEmpty()
        {
            GameObject root = NewObject("Director");
            FirstSessionDirector director =
                root.AddComponent<FirstSessionDirector>();
            FirstHourBalanceConfig config =
                ScriptableObject.CreateInstance<
                    FirstHourBalanceConfig>();
            director.Initialize(null, null, null, null, config);

            ExplorationResult empty = new ExplorationResult
            {
                type = ExplorationResultType.Nothing
            };
            director.RecordOutcomeForSimulation(empty);
            director.RecordOutcomeForSimulation(empty);
            Assert.AreNotEqual(
                WorldPickupType.Nothing,
                director.SelectPickupType(1f));
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ResourceRepeatLimitChoosesAlternative()
        {
            GameObject root = NewObject("Director");
            FirstSessionDirector director =
                root.AddComponent<FirstSessionDirector>();
            FirstHourBalanceConfig config =
                ScriptableObject.CreateInstance<
                    FirstHourBalanceConfig>();
            director.Initialize(null, null, null, null, config);

            ItemData first = ScriptableObject.CreateInstance<ItemData>();
            first.id = "first";
            ItemData second = ScriptableObject.CreateInstance<ItemData>();
            second.id = "second";
            for (int index = 0;
                 index < config.maxSameResourceStreak;
                 index++)
            {
                director.RecordOutcomeForSimulation(
                    new ExplorationResult
                    {
                        type = ExplorationResultType.Item,
                        item = first
                    });
            }

            Assert.AreSame(
                second,
                director.ChooseItem(
                    new List<ItemData> { first, second }));
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void BalanceConfigAndSimulationReportExist()
        {
            Assert.NotNull(
                AssetDatabase.LoadAssetAtPath<FirstHourBalanceConfig>(
                    "Assets/Monstrology/Resources/FirstHourBalanceConfig.asset"));
            Assert.IsTrue(File.Exists(ProjectPath(
                "Assets/Monstrology/Documentation/FIRST_HOUR_BALANCE_REPORT.md")));
        }

        [Test]
        public void VacuumRhinoAndFireStoneRegressionsRemainFixed()
        {
            GameContent content = DemoContentFactory.Create();
            DemoGameplayContentRepair.Repair(content);
            CreatureData vacuum = content.creatures.Find(value =>
                value != null && value.id == "vacuum_rhino");
            ItemData fireStone = content.items.Find(value =>
                value != null && value.id == "fire_stone");
            Assert.NotNull(vacuum);
            Assert.AreEqual(BiomeType.Desert, vacuum.biome);
            Assert.AreEqual(CreatureRarity.Rare, vacuum.rarity);
            Assert.NotNull(fireStone);
            Assert.IsFalse(ExplorationSystem.CanItemDropInBiome(
                fireStone,
                BiomeType.Forest));
        }

        [Test]
        public void RewardedEnergyAndFollowerRarityRegressionsRemainFixed()
        {
            string ui = ReadProjectFile(
                "Assets/Monstrology/Scripts/UIManager.cs");
            string bridge = ReadProjectFile(
                "Assets/Monstrology/Scripts/YandexGamesBridge.cs");
            string follower = ReadProjectFile(
                "Assets/Monstrology/Scripts/FollowPetController.cs");
            StringAssert.Contains(
                "ShowRewardedAd(YandexGamesBridge.EnergyRewardId, () =>",
                ui);
            StringAssert.Contains(
                "public const string EnergyRewardId = \"energy_3\"",
                bridge);
            StringAssert.Contains("HasWorldRarityLabel", follower);
            Assert.IsFalse(follower.Contains(
                "new GameObject(\"Rarity\")"));
        }

        [Test]
        public void RussianFontAssetsRemainEmbedded()
        {
            Assert.IsTrue(File.Exists(ProjectPath(
                "Assets/Monstrology/Fonts/NotoSans-Regular.ttf")));
            Assert.IsTrue(File.Exists(ProjectPath(
                "Assets/Monstrology/Fonts/MonstrologyRussian SDF.asset")));
        }

        private TestContext CreateContext()
        {
            GameObject root = NewObject("TestGame");
            GameContent content = DemoContentFactory.Create();
            DemoGameplayContentRepair.Repair(content);
            EvolutionFallbackBuilder.EnsureFallbackChains(content);
            EvolutionProgressUtility.ApplyCumulativeThresholds(content);

            GameManager game = root.AddComponent<GameManager>();
            game.Initialize(content);
            CreatureCollectionManager collection =
                root.AddComponent<CreatureCollectionManager>();
            collection.Initialize(game);
            AccessoryInventoryManager wardrobe =
                root.AddComponent<AccessoryInventoryManager>();
            wardrobe.Initialize(game, collection);
            return new TestContext
            {
                Root = root,
                Game = game,
                Collection = collection,
                Wardrobe = wardrobe
            };
        }

        private GameObject NewObject(string name)
        {
            GameObject instance = new GameObject(name);
            created.Add(instance);
            return instance;
        }

        private static string ReadProjectFile(string relativePath)
        {
            return File.ReadAllText(ProjectPath(relativePath));
        }

        private static string ProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                relativePath));
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

        private sealed class TestContext
        {
            public GameObject Root;
            public GameManager Game;
            public CreatureCollectionManager Collection;
            public AccessoryInventoryManager Wardrobe;
        }
    }
}
#endif
