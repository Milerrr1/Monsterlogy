using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class FirstSessionDirector : MonoBehaviour
    {
        public static FirstSessionDirector Instance { get; private set; }

        private GameManager game;
        private ExplorationSystem exploration;
        private CreatureCollectionManager collection;
        private AccessoryInventoryManager wardrobe;
        private FirstHourBalanceConfig config;
        private float activePlaySeconds;
        private float lastNewContentSeconds;
        private float lastWardrobeSeconds;
        private int completedFindings;
        private int emptyStreak;
        private int sameResourceStreak;
        private string lastResourceId;
        private string lastAccessoryId;
        private bool lastCreatureWasRare;
        private bool rareHintSeen;

        public float ActivePlaySeconds { get { return activePlaySeconds; } }
        public int EmptyStreak { get { return emptyStreak; } }
        public int SameResourceStreak { get { return sameResourceStreak; } }
        public FirstHourBalanceConfig Config { get { return config; } }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (exploration != null)
            {
                exploration.ExplorationCompleted -= RecordResult;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!CanCountActiveTime())
            {
                return;
            }

            activePlaySeconds += Time.unscaledDeltaTime;
        }

        public void Initialize(
            GameManager gameManager,
            ExplorationSystem explorationSystem,
            CreatureCollectionManager collectionManager,
            AccessoryInventoryManager wardrobeManager,
            FirstHourBalanceConfig balanceConfig)
        {
            if (exploration != null)
            {
                exploration.ExplorationCompleted -= RecordResult;
            }

            game = gameManager;
            exploration = explorationSystem;
            collection = collectionManager;
            wardrobe = wardrobeManager;
            config = balanceConfig != null
                ? balanceConfig
                : FirstHourBalanceConfig.LoadOrCreate();
            lastNewContentSeconds = activePlaySeconds;
            lastWardrobeSeconds = activePlaySeconds;
            if (exploration != null)
            {
                exploration.ExplorationCompleted += RecordResult;
                exploration.SetFirstSessionDirector(this);
            }
        }

        public WorldPickupType SelectPickupType(float roll01)
        {
            FirstHourPhaseWeights phase = config.GetPhase(activePlaySeconds);
            float creature = config.creatureWeight * phase.creature;
            float trace = config.traceWeight * phase.trace;
            float resource = config.resourceWeight * phase.resource;
            float egg = config.eggWeight * phase.egg;
            float accessory = config.accessoryWeight * phase.accessory;
            float empty = config.emptyWeight * phase.empty;

            if (completedFindings < config.protectedOpeningFindings ||
                emptyStreak >= config.maxEmptyStreak)
            {
                empty = 0f;
            }

            if (activePlaySeconds - lastNewContentSeconds >=
                config.newContentPityMinutes * 60f)
            {
                creature *= config.pityMultiplier * phase.newContent;
                accessory *= config.pityMultiplier;
            }

            if (activePlaySeconds - lastWardrobeSeconds >=
                config.wardrobePityMinutes * 60f)
            {
                accessory *= config.pityMultiplier;
            }

            if (wardrobe != null &&
                wardrobe.GetOwnedAccessoryIds().Count == 0 &&
                activePlaySeconds >= config.phaseOneEndMinutes * 60f)
            {
                accessory *= phase.newContent;
            }

            if (wardrobe != null &&
                activePlaySeconds < config.firstHourEndMinutes * 60f &&
                wardrobe.GetOwnedAccessoryIds().Count >=
                    config.firstHourWardrobeSoftCap)
            {
                accessory *=
                    config.wardrobeAfterSoftCapMultiplier;
            }

            if (!rareHintSeen &&
                activePlaySeconds >= config.rareHintPityMinutes * 60f)
            {
                trace *= config.pityMultiplier;
            }

            float total = creature + trace + resource + egg +
                          accessory + empty;
            if (total <= 0.0001f)
            {
                return WorldPickupType.Trace;
            }

            float selection = Mathf.Clamp01(roll01) * total;
            selection -= trace;
            if (selection <= 0f)
            {
                return WorldPickupType.Trace;
            }

            selection -= resource;
            if (selection <= 0f)
            {
                return WorldPickupType.Item;
            }

            selection -= egg;
            if (selection <= 0f)
            {
                return WorldPickupType.Egg;
            }

            selection -= accessory;
            if (selection <= 0f)
            {
                return WorldPickupType.Accessory;
            }

            selection -= creature;
            return selection <= 0f
                ? WorldPickupType.Creature
                : WorldPickupType.Nothing;
        }

        public float GetCreatureWeightMultiplier(CreatureData creature)
        {
            if (creature == null)
            {
                return 0f;
            }

            bool rare = creature.rarity != CreatureRarity.Common;
            FirstHourPhaseWeights phase = config.GetPhase(activePlaySeconds);
            if (rare)
            {
                if (activePlaySeconds < config.rareLockMinutes * 60f ||
                    lastCreatureWasRare)
                {
                    return 0f;
                }

                return phase.rareCreature;
            }

            string neededRoot = GetNearEvolutionRoot();
            if (!string.IsNullOrEmpty(neededRoot) &&
                game.GetEvolutionRootSpeciesId(creature.id) == neededRoot)
            {
                return config.nearEvolutionCreatureMultiplier;
            }

            return 1f;
        }

        public ItemData ChooseItem(List<ItemData> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            List<ItemData> filtered = candidates;
            if (sameResourceStreak >= config.maxSameResourceStreak &&
                !string.IsNullOrEmpty(lastResourceId))
            {
                List<ItemData> alternatives = candidates.FindAll(item =>
                    item != null && item.id != lastResourceId);
                if (alternatives.Count > 0)
                {
                    filtered = alternatives;
                }
            }

            return filtered[UnityEngine.Random.Range(0, filtered.Count)];
        }

        public int ChooseAccessoryIndex(
            IList<AccessoryData> candidates,
            IList<float> weights)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return -1;
            }

            float total = 0f;
            for (int index = 0; index < candidates.Count; index++)
            {
                float weight = index < weights.Count
                    ? Mathf.Max(0f, weights[index])
                    : 1f;
                AccessoryData candidate = candidates[index];
                if (candidate != null &&
                    candidate.id == lastAccessoryId &&
                    candidates.Count > 1)
                {
                    weight = 0f;
                }

                total += weight;
            }

            if (total <= 0.0001f)
            {
                return UnityEngine.Random.Range(0, candidates.Count);
            }

            float roll = UnityEngine.Random.value * total;
            for (int index = 0; index < candidates.Count; index++)
            {
                float weight = index < weights.Count
                    ? Mathf.Max(0f, weights[index])
                    : 1f;
                AccessoryData candidate = candidates[index];
                if (candidate != null &&
                    candidate.id == lastAccessoryId &&
                    candidates.Count > 1)
                {
                    weight = 0f;
                }

                roll -= weight;
                if (roll <= 0f)
                {
                    return index;
                }
            }

            return candidates.Count - 1;
        }

        public void RecordOutcomeForSimulation(ExplorationResult result)
        {
            RecordResult(result);
        }

        private void RecordResult(ExplorationResult result)
        {
            if (result == null)
            {
                return;
            }

            completedFindings++;
            if (result.type == ExplorationResultType.Nothing)
            {
                emptyStreak++;
            }
            else
            {
                emptyStreak = 0;
            }

            if (result.item != null)
            {
                if (result.item.id == lastResourceId)
                {
                    sameResourceStreak++;
                }
                else
                {
                    lastResourceId = result.item.id;
                    sameResourceStreak = 1;
                }
            }
            else
            {
                sameResourceStreak = 0;
                lastResourceId = string.Empty;
            }

            if (result.accessory != null)
            {
                lastAccessoryId = result.accessory.id;
                lastWardrobeSeconds = activePlaySeconds;
                lastNewContentSeconds = activePlaySeconds;
            }

            if (result.creature != null)
            {
                lastCreatureWasRare =
                    result.creature.rarity != CreatureRarity.Common;
                if (result.firstSpeciesDiscovery)
                {
                    lastNewContentSeconds = activePlaySeconds;
                }
            }
            else
            {
                lastCreatureWasRare = false;
            }

            if (result.type == ExplorationResultType.Track &&
                activePlaySeconds >= config.rareLockMinutes * 60f)
            {
                rareHintSeen = true;
            }
        }

        private string GetNearEvolutionRoot()
        {
            if (game == null || collection == null)
            {
                return string.Empty;
            }

            foreach (CreatureInstance pet in collection.GetAllPets())
            {
                if (pet == null)
                {
                    continue;
                }

                string root = string.IsNullOrEmpty(
                    pet.evolutionRootSpeciesId)
                    ? game.GetEvolutionRootSpeciesId(pet.speciesId)
                    : pet.evolutionRootSpeciesId;
                int copies = game.GetLifetimeCreatureCount(root);
                if (copies >= config.nearEvolutionMinimumCopies &&
                    copies < EvolutionProgressUtility
                        .GetThresholdForTransition(0))
                {
                    return root;
                }
            }

            return string.Empty;
        }

        private static bool CanCountActiveTime()
        {
            if (!Application.isFocused || Time.timeScale <= 0f)
            {
                return false;
            }

            YandexGamesBridge bridge = YandexGamesBridge.Instance;
            return bridge == null ||
                   (bridge.GameplayIsActive &&
                    !bridge.IsAdvertisementOpen);
        }
    }
}
