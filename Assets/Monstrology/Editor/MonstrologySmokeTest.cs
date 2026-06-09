#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology.Editor
{
    public static class MonstrologySmokeTest
    {
        private const string SessionKey = "Monstrology.SmokeTest.Stage";
        private const string ResultShownAtKey = "Monstrology.SmokeTest.ResultShownAt";
        private static int playFrames;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(SessionKey, "")))
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        [MenuItem("Tools/Monstrology/Run Smoke Test %#m")]
        public static void Run()
        {
            SessionState.SetString(SessionKey, "enter");
            playFrames = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            string stage = SessionState.GetString(SessionKey, "");
            if (stage == "enter" && EditorApplication.isPlaying)
            {
                SessionState.SetString(SessionKey, "play");
                return;
            }

            if (stage == "play" && EditorApplication.isPlaying)
            {
                playFrames++;
                if (playFrames < 10)
                {
                    return;
                }

                try
                {
                    GameManager game = Object.FindObjectOfType<GameManager>();
                    UIManager ui = Object.FindObjectOfType<UIManager>();
                    ExplorationSystem exploration = Object.FindObjectOfType<ExplorationSystem>();
                    Canvas canvas = Object.FindObjectOfType<Canvas>();
                    PlayerController2D player = Object.FindObjectOfType<PlayerController2D>();
                    WorldExplorationManager world = Object.FindObjectOfType<WorldExplorationManager>();
                    InteractionSystem interaction = Object.FindObjectOfType<InteractionSystem>();
                    CameraFollow2D cameraFollow = Object.FindObjectOfType<CameraFollow2D>();
                    CreatureCollectionManager petCollection = Object.FindObjectOfType<CreatureCollectionManager>();
                    PetsPanel petsPanel = Object.FindObjectOfType<PetsPanel>(true);
                    AccessoryInventoryManager accessoryInventory =
                        Object.FindObjectOfType<AccessoryInventoryManager>();
                    BreedingSystem breeding = Object.FindObjectOfType<BreedingSystem>();
                    PetUpgradeSystem upgrades = Object.FindObjectOfType<PetUpgradeSystem>();
                    WorldEnvironmentSystem environment =
                        Object.FindObjectOfType<WorldEnvironmentSystem>();
                    AchievementSystem achievements = Object.FindObjectOfType<AchievementSystem>();
                    FollowPetController followPet = Object.FindObjectOfType<FollowPetController>();
                    WardrobePanel wardrobePanel = Object.FindObjectOfType<WardrobePanel>(true);
                    BreedingPanel breedingPanel = Object.FindObjectOfType<BreedingPanel>(true);
                    PetUpgradePanel upgradePanel = Object.FindObjectOfType<PetUpgradePanel>(true);
                    AchievementPanel achievementPanel =
                        Object.FindObjectOfType<AchievementPanel>(true);
                    TrackChainSystem trackChains = Object.FindObjectOfType<TrackChainSystem>();
                    MutationSystem legacyMutationSystem = Object.FindObjectOfType<MutationSystem>();
                    if (game == null || ui == null || exploration == null ||
                        canvas == null || player == null ||
                        world == null || interaction == null || cameraFollow == null ||
                        petCollection == null || petsPanel == null || accessoryInventory == null ||
                        breeding == null || upgrades == null || environment == null ||
                        achievements == null || followPet == null || wardrobePanel == null ||
                        breedingPanel == null || achievementPanel == null || trackChains == null)
                    {
                        throw new System.InvalidOperationException(
                            "Missing runtime objects: " +
                            "game=" + (game != null) +
                            ", ui=" + (ui != null) +
                            ", exploration=" + (exploration != null) +
                            ", canvas=" + (canvas != null) +
                            ", player=" + (player != null) +
                            ", world=" + (world != null) +
                            ", interaction=" + (interaction != null) +
                            ", camera=" + (cameraFollow != null) +
                            ", collection=" + (petCollection != null) +
                            ", petsPanel=" + (petsPanel != null) +
                            ", inventory=" + (accessoryInventory != null) +
                            ", breeding=" + (breeding != null) +
                            ", upgrades=" + (upgrades != null) +
                            ", environment=" + (environment != null) +
                            ", achievements=" + (achievements != null) +
                            ", follower=" + (followPet != null) +
                            ", wardrobePanel=" + (wardrobePanel != null) +
                            ", breedingPanel=" + (breedingPanel != null) +
                            ", achievementPanel=" + (achievementPanel != null) +
                            ", trackChains=" + (trackChains != null));
                    }

                    if (legacyMutationSystem != null || upgradePanel != null ||
                        game.Content.mutationRecipes.Count != 0 ||
                        GameObject.Find("МУТАЦИИ") != null ||
                        GameObject.Find("ПРОКАЧКА") != null)
                    {
                        throw new System.InvalidOperationException("Mutation gameplay is still active.");
                    }

                    if (game.Content.biomes.Count != 6 || game.Content.creatures.Count < 20 ||
                        game.Content.accessories.Count < 6 || game.Content.speciesEvolutions.Count < 7 ||
                        game.Content.breedingRecipes.Count != 0)
                    {
                        throw new System.InvalidOperationException("Runtime demo content is incomplete.");
                    }

                    if (world.LoadedBiome != game.CurrentBiome || world.ActivePickupCount == 0)
                    {
                        throw new System.InvalidOperationException("World map or findings were not initialized.");
                    }

                    if (world.InfiniteMap == null || world.InfiniteMap.PooledTileCount != 9 ||
                        world.NavigationGuide == null)
                    {
                        throw new System.InvalidOperationException(
                            "Infinite map pooling or explorer compass was not initialized.");
                    }

                    if (world.NavigationGuide.HasTarget || ui.ResultCardVisible)
                    {
                        throw new System.InvalidOperationException(
                            "Compass or discovery result is visible at startup.");
                    }

                    if (game.Energy < WorldExplorationManager.ExplorerCompassEnergyCost)
                    {
                        game.AddEnergy(
                            WorldExplorationManager.ExplorerCompassEnergyCost - game.Energy);
                    }

                    int energyBeforeCompass = game.Energy;
                    int explorationCountBeforeCompass = game.ExplorationCount;
                    int creaturesBeforeCompass = game.TotalCreaturesFound;
                    bool compassStarted = world.StartQuickSearch();
                    if (!compassStarted || !world.NavigationGuide.HasTarget ||
                        game.Energy != energyBeforeCompass -
                            WorldExplorationManager.ExplorerCompassEnergyCost ||
                        game.ExplorationCount != explorationCountBeforeCompass ||
                        game.TotalCreaturesFound != creaturesBeforeCompass)
                    {
                        throw new System.InvalidOperationException(
                            "Quick search granted a reward or failed to create a navigation target.");
                    }

                    int energyAfterCompass = game.Energy;
                    if (world.StartQuickSearch() || game.Energy != energyAfterCompass)
                    {
                        throw new System.InvalidOperationException(
                            "Active one-shot compass was charged or replaced twice.");
                    }

                    WorldPickup compassTarget = world.NavigationGuide.Target;
                    world.NotifyPickupCollected(compassTarget);
                    if (world.NavigationGuide.HasTarget)
                    {
                        throw new System.InvalidOperationException(
                            "Compass stayed active after its target was collected.");
                    }

                    if (petCollection.Count > 0)
                    {
                        ui.OpenPets();
                        RectTransform selectedCard = null;
                        foreach (RectTransform rect in
                                 canvas.GetComponentsInChildren<RectTransform>(true))
                        {
                            if (rect.name == "SelectedPet")
                            {
                                selectedCard = rect;
                                break;
                            }
                        }

                        if (selectedCard == null ||
                            selectedCard.GetComponent<HorizontalLayoutGroup>() == null ||
                            selectedCard.Find("PortraitColumn") == null ||
                            selectedCard.Find("InfoColumn") == null ||
                            selectedCard.Find("ActionsColumn") == null)
                        {
                            throw new System.InvalidOperationException(
                                "Selected pet card does not use the three-column layout.");
                        }

                        ui.CloseAllPanels();
                    }

                    System.Collections.Generic.HashSet<string> uniqueSpecies =
                        new System.Collections.Generic.HashSet<string>();
                    foreach (CreatureInstance pet in petCollection.GetAllPets())
                    {
                        if (pet != null && !uniqueSpecies.Add(pet.speciesId))
                        {
                            throw new System.InvalidOperationException(
                                "Collection contains more than one pet of the same species.");
                        }
                    }

                    if (achievements.GetDefinitions().Count < 5 ||
                        game.GetCreature("cosmo_cat").allowedTimes.Count == 0 ||
                        game.GetCreature("astro_fox").allowedWeather.Count == 0)
                    {
                        throw new System.InvalidOperationException(
                            "Achievements, time restrictions or weather restrictions are incomplete.");
                    }

                    ItemData fireStone = game.GetItem("fire_stone");
                    ItemData iceCrystal = game.GetItem("ice_crystal");
                    if (fireStone == null || fireStone.kind != ItemKind.UpgradeResource ||
                        fireStone.requiredSpeciesId != "magma_orb" ||
                        iceCrystal == null || iceCrystal.requiredSpeciesId != "ice_saur")
                    {
                        throw new System.InvalidOperationException("Upgrade resources were not initialized.");
                    }

                    if (upgrades.GetRequiredResourceCount(1) != 1 ||
                        upgrades.GetRequiredResourceCount(2) != 2 ||
                        upgrades.GetRequiredResourceCount(3) != 4 ||
                        upgrades.GetRequiredResourceCount(4) != 7 ||
                        upgrades.GetRequiredResourceCount(5) != 11 ||
                        System.Enum.GetValues(typeof(AccessorySlot)).Length != 3)
                    {
                        throw new System.InvalidOperationException("Upgrade formula or equipment slots are invalid.");
                    }

                    CreatureInstance testParentA = new CreatureInstance
                    {
                        uniqueId = "smoke-a",
                        speciesId = "bread_cat",
                        level = 3,
                        genetics = CreatureGenetics.CreateWild(PetRarity.Common)
                    };
                    CreatureInstance testParentB = new CreatureInstance
                    {
                        uniqueId = "smoke-b",
                        speciesId = "cosmo_cat",
                        level = 3,
                        genetics = CreatureGenetics.CreateWild(PetRarity.Rare)
                    };
                    CreatureGenetics inherited = breeding.CalculateOffspringGenetics(testParentA, testParentB);
                    if (breeding.CanBreed(testParentA, testParentB) ||
                        !string.IsNullOrEmpty(breeding.GetPotentialSpecies(testParentA, testParentB)) ||
                        inherited.sizeGene < 0f || inherited.sizeGene > 1f ||
                        inherited.energyGene < 0f || inherited.energyGene > 1f ||
                        inherited.luckGene < 0f || inherited.luckGene > 1f)
                    {
                        throw new System.InvalidOperationException("Species restriction or legacy genetics failed.");
                    }

                    testParentB.speciesId = "bread_cat";
                    if (breeding.GetPotentialSpecies(testParentA, testParentB) != "bread_cat_ii")
                    {
                        throw new System.InvalidOperationException("Evolution chain is not deterministic.");
                    }

                    exploration.ResolveWorldDiscovery(ExplorationResultType.Nothing);
                    if (!ui.ResultCardVisible)
                    {
                        throw new System.InvalidOperationException(
                            "Discovery result card did not become visible.");
                    }

                    SessionState.SetFloat(ResultShownAtKey, Time.realtimeSinceStartup);
                    SessionState.SetString(SessionKey, "result_wait");
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    SessionState.EraseString(SessionKey);
                    EditorApplication.update -= Tick;
                    if (Application.isBatchMode)
                    {
                        EditorApplication.Exit(1);
                    }
                }

                return;
            }

            if (stage == "result_wait" && EditorApplication.isPlaying)
            {
                float shownAt = SessionState.GetFloat(ResultShownAtKey, 0f);
                if (Time.realtimeSinceStartup - shownAt < 3.6f)
                {
                    return;
                }

                try
                {
                    UIManager ui = Object.FindObjectOfType<UIManager>();
                    if (ui == null || ui.ResultCardVisible)
                    {
                        throw new System.InvalidOperationException(
                            "Discovery result card did not hide after three seconds.");
                    }

                    Debug.Log("MONSTROLOGY_SMOKE_TEST_PASS");
                    SessionState.EraseFloat(ResultShownAtKey);
                    SessionState.SetString(SessionKey, "exit");
                    EditorApplication.ExitPlaymode();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    SessionState.EraseFloat(ResultShownAtKey);
                    SessionState.EraseString(SessionKey);
                    EditorApplication.update -= Tick;
                    if (Application.isBatchMode)
                    {
                        EditorApplication.Exit(1);
                    }
                }

                return;
            }

            if (stage == "exit" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseFloat(ResultShownAtKey);
                SessionState.EraseString(SessionKey);
                EditorApplication.update -= Tick;
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }
    }
}
#endif
