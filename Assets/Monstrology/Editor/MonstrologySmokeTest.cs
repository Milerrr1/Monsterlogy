#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Monstrology.Editor
{
    public static class MonstrologySmokeTest
    {
        private const string SessionKey = "Monstrology.SmokeTest.Stage";
        private const string ResultShownAtKey = "Monstrology.SmokeTest.ResultShownAt";
        private static int playStartFrame;
        private static int restartWaitUpdates;
        private static int rewardedEnergyBefore;
        private static float rewardedRequestedAt;
        private static float rewardedOpenedAt;
        private static float rewardedTimeScaleBefore;
        private static bool rewardedAudioPauseBefore;
        private static bool rewardedMovementBefore;
        private static bool rewardedAdvertisementOpened;

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
            playStartFrame = 0;
            restartWaitUpdates = 0;
            rewardedEnergyBefore = 0;
            rewardedRequestedAt = 0f;
            rewardedOpenedAt = 0f;
            rewardedTimeScaleBefore = 1f;
            rewardedAudioPauseBefore = false;
            rewardedMovementBefore = true;
            rewardedAdvertisementOpened = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(SessionKey, "restart");
                EditorApplication.ExitPlaymode();
                return;
            }

            SessionState.SetString(SessionKey, "enter");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            string stage = SessionState.GetString(SessionKey, "");
            if (stage == "restart" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                restartWaitUpdates = 0;
                SessionState.SetString(SessionKey, "restart_wait");
                return;
            }

            if (stage == "restart_wait" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                restartWaitUpdates++;
                if (restartWaitUpdates < 30)
                {
                    return;
                }

                SessionState.SetString(SessionKey, "enter");
                EditorApplication.EnterPlaymode();
                return;
            }

            if (stage == "enter" && EditorApplication.isPlaying)
            {
                playStartFrame = Time.frameCount;
                SessionState.SetString(SessionKey, "play");
                return;
            }

            if (stage == "play" && EditorApplication.isPlaying)
            {
                if (Time.frameCount - playStartFrame < 30)
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
                    EnergyRegenerationSystem energyRegeneration =
                        Object.FindObjectOfType<EnergyRegenerationSystem>();
                    StarterBoostSystem starterBoost = Object.FindObjectOfType<StarterBoostSystem>();
                    SignatureSetSystem signatureSets = Object.FindObjectOfType<SignatureSetSystem>();
                    CreatureNestSystem creatureNests = Object.FindObjectOfType<CreatureNestSystem>();
                    BiomeEventSystem biomeEvents = Object.FindObjectOfType<BiomeEventSystem>();
                    FavoriteHelperSystem favoriteHelper = Object.FindObjectOfType<FavoriteHelperSystem>();
                    AudioManager audioManager = Object.FindObjectOfType<AudioManager>();
                    DailyRewardSystem dailyRewards = Object.FindObjectOfType<DailyRewardSystem>();
                    GameplayHintSystem hints = Object.FindObjectOfType<GameplayHintSystem>();
                    ReleaseReadinessCheck readiness =
                        Object.FindObjectOfType<ReleaseReadinessCheck>();
                    BetaReadinessCheck betaReadiness =
                        Object.FindObjectOfType<BetaReadinessCheck>();
                    CreatureVisualValidator creatureVisualValidator =
                        Object.FindObjectOfType<CreatureVisualValidator>();
                    EncyclopediaAvailabilityCheck encyclopediaCheck =
                        Object.FindObjectOfType<EncyclopediaAvailabilityCheck>();
                    NestPanel nestPanel = Object.FindObjectOfType<NestPanel>(true);
                    FollowPetController followPet = Object.FindObjectOfType<FollowPetController>();
                    WardrobePanel wardrobePanel = Object.FindObjectOfType<WardrobePanel>(true);
                    BreedingPanel breedingPanel = Object.FindObjectOfType<BreedingPanel>(true);
                    PetUpgradePanel upgradePanel = Object.FindObjectOfType<PetUpgradePanel>(true);
                    AchievementPanel achievementPanel =
                        Object.FindObjectOfType<AchievementPanel>(true);
                    TrackChainSystem trackChains = Object.FindObjectOfType<TrackChainSystem>();
                    YandexGamesBridge yandexBridge =
                        Object.FindObjectOfType<YandexGamesBridge>();
                    MutationSystem legacyMutationSystem = Object.FindObjectOfType<MutationSystem>();
                    if (game == null || ui == null || exploration == null ||
                        canvas == null || player == null ||
                        world == null || interaction == null || cameraFollow == null ||
                        petCollection == null || petsPanel == null || accessoryInventory == null ||
                        breeding == null || upgrades == null || environment == null ||
                        achievements == null || energyRegeneration == null ||
                        starterBoost == null || signatureSets == null || creatureNests == null ||
                        biomeEvents == null || favoriteHelper == null ||
                        audioManager == null || dailyRewards == null || hints == null ||
                        readiness == null || betaReadiness == null ||
                        creatureVisualValidator == null ||
                        encyclopediaCheck == null || nestPanel == null ||
                        followPet == null || wardrobePanel == null ||
                        breedingPanel == null || achievementPanel == null ||
                        trackChains == null || yandexBridge == null)
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
                            ", energyRegeneration=" + (energyRegeneration != null) +
                            ", starterBoost=" + (starterBoost != null) +
                            ", signatureSets=" + (signatureSets != null) +
                            ", creatureNests=" + (creatureNests != null) +
                            ", biomeEvents=" + (biomeEvents != null) +
                            ", favoriteHelper=" + (favoriteHelper != null) +
                            ", audioManager=" + (audioManager != null) +
                            ", dailyRewards=" + (dailyRewards != null) +
                            ", hints=" + (hints != null) +
                            ", readiness=" + (readiness != null) +
                            ", betaReadiness=" + (betaReadiness != null) +
                            ", creatureVisualValidator=" +
                            (creatureVisualValidator != null) +
                            ", encyclopediaCheck=" + (encyclopediaCheck != null) +
                            ", nestPanel=" + (nestPanel != null) +
                            ", follower=" + (followPet != null) +
                            ", wardrobePanel=" + (wardrobePanel != null) +
                            ", breedingPanel=" + (breedingPanel != null) +
                            ", achievementPanel=" + (achievementPanel != null) +
                            ", trackChains=" + (trackChains != null) +
                            ", yandexBridge=" + (yandexBridge != null));
                    }

                    if (legacyMutationSystem != null || upgradePanel != null ||
                        game.Content.mutationRecipes.Count != 0 ||
                        GameObject.Find("МУТАЦИИ") != null ||
                        GameObject.Find("ПРОКАЧКА") != null)
                    {
                        throw new System.InvalidOperationException("Mutation gameplay is still active.");
                    }

                    if (game.Content.biomes.Count != 6 || game.Content.creatures.Count < 20 ||
                        game.Content.accessories.Count < 24 ||
                        game.Content.signatureSets.Count < 6 ||
                        game.Content.creatureNests.Count < 5 ||
                        game.Content.biomeEvents.Count < 6 ||
                        game.Content.speciesEvolutions.Count < 7 ||
                        game.Content.breedingRecipes.Count != 0)
                    {
                        throw new System.InvalidOperationException("Runtime demo content is incomplete.");
                    }

                    SpriteDatabase spriteDatabase = SpriteDatabase.Active;
                    if (spriteDatabase == null ||
                        Resources.Load<SpriteDatabase>("SpriteDatabase") == null ||
                        Resources.Load<CreatureBaseTemplate>(
                            "CreatureBaseTemplate") == null ||
                        !CreatureBaseTemplate.Active.HasValidStandard() ||
                        game.Content.creatures.Any(creature =>
                            spriteDatabase.GetCreaturePortrait(creature) == null ||
                            spriteDatabase.GetCreatureWorld(creature) == null ||
                            spriteDatabase.GetCreatureEvolution(creature) == null) ||
                        game.Content.items.Any(item =>
                            spriteDatabase.GetItem(item) == null) ||
                        game.Content.accessories.Any(accessory =>
                            spriteDatabase.GetAccessory(accessory) == null) ||
                        game.Content.creatureNests.Any(nest =>
                            spriteDatabase.GetNest(nest) == null) ||
                        game.Content.biomeEvents.Any(eventData =>
                            spriteDatabase.GetSpecialEvent(eventData) == null) ||
                        game.Content.biomes.Any(biome =>
                            spriteDatabase.GetBiomeBackground(
                                biome,
                                biome != null ? biome.mapData : null) == null) ||
                        System.Enum.GetValues(typeof(TrackType))
                            .Cast<TrackType>()
                            .Any(track => spriteDatabase.GetTrack(track) == null))
                    {
                        throw new System.InvalidOperationException(
                            "SpriteDatabase does not resolve all replaceable artwork.");
                    }

                    string forestContentReport =
                        ForestContentInstaller.GetValidationReport();
                    if (string.IsNullOrEmpty(forestContentReport) ||
                        !forestContentReport.Contains(
                            "FOREST_CONTENT_VALIDATION_PASS"))
                    {
                        throw new System.InvalidOperationException(
                            "Forest content validation failed:\n" +
                            forestContentReport);
                    }

                    string desertContentReport =
                        ForestContentInstaller.GetDesertValidationReport();
                    if (string.IsNullOrEmpty(desertContentReport) ||
                        !desertContentReport.Contains(
                            "DESERT_CONTENT_VALIDATION_PASS"))
                    {
                        throw new System.InvalidOperationException(
                            "Desert content validation failed:\n" +
                            desertContentReport);
                    }

                    CreatureData breadcat = game.GetCreature("bread_cat");
                    CreatureData vacuumRhino = game.GetCreature("vacuum_rhino");
                    CreatureData watermelon = game.GetCreature("watermelon_saur");
                    BiomeData forest = game.GetBiome(BiomeType.Forest);
                    BiomeData desert = game.GetBiome(BiomeType.Desert);
                    float desertRarityRatio =
                        watermelon != null && vacuumRhino != null
                            ? watermelon.appearanceChance * 0.2f /
                              (vacuumRhino.appearanceChance * 0.48f)
                            : 0f;
                    if (breadcat == null ||
                        breadcat.creatureName != "Хлебокот" ||
                        breadcat.biome != BiomeType.Forest ||
                        breadcat.rarity != CreatureRarity.Common ||
                        breadcat.element != CreatureElement.Nature ||
                        vacuumRhino == null ||
                        vacuumRhino.creatureName != "Пылесосорог" ||
                        vacuumRhino.description !=
                            "Пустынный чистюля, который засасывает песок, пыль и " +
                            "потерянные путешественниками предметы металлическим хоботом." ||
                        vacuumRhino.biome != BiomeType.Desert ||
                        vacuumRhino.rarity != CreatureRarity.Rare ||
                        vacuumRhino.element != CreatureElement.Sand ||
                        vacuumRhino.appearanceChance <= 0f ||
                        forest == null ||
                        !forest.availableCreatures.Any(creature =>
                            creature != null && creature.id == "bread_cat") ||
                        forest.availableCreatures.Any(creature =>
                            creature != null && creature.id == "vacuum_rhino") ||
                        forest.mapData.possibleCreatures.Any(creature =>
                            creature != null && creature.id == "vacuum_rhino") ||
                        desert == null ||
                        !desert.availableCreatures.Any(creature =>
                            creature != null && creature.id == "vacuum_rhino") ||
                        !desert.mapData.possibleCreatures.Any(creature =>
                            creature != null && creature.id == "vacuum_rhino") ||
                        desertRarityRatio < 1.5f ||
                        AssetDatabase.GetAssetPath(
                            spriteDatabase.GetCreatureWorld(breadcat)) !=
                        "Assets/Monstrology/Art/Creatures/Breadcat.png" ||
                        AssetDatabase.GetAssetPath(
                            spriteDatabase.GetCreatureWorld(vacuumRhino)) !=
                        "Assets/Monstrology/Art/Creatures/VacuumRhino.png")
                    {
                        throw new System.InvalidOperationException(
                            "Forest/desert creature data or final artwork is invalid.");
                    }

                    ItemData breadCrumbs = game.GetItem("bread_crumbs");
                    ItemData vacuumFilter = game.GetItem("forest_battery");
                    if (breadCrumbs == null ||
                        breadCrumbs.kind != ItemKind.UpgradeResource ||
                        breadCrumbs.requiredSpeciesId != "bread_cat" ||
                        breadCrumbs.preferredBiome != BiomeType.Forest ||
                        vacuumFilter == null ||
                        vacuumFilter.kind != ItemKind.UpgradeResource ||
                        vacuumFilter.itemName != "Песчаный фильтр" ||
                        vacuumFilter.requiredSpeciesId != "vacuum_rhino" ||
                        vacuumFilter.preferredBiome != BiomeType.Desert)
                    {
                        throw new System.InvalidOperationException(
                            "Pet upgrade resources are invalid.");
                    }

                    ItemData magmaFireStone = game.GetItem("fire_stone");
                    if (magmaFireStone == null ||
                        magmaFireStone.requiredSpeciesId != "magma_orb" ||
                        magmaFireStone.preferredBiome != BiomeType.Volcano ||
                        magmaFireStone.GetVisibleName(false) !=
                            ItemData.HiddenName ||
                        magmaFireStone.GetVisibleDescription(false) !=
                            ItemData.HiddenDescription ||
                        magmaFireStone.GetVisibleName(true) !=
                            magmaFireStone.itemName ||
                        magmaFireStone.GetVisibleDescription(true) !=
                            magmaFireStone.description ||
                        ExplorationSystem.CanItemDropInBiome(
                            magmaFireStone,
                            BiomeType.Forest) ||
                        !ExplorationSystem.CanItemDropInBiome(
                            magmaFireStone,
                            BiomeType.Volcano))
                    {
                        throw new System.InvalidOperationException(
                            "Linked resource visibility or biome assignment is invalid.");
                    }

                    if (!game.IsCreatureFound(vacuumRhino.id))
                    {
                        game.AddCreature(vacuumRhino);
                    }

                    CreatureInstance oldVacuumPet =
                        petCollection.GetPetBySpecies(vacuumRhino.id);
                    if (oldVacuumPet != null &&
                        !petCollection.RemovePet(oldVacuumPet.uniqueId))
                    {
                        throw new System.InvalidOperationException(
                            "Vacuum Rhino setup could not remove the old pet entry.");
                    }

                    petCollection.Initialize(game);
                    CreatureInstance vacuumPet =
                        petCollection.GetPetBySpecies(vacuumRhino.id);
                    if (vacuumPet == null ||
                        petCollection.GetAllPets().Count(pet =>
                            pet != null &&
                            pet.speciesId == vacuumRhino.id) != 1)
                    {
                        throw new System.InvalidOperationException(
                            "Discovered Vacuum Rhino was not migrated into Pets.");
                    }

                    const string vacuumPetName = "Пылесос";
                    if (!petCollection.RenamePet(
                            vacuumPet.uniqueId,
                            vacuumPetName) ||
                        !petCollection.SetFavorite(vacuumPet.uniqueId))
                    {
                        throw new System.InvalidOperationException(
                            "Vacuum Rhino could not be renamed or selected as favorite.");
                    }

                    string vacuumPetId = vacuumPet.uniqueId;
                    int vacuumCopiesBeforeRepeat =
                        game.GetCreatureCount(vacuumRhino.id);
                    game.AddCreature(vacuumRhino);
                    CreatureInstance repeatedVacuumPet =
                        petCollection.GetPetBySpecies(vacuumRhino.id);
                    if (game.GetCreatureCount(vacuumRhino.id) !=
                            vacuumCopiesBeforeRepeat + 1 ||
                        repeatedVacuumPet == null ||
                        repeatedVacuumPet.uniqueId != vacuumPetId ||
                        petCollection.GetAllPets().Count(pet =>
                            pet != null &&
                            pet.speciesId == vacuumRhino.id) != 1)
                    {
                        throw new System.InvalidOperationException(
                            "Repeated Vacuum Rhino encounter created a duplicate pet.");
                    }

                    petCollection.Initialize(game);
                    vacuumPet = petCollection.GetPetBySpecies(vacuumRhino.id);
                    if (vacuumPet == null ||
                        vacuumPet.customName != vacuumPetName ||
                        !vacuumPet.isFavorite)
                    {
                        throw new System.InvalidOperationException(
                            "Vacuum Rhino name or favorite state did not survive reload.");
                    }

                    CreatureNestData vacuumNest =
                        game.Content.creatureNests.Find(nest =>
                            nest != null &&
                            nest.speciesId == "vacuum_rhino");
                    if (vacuumNest == null ||
                        vacuumNest.biome != BiomeType.Desert)
                    {
                        throw new System.InvalidOperationException(
                            "Vacuum Rhino nest is not assigned to Desert.");
                    }

                    if (game.GetEvolution("vacuum_rhino") == null ||
                        game.GetEvolution("vacuum_rhino").requiredCopies != 100 ||
                        game.GetEvolution("vacuum_rhino").resultSpeciesId !=
                            "vacuum_rhino_ii" ||
                        game.GetEvolution("vacuum_rhino_ii") == null ||
                        game.GetEvolution("vacuum_rhino_ii").requiredCopies != 250 ||
                        game.GetEvolution("vacuum_rhino_ii").resultSpeciesId !=
                            "vacuum_rhino_iii" ||
                        game.GetEvolution("vacuum_rhino_iii") == null ||
                        game.GetEvolution("vacuum_rhino_iii").requiredCopies != 500 ||
                        game.GetEvolution("vacuum_rhino_iii").resultSpeciesId !=
                            "turbo_vacuum_rhino")
                    {
                        throw new System.InvalidOperationException(
                            "Vacuum Rhino evolution chain is invalid.");
                    }

                    string[] vacuumForms =
                    {
                        "vacuum_rhino",
                        "vacuum_rhino_ii",
                        "vacuum_rhino_iii",
                        "turbo_vacuum_rhino"
                    };
                    if (vacuumForms.Any(id =>
                    {
                        CreatureData form = game.GetCreature(id);
                        return form == null ||
                               form.biome != BiomeType.Desert ||
                               form.element != CreatureElement.Sand ||
                               (id != "vacuum_rhino" &&
                                form.appearanceChance > 0f);
                    }))
                    {
                        throw new System.InvalidOperationException(
                            "Vacuum Rhino forms have invalid desert or wild-spawn data.");
                    }

                    AccessoryData forestHat = game.GetAccessory("forest_hat");
                    AccessoryData forestScarf = game.GetAccessory("forest_scarf");
                    AccessoryData forestBoots = game.GetAccessory("forest_boots");
                    SignatureSetData forestSet =
                        game.Content.signatureSets.Find(set =>
                            set != null && set.id == "forest_set");
                    if (forestHat == null ||
                        forestHat.slot != AccessorySlot.Head ||
                        forestScarf == null ||
                        forestScarf.slot != AccessorySlot.Body ||
                        forestBoots == null ||
                        forestBoots.slot != AccessorySlot.Legs ||
                        forestScarf.dropChance <= forestHat.dropChance ||
                        forestHat.dropChance <= forestBoots.dropChance ||
                        !ExplorationSystem.CanAccessoryDropInBiome(
                            forestHat,
                            BiomeType.Forest) ||
                        ExplorationSystem.CanAccessoryDropInBiome(
                            forestHat,
                            BiomeType.Desert) ||
                        forestSet == null ||
                        !forestSet.accessoryIds.SequenceEqual(new[]
                        {
                            "forest_hat",
                            "forest_scarf",
                            "forest_boots"
                        }) ||
                        !Mathf.Approximately(
                            forestSet.twoPieceResourceBonus,
                            0.05f) ||
                        !Mathf.Approximately(
                            forestSet.fullSetResourceBonus,
                            0.1f) ||
                        !Mathf.Approximately(
                            forestSet.fullSetRareCreatureBonus,
                            0.03f))
                    {
                        throw new System.InvalidOperationException(
                            "Forest wardrobe set data is invalid.");
                    }

                    GameObject forestRigObject =
                        new GameObject("ForestContentRigTest");
                    CreatureVisualRig forestRig =
                        forestRigObject.AddComponent<CreatureVisualRig>();
                    forestRig.ApplyCreature(breadcat, Color.white);
                    forestRig.ApplyAccessories(new[]
                    {
                        forestHat,
                        forestScarf,
                        forestBoots
                    });
                    bool forestRigValid =
                        forestRig.HasValidStructure() &&
                        forestRig.HeadAnchor.childCount == 1 &&
                        forestRig.BodyAnchor.childCount == 1 &&
                        forestRig.LegAnchor.childCount == 1 &&
                        forestRig.HeadAnchor.GetChild(0)
                            .GetComponent<SpriteRenderer>().sprite ==
                        spriteDatabase.GetAccessory(forestHat) &&
                        forestRig.BodyAnchor.GetChild(0)
                            .GetComponent<SpriteRenderer>().sprite ==
                        spriteDatabase.GetAccessory(forestScarf) &&
                        forestRig.LegAnchor.GetChild(0)
                            .GetComponent<SpriteRenderer>().sprite ==
                        spriteDatabase.GetAccessory(forestBoots);
                    Object.Destroy(forestRigObject);
                    if (!forestRigValid)
                    {
                        throw new System.InvalidOperationException(
                            "Forest wardrobe visuals are not attached to standard anchors.");
                    }

                    if (!accessoryInventory.AddAccessory(forestBoots) ||
                        !accessoryInventory.EquipAccessory(
                            vacuumPet.uniqueId,
                            forestBoots.id))
                    {
                        throw new System.InvalidOperationException(
                            "Forest boots could not be owned and equipped.");
                    }

                    petCollection.Initialize(game);
                    accessoryInventory.Initialize(game, petCollection);
                    vacuumPet = petCollection.GetPetBySpecies(vacuumRhino.id);
                    followPet.Initialize(
                        player,
                        petCollection,
                        accessoryInventory);
                    bool bootsPersisted =
                        vacuumPet != null &&
                        accessoryInventory.GetEquippedAccessories(
                                vacuumPet.uniqueId)
                            .Any(accessory =>
                                accessory != null &&
                                accessory.id == forestBoots.id);
                    if (!bootsPersisted ||
                        followPet.HasWorldRarityLabel ||
                        followPet.DisplayedName != vacuumPetName ||
                        followPet.VisualRig == null ||
                        !followPet.VisualRig.HasVisibleAccessory(
                            forestBoots.id))
                    {
                        throw new System.InvalidOperationException(
                            "Follower label or persisted forest boots visual is invalid.");
                    }

                    System.Collections.Generic.HashSet<string> evolutionResults =
                        new System.Collections.Generic.HashSet<string>(
                            game.Content.speciesEvolutions
                                .Where(evolution => evolution != null)
                                .Select(evolution => evolution.resultSpeciesId));
                    System.Collections.Generic.List<CreatureData> evolutionRoots =
                        game.Content.creatures.Where(creature =>
                            creature != null &&
                            !evolutionResults.Contains(creature.id)).ToList();
                    foreach (CreatureData root in evolutionRoots)
                    {
                        int forms = 1;
                        string currentSpeciesId = root.id;
                        System.Collections.Generic.HashSet<string> visited =
                            new System.Collections.Generic.HashSet<string>();
                        while (visited.Add(currentSpeciesId))
                        {
                            SpeciesEvolutionData next =
                                game.GetEvolution(currentSpeciesId);
                            if (next == null ||
                                string.IsNullOrEmpty(next.resultSpeciesId))
                            {
                                break;
                            }

                            forms++;
                            currentSpeciesId = next.resultSpeciesId;
                        }

                        if (forms < 3)
                        {
                            throw new System.InvalidOperationException(
                                "Evolution fallback chain is incomplete for " + root.id + ".");
                        }

                        CreatureNestData nest = game.Content.creatureNests.Find(entry =>
                            entry != null && entry.speciesId == root.id);
                        if (nest == null ||
                            string.IsNullOrEmpty(nest.id) ||
                            string.IsNullOrEmpty(nest.displayName) ||
                            string.IsNullOrEmpty(nest.description) ||
                            nest.rewardCooldownHours <= 0f)
                        {
                            throw new System.InvalidOperationException(
                                "Base species nest is incomplete for " + root.id + ".");
                        }
                    }

                    if (game.GetEvolution("bread_cat").requiredCopies != 100 ||
                        game.GetEvolution("bread_cat_ii").requiredCopies != 250 ||
                        game.GetEvolution("bread_cat_iii").requiredCopies != 500 ||
                        exploration.SelectCreature(new[]
                        {
                            game.GetCreature("bread_cat_ii")
                        }) != null ||
                        ContentValidator.ValidateEncyclopediaAvailability(game.Content) != 0)
                    {
                        throw new System.InvalidOperationException(
                            "Evolution copy requirements or encyclopedia availability are invalid.");
                    }

                    if (game.Content.creatureNests.Count < evolutionRoots.Count)
                    {
                        throw new System.InvalidOperationException(
                            "Not every base species has a creature nest.");
                    }

                    if (game.GetBiome(BiomeType.Forest).unlockPrice != 0 ||
                        game.GetBiome(BiomeType.Desert).unlockPrice != 300 ||
                        game.GetBiome(BiomeType.Tundra).unlockPrice != 1000 ||
                        game.GetBiome(BiomeType.Volcano).unlockPrice != 3000 ||
                        game.GetBiome(BiomeType.Ocean).unlockPrice != 8000 ||
                        game.GetBiome(BiomeType.Space).unlockPrice != 20000 ||
                        !WorldExplorationManager.ReleaseDropChancesAreValid)
                    {
                        throw new System.InvalidOperationException(
                            "Biome prices or release drop balance is invalid.");
                    }

                    if (world.LoadedBiome != game.CurrentBiome || world.ActivePickupCount == 0)
                    {
                        throw new System.InvalidOperationException("World map or findings were not initialized.");
                    }

                    if (WorldPickup.ActivePickups.Any(pickup =>
                            pickup == null ||
                            pickup.VisualRenderer == null ||
                            pickup.VisualRenderer.transform == pickup.transform) ||
                        world.NavigationGuide.GetComponent<SpriteRenderer>() != null ||
                        world.NavigationGuide.GetComponentInChildren<SpriteRenderer>() == null ||
                        player.GetComponent<SpriteRenderer>() != null ||
                        player.GetComponentInChildren<SpriteRenderer>() == null)
                    {
                        throw new System.InvalidOperationException(
                            "World visuals are not isolated in child SpriteRenderer objects.");
                    }

                    CreatureVisualRig followerVisual =
                        Object.FindObjectsOfType<CreatureVisualRig>(true)
                            .FirstOrDefault(rig =>
                                rig != null && rig.name == "FavoritePetFollower");
                    if (followerVisual == null)
                    {
                        throw new System.InvalidOperationException(
                            "Favorite pet does not use CreatureVisualRig.");
                    }

                    followerVisual.EnsureStructure();
                    if (!followerVisual.HasValidStructure() ||
                        followerVisual.HeadAnchor == null ||
                        followerVisual.BodyAnchor == null ||
                        followerVisual.LegAnchor == null ||
                        followerVisual.CreatureRenderer == null ||
                        followerVisual.CreatureRenderer.transform ==
                            followerVisual.transform)
                    {
                        throw new System.InvalidOperationException(
                            "Creature visual anchors or renderer are incomplete.");
                    }

                    if (world.InfiniteMap == null || world.InfiniteMap.PooledTileCount != 9 ||
                        world.NavigationGuide == null)
                    {
                        throw new System.InvalidOperationException(
                            "Infinite map pooling or explorer compass was not initialized.");
                    }

                    if (EnergyRegenerationSystem.MaxEnergy != 100 ||
                        EnergyRegenerationSystem.RegenerationSeconds != 45 ||
                        energyRegeneration.SecondsUntilNextEnergy < 0 ||
                        energyRegeneration.SecondsUntilNextEnergy > 45 ||
                        game.GetBiomeCreatureTotal(BiomeType.Forest) <= 0 ||
                        game.GetBiomeCreatureFound(BiomeType.Forest) >
                            game.GetBiomeCreatureTotal(BiomeType.Forest))
                    {
                        throw new System.InvalidOperationException(
                            "Energy regeneration or biome progress is invalid.");
                    }

                    SignatureSetData astronautSet = game.Content.signatureSets.Find(set =>
                        set != null && set.id == "astronaut_set");
                    if (astronautSet == null || astronautSet.accessoryIds.Count != 3 ||
                        game.GetAccessory("astro_helmet") == null ||
                        game.GetAccessory("astro_jacket") == null ||
                        game.GetAccessory("astro_boots") == null)
                    {
                        throw new System.InvalidOperationException(
                            "Signature set content is incomplete.");
                    }

                    AccessoryData astroHelmet = game.GetAccessory("astro_helmet");
                    AccessoryData strawHat = game.GetAccessory("straw_hat");
                    if (!astroHelmet.IsSignature ||
                        astroHelmet.EffectiveSetId != "astronaut_set" ||
                        !ExplorationSystem.CanAccessoryDropInBiome(
                            astroHelmet, BiomeType.Space) ||
                        ExplorationSystem.CanAccessoryDropInBiome(
                            astroHelmet, BiomeType.Forest) ||
                        strawHat == null || strawHat.IsSignature ||
                        System.Enum.GetValues(typeof(BiomeType))
                            .Cast<BiomeType>()
                            .Any(biome =>
                                !ExplorationSystem.CanAccessoryDropInBiome(
                                    strawHat, biome)))
                    {
                        throw new System.InvalidOperationException(
                            "Signature clothing biome restrictions are invalid.");
                    }

                    if (world.NavigationGuide.HasTarget || ui.ResultCardVisible)
                    {
                        throw new System.InvalidOperationException(
                            "Compass or discovery result is visible at startup.");
                    }

                    if (!ui.HasPauseMenu ||
                        audioManager.MusicVolume < 0f || audioManager.MusicVolume > 1f ||
                        audioManager.SfxVolume < 0f || audioManager.SfxVolume > 1f ||
                        dailyRewards.NextRewardDay < 1 || dailyRewards.NextRewardDay > 7)
                    {
                        throw new System.InvalidOperationException(
                            "Pause, audio settings or daily rewards were not initialized.");
                    }

                    if (SaveSystem.DefaultCoins < 50 ||
                        SaveSystem.DefaultCoins > 100 ||
                        SaveSystem.DefaultEnergy + 25 !=
                            EnergyRegenerationSystem.MaxEnergy ||
                        GameManager.CalculateHintCost(CreatureRarity.Common, 1) != 50 ||
                        GameManager.CalculateHintCost(CreatureRarity.Common, 2) != 150 ||
                        GameManager.CalculateHintCost(CreatureRarity.Common, 3) != 400 ||
                        GameManager.CalculateHintCost(CreatureRarity.Common, 4) != 1000 ||
                        GameManager.CalculateHintCost(CreatureRarity.Common, 5) != 2500 ||
                        GameManager.CalculateHintCost(CreatureRarity.Common, 6) != 5000 ||
                        GameManager.CalculateHintCost(CreatureRarity.Legendary, 1) <= 50)
                    {
                        throw new System.InvalidOperationException(
                            "Starting resources or progressive hint prices are invalid.");
                    }

                    if (!yandexBridge.IsSDKReady() ||
                        YG.YG2.infoYG.Basic.autoGRA ||
                        (player.MovementEnabled &&
                         (!yandexBridge.IsGameReadySent ||
                          !yandexBridge.IsGameplayAvailable)))
                    {
                        throw new System.InvalidOperationException(
                            "PluginYG2 bridge readiness or manual Game Ready is invalid.");
                    }

                    RectTransform bottomBar = canvas.GetComponentsInChildren<RectTransform>(true)
                        .FirstOrDefault(rect => rect.name == "BottomBar");
                    if (bottomBar == null || bottomBar.Find("ДОСТИЖЕНИЯ") != null)
                    {
                        throw new System.InvalidOperationException(
                            "Achievements are still exposed in the main HUD.");
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
                            selectedCard.Find("ActionsColumn") == null ||
                            selectedCard.Find(
                                "PortraitColumn/Portrait/EquippedAccessories/HeadAnchor") ==
                            null ||
                            selectedCard.Find(
                                "PortraitColumn/Portrait/EquippedAccessories/BodyAnchor") ==
                            null ||
                            selectedCard.Find(
                                "PortraitColumn/Portrait/EquippedAccessories/LegAnchor") ==
                            null)
                        {
                            throw new System.InvalidOperationException(
                                "Selected pet card does not use the three-column layout.");
                        }

                        ui.CloseAllPanels();
                    }

                    ui.OpenBreeding();
                    Dropdown evolutionDropdown =
                        breedingPanel.GetComponentInChildren<Dropdown>(true);
                    int expectedEvolutionSpecies = game.Content.creatures.Count(creature =>
                        creature != null &&
                        (game.IsCreatureFound(creature.id) ||
                         petCollection.GetPetBySpecies(creature.id) != null));
                    if (expectedEvolutionSpecies > 0 &&
                        (evolutionDropdown == null ||
                         evolutionDropdown.options.Count != expectedEvolutionSpecies))
                    {
                        throw new System.InvalidOperationException(
                            "Evolution dropdown does not contain all discovered species.");
                    }

                    ui.CloseAllPanels();

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

                    if (achievements.GetDefinitions().Count < 12 ||
                        achievements.GetDefinitions().Any(definition =>
                            definition == null ||
                            string.IsNullOrEmpty(definition.id) ||
                            string.IsNullOrEmpty(definition.title) ||
                            string.IsNullOrEmpty(definition.description) ||
                            string.IsNullOrEmpty(definition.condition) ||
                            string.IsNullOrEmpty(definition.reward) ||
                            definition.coinReward <= 0) ||
                        game.GetCreature("cosmo_cat").allowedTimes.Count == 0 ||
                        game.GetCreature("astro_fox").allowedWeather.Count == 0)
                    {
                        throw new System.InvalidOperationException(
                            "Achievements, time restrictions or weather restrictions are incomplete.");
                    }

                    foreach (RectTransform rect in
                             canvas.GetComponentsInChildren<RectTransform>(true))
                    {
                        if ((rect.name == "IntroPanel" || rect.name == "DailyRewardPanel") &&
                            rect.gameObject.activeSelf)
                        {
                            rect.gameObject.SetActive(false);
                        }
                    }

                    ui.CloseAllPanels();

                    ui.OpenBiomes();
                    RectTransform forestBiomeRow =
                        canvas.GetComponentsInChildren<RectTransform>(true)
                            .FirstOrDefault(rect => rect.name == BiomeType.Forest.ToString());
                    Text visibleBiomeProgress = forestBiomeRow != null &&
                                                forestBiomeRow.Find("MainProgress") != null
                        ? forestBiomeRow.Find("MainProgress").GetComponent<Text>()
                        : null;
                    if (visibleBiomeProgress == null ||
                        !visibleBiomeProgress.text.Contains(" / "))
                    {
                        throw new System.InvalidOperationException(
                            "Biome progress is not displayed prominently.");
                    }

                    ui.CloseAllPanels();

                    if (!game.SetCurrentBiome(BiomeType.Forest))
                    {
                        throw new System.InvalidOperationException(
                            "Forest could not be selected for the nest smoke test.");
                    }

                    for (int itemRoll = 0; itemRoll < 64; itemRoll++)
                    {
                        ItemData selectedItem = exploration.SelectItem(false);
                        if (selectedItem != null &&
                            selectedItem.preferredBiome != BiomeType.Forest)
                        {
                            throw new System.InvalidOperationException(
                                "Exploration selected an item from another biome.");
                        }
                    }

                    CreatureNestProgress forestNest;
                    creatureNests.Discover("bread_cat", out forestNest);
                    if (forestNest == null)
                    {
                        throw new System.InvalidOperationException(
                            "A forest nest could not be discovered.");
                    }

                    string futureCooldown = System.DateTime.UtcNow
                        .AddMinutes(5)
                        .ToString(
                            "o",
                            System.Globalization.CultureInfo.InvariantCulture);
                    foreach (CreatureNestProgress entry in
                             creatureNests.GetAllNests())
                    {
                        CreatureNestData entryData =
                            creatureNests.GetNestData(entry.nestId);
                        if (entryData != null &&
                            entryData.biome == BiomeType.Forest)
                        {
                            entry.spawnCooldownUntilUtc = futureCooldown;
                        }
                    }

                    game.SaveCreatureNests(
                        creatureNests.GetAllNests()
                            .Select(entry => entry.Clone())
                            .ToList());
                    creatureNests.Initialize(game, accessoryInventory);
                    forestNest = creatureNests.GetNest(forestNest.nestId);
                    System.DateTime parsedCooldown;
                    if (forestNest == null ||
                        !System.DateTime.TryParse(
                            forestNest.spawnCooldownUntilUtc,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.RoundtripKind,
                            out parsedCooldown) ||
                        parsedCooldown.ToUniversalTime() <=
                            System.DateTime.UtcNow ||
                        !creatureNests.IsBiomeSpawnCooldownActive(
                            BiomeType.Forest))
                    {
                        throw new System.InvalidOperationException(
                            "Forest nest UTC spawn cooldown was not restored.");
                    }

                    CreatureNestData cooldownProbe =
                        ScriptableObject.CreateInstance<CreatureNestData>();
                    cooldownProbe.id = "smoke_forest_nest_after_cooldown";
                    cooldownProbe.speciesId = "bread_cat";
                    cooldownProbe.biome = BiomeType.Forest;
                    if (creatureNests.CanSpawnNest(cooldownProbe))
                    {
                        Object.Destroy(cooldownProbe);
                        throw new System.InvalidOperationException(
                            "A second forest nest could spawn during cooldown.");
                    }

                    string expiredCooldown = System.DateTime.UtcNow
                        .AddMinutes(-1)
                        .ToString(
                            "o",
                            System.Globalization.CultureInfo.InvariantCulture);
                    foreach (CreatureNestProgress entry in
                             creatureNests.GetAllNests())
                    {
                        CreatureNestData entryData =
                            creatureNests.GetNestData(entry.nestId);
                        if (entryData != null &&
                            entryData.biome == BiomeType.Forest)
                        {
                            entry.spawnCooldownUntilUtc = expiredCooldown;
                        }
                    }

                    game.SaveCreatureNests(
                        creatureNests.GetAllNests()
                            .Select(entry => entry.Clone())
                            .ToList());
                    creatureNests.Initialize(game, accessoryInventory);
                    bool canSpawnAfterCooldown =
                        !creatureNests.IsBiomeSpawnCooldownActive(
                            BiomeType.Forest) &&
                        creatureNests.CanSpawnNest(cooldownProbe);
                    Object.Destroy(cooldownProbe);
                    if (!canSpawnAfterCooldown)
                    {
                        throw new System.InvalidOperationException(
                            "Forest nest did not become available after cooldown.");
                    }

                    world.ReloadCurrentBiome();
                    WorldPickup nestMarker = Object.FindObjectsOfType<WorldPickup>(true)
                        .FirstOrDefault(pickup =>
                            pickup != null &&
                            pickup.NestId == forestNest.nestId &&
                            pickup.IsPersistent);
                    if (nestMarker == null || world.PersistentNestCount <= 0)
                    {
                        throw new System.InvalidOperationException(
                            "A discovered nest was not restored as a persistent map marker.");
                    }

                    nestMarker.Interact();
                    if (!ui.NestPanelVisible || !nestMarker.CanInteract ||
                        !nestMarker.gameObject.activeSelf)
                    {
                        throw new System.InvalidOperationException(
                            "The nest panel did not open or the map marker was consumed.");
                    }

                    Text nestTimer = nestPanel.GetComponentsInChildren<Text>(true)
                        .FirstOrDefault(text =>
                            text != null &&
                            text.text.Contains("До следующей награды"));
                    Text nestDescription = nestPanel.GetComponentsInChildren<Text>(true)
                        .FirstOrDefault(text =>
                            text != null &&
                            text.text.Contains("Здесь часто встречаются"));
                    if (nestTimer == null || nestDescription == null)
                    {
                        throw new System.InvalidOperationException(
                            "Nest panel does not show its description and reward timer.");
                    }

                    ui.CloseAllPanels();
                    ui.TogglePause();
                    if (!ui.IsPaused || Time.timeScale != 0f || player.MovementEnabled)
                    {
                        throw new System.InvalidOperationException(
                            "Pause menu did not stop time and player movement.");
                    }

                    ui.TogglePause();
                    if (ui.IsPaused || Time.timeScale <= 0f || !player.MovementEnabled)
                    {
                        throw new System.InvalidOperationException(
                            "Pause menu did not resume the game.");
                    }

                    if (!readiness.RunReleaseReadinessCheck())
                    {
                        throw new System.InvalidOperationException(
                            "Release readiness check failed.");
                    }

                    if (!creatureVisualValidator.ValidateCreatureTemplate() ||
                        string.IsNullOrEmpty(creatureVisualValidator.LastReport) ||
                        !creatureVisualValidator.LastReport.Contains(
                            "CREATURE_TEMPLATE_VALIDATION_PASS"))
                    {
                        throw new System.InvalidOperationException(
                            "Creature visual template validation failed.");
                    }

                    if (!betaReadiness.RunBetaReadinessCheck() ||
                        string.IsNullOrEmpty(betaReadiness.LastReport) ||
                        !betaReadiness.LastReport.Contains("Монстры") ||
                        !betaReadiness.LastReport.Contains("Эволюции") ||
                        !betaReadiness.LastReport.Contains("Гардероб") ||
                        !betaReadiness.LastReport.Contains("Шаблон существ") ||
                        !betaReadiness.LastReport.Contains(
                            "MONSTROLOGY_BETA_READINESS_PASS"))
                    {
                        throw new System.InvalidOperationException(
                            "Beta readiness report failed or is incomplete.");
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

                    if (game.Energy >
                        EnergyRegenerationSystem.MaxEnergy - 3)
                    {
                        game.SpendEnergy(3);
                    }

                    rewardedEnergyBefore = game.Energy;
                    rewardedRequestedAt = Time.unscaledTime;
                    rewardedOpenedAt = Time.realtimeSinceStartup;
                    rewardedAdvertisementOpened = false;
                    rewardedTimeScaleBefore = Time.timeScale;
                    rewardedAudioPauseBefore = AudioListener.pause;
                    rewardedMovementBefore = player.MovementEnabled;
                    ui.ShowRewardedEnergy();
                    if (game.Energy != rewardedEnergyBefore ||
                        yandexBridge.LastRewardId !=
                            YandexGamesBridge.EnergyRewardId)
                    {
                        throw new System.InvalidOperationException(
                            "Rewarded energy was granted early or used a wrong reward ID.");
                    }

                    SessionState.SetString(SessionKey, "reward_wait");
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

            if (stage == "reward_wait" && EditorApplication.isPlaying)
            {
                try
                {
                    GameManager game = Object.FindObjectOfType<GameManager>();
                    UIManager ui = Object.FindObjectOfType<UIManager>();
                    ExplorationSystem exploration =
                        Object.FindObjectOfType<ExplorationSystem>();
                    YandexGamesBridge bridge =
                        Object.FindObjectOfType<YandexGamesBridge>();
                    PlayerController2D player =
                        Object.FindObjectOfType<PlayerController2D>();

                    if (bridge != null &&
                        bridge.IsAdvertisementOpen &&
                        !rewardedAdvertisementOpened)
                    {
                        rewardedAdvertisementOpened = true;
                        rewardedOpenedAt = Time.realtimeSinceStartup;
                    }

                    if (game != null &&
                        game.Energy == rewardedEnergyBefore)
                    {
                        bool stillOpening =
                            !rewardedAdvertisementOpened &&
                            Time.unscaledTime - rewardedRequestedAt < 10f;
                        bool stillShowing =
                            rewardedAdvertisementOpened &&
                            Time.realtimeSinceStartup - rewardedOpenedAt < 5f;
                        if (stillOpening || stillShowing)
                        {
                            return;
                        }
                    }

                    bool rewardGranted =
                        game != null &&
                        game.Energy == rewardedEnergyBefore + 3;
                    bool platformStateRestored =
                        bridge != null &&
                        player != null &&
                        !bridge.IsAdvertisementOpen &&
                        AudioListener.pause == rewardedAudioPauseBefore &&
                        Mathf.Approximately(
                            Time.timeScale,
                            rewardedTimeScaleBefore) &&
                        player.MovementEnabled == rewardedMovementBefore;
                    if (rewardGranted &&
                        (bridge == null ||
                         bridge.IsRewardedRequestPending ||
                         !platformStateRestored) &&
                        Time.realtimeSinceStartup - rewardedOpenedAt < 10f)
                    {
                        return;
                    }

                    if (game == null || ui == null || exploration == null ||
                        bridge == null || player == null ||
                        !rewardGranted ||
                        !platformStateRestored)
                    {
                        throw new System.InvalidOperationException(
                            "PluginYG2 rewarded callback or ad-state restoration failed.");
                    }

                    ui.TogglePause();
                    if (!ui.IsPaused ||
                        Time.timeScale != 0f ||
                        player.MovementEnabled)
                    {
                        throw new System.InvalidOperationException(
                            "Pause menu did not activate before the rewarded ad.");
                    }

                    if (game.Energy >
                        EnergyRegenerationSystem.MaxEnergy - 3)
                    {
                        game.SpendEnergy(3);
                    }

                    rewardedEnergyBefore = game.Energy;
                    rewardedRequestedAt = Time.unscaledTime;
                    rewardedOpenedAt = Time.realtimeSinceStartup;
                    rewardedAdvertisementOpened = false;
                    rewardedTimeScaleBefore = Time.timeScale;
                    rewardedAudioPauseBefore = AudioListener.pause;
                    rewardedMovementBefore = player.MovementEnabled;
                    ui.ShowRewardedEnergy();
                    if (game.Energy != rewardedEnergyBefore ||
                        bridge.LastRewardId !=
                            YandexGamesBridge.EnergyRewardId)
                    {
                        throw new System.InvalidOperationException(
                            "Paused rewarded request granted early or used a wrong ID.");
                    }

                    SessionState.SetString(
                        SessionKey,
                        "paused_reward_wait");
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

            if (stage == "paused_reward_wait" &&
                EditorApplication.isPlaying)
            {
                try
                {
                    GameManager game = Object.FindObjectOfType<GameManager>();
                    UIManager ui = Object.FindObjectOfType<UIManager>();
                    ExplorationSystem exploration =
                        Object.FindObjectOfType<ExplorationSystem>();
                    YandexGamesBridge bridge =
                        Object.FindObjectOfType<YandexGamesBridge>();
                    PlayerController2D player =
                        Object.FindObjectOfType<PlayerController2D>();

                    if (bridge != null &&
                        bridge.IsAdvertisementOpen &&
                        !rewardedAdvertisementOpened)
                    {
                        rewardedAdvertisementOpened = true;
                        rewardedOpenedAt = Time.realtimeSinceStartup;
                    }

                    if (game != null &&
                        game.Energy == rewardedEnergyBefore)
                    {
                        bool stillOpening =
                            !rewardedAdvertisementOpened &&
                            Time.unscaledTime - rewardedRequestedAt < 10f;
                        bool stillShowing =
                            rewardedAdvertisementOpened &&
                            Time.realtimeSinceStartup - rewardedOpenedAt < 5f;
                        if (stillOpening || stillShowing)
                        {
                            return;
                        }
                    }

                    bool rewardGranted =
                        game != null &&
                        game.Energy == rewardedEnergyBefore + 3;
                    bool userPausePreserved =
                        bridge != null &&
                        ui != null &&
                        player != null &&
                        !bridge.IsAdvertisementOpen &&
                        ui.IsPaused &&
                        AudioListener.pause == rewardedAudioPauseBefore &&
                        Mathf.Approximately(
                            Time.timeScale,
                            rewardedTimeScaleBefore) &&
                        player.MovementEnabled == rewardedMovementBefore;
                    if (rewardGranted &&
                        (bridge == null ||
                         bridge.IsRewardedRequestPending ||
                         !userPausePreserved) &&
                        Time.realtimeSinceStartup - rewardedOpenedAt < 10f)
                    {
                        return;
                    }

                    if (game == null || ui == null || exploration == null ||
                        bridge == null || player == null ||
                        !rewardGranted ||
                        !userPausePreserved)
                    {
                        throw new System.InvalidOperationException(
                            "Rewarded ad resumed a user-paused game.");
                    }

                    ui.TogglePause();
                    if (ui.IsPaused ||
                        Time.timeScale <= 0f ||
                        !player.MovementEnabled)
                    {
                        throw new System.InvalidOperationException(
                            "User pause could not be closed after the rewarded ad.");
                    }

                    exploration.ResolveWorldDiscovery(
                        ExplorationResultType.Nothing);
                    if (!ui.ResultCardVisible)
                    {
                        throw new System.InvalidOperationException(
                            "Discovery result card did not become visible.");
                    }

                    SessionState.SetFloat(ResultShownAtKey, Time.unscaledTime);
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
                if (Time.unscaledTime - shownAt < 5f)
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
