using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public enum TrackType
    {
        Paws,
        Fur,
        EggShell,
        StrangeSound,
        ShinyStone,
        Lair
    }

    [Serializable]
    public class GameContent
    {
        public List<CreatureData> creatures = new List<CreatureData>();
        public List<BiomeData> biomes = new List<BiomeData>();
        public List<ItemData> items = new List<ItemData>();
        public List<AccessoryData> accessories = new List<AccessoryData>();
        public List<QuestData> quests = new List<QuestData>();
        public List<MutationRecipe> mutationRecipes = new List<MutationRecipe>();
        public List<BreedingRecipeData> breedingRecipes = new List<BreedingRecipeData>();
        public List<SpeciesEvolutionData> speciesEvolutions = new List<SpeciesEvolutionData>();
        public List<SignatureSetData> signatureSets = new List<SignatureSetData>();
        public List<BiomeEventData> biomeEvents = new List<BiomeEventData>();
        public List<CreatureNestData> creatureNests = new List<CreatureNestData>();
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameContent content = new GameContent();

        public event Action StateChanged;
        public event Action<string> NotificationRaised;
        public event Action ExplorationRegistered;
        public event Action<CreatureData, bool> CreatureRegistered;
        public event Action<BiomeData> BiomeUnlocked;
        public event Action ProgressReset;

        public GameContent Content { get { return content; } }
        public int Coins { get { return progress.coins; } }
        public int Energy { get { return progress.energy; } }
        public float StarterBoostPlaySeconds { get { return progress.starterBoostPlaySeconds; } }
        public bool IsStarterBoostActive { get { return progress.starterBoostPlaySeconds < 15f * 60f; } }
        public int ExplorationCount { get { return progress.explorationCount; } }
        public int MutationCount { get { return progress.mutationCount; } }
        public int TotalCreaturesFound { get { return progress.totalCreaturesFound; } }
        public bool IntroCompleted { get { return progress.introCompleted; } }
        public string LastDailyRewardUtcDate { get { return progress.lastDailyRewardUtcDate; } }
        public int DailyRewardStreak { get { return Mathf.Clamp(progress.dailyRewardStreak, 0, 7); } }
        public bool DailyRewardFirstLaunchRegistered
        {
            get { return progress.dailyRewardFirstLaunchRegistered; }
        }
        public string DailyRewardFirstLaunchUtcDate
        {
            get { return progress.dailyRewardFirstLaunchUtcDate; }
        }
        public int PetMigrationVersion
        {
            get { return Mathf.Max(0, progress.petMigrationVersion); }
        }
        public TimeOfDay CurrentTimeOfDay { get { return ParseTimeOfDay(progress.timeOfDay); } }
        public WeatherType CurrentWeather { get { return ParseWeather(progress.weather); } }
        public float WorldTime01 { get { return Mathf.Repeat(progress.worldTime01, 1f); } }
        public float WeatherTimer { get { return Mathf.Max(0f, progress.weatherTimer); } }
        public BiomeData CurrentBiome { get; private set; }

        private GameProgress progress;
        private readonly Dictionary<string, int> creatureCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> lifetimeCreatureCounts =
            new Dictionary<string, int>();
        private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> trackCounts = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(GameContent gameContent)
        {
            content = gameContent ?? new GameContent();
            EnsureContentDefaults();
            progress = SaveSystem.Load();
            EnsureProgressDefaults();
            ReadEntries(progress.creatures, creatureCounts);
            ReadEntries(progress.lifetimeCreatures, lifetimeCreatureCounts);
            ReadEntries(progress.items, itemCounts);
            ReadEntries(progress.tracks, trackCounts);
            MigrateLifetimeCreatureCounts();
            foreach (KeyValuePair<string, int> entry in creatureCounts)
            {
                if (entry.Value > 0 && !progress.discoveredSpecies.Contains(entry.Key))
                {
                    progress.discoveredSpecies.Add(entry.Key);
                }
            }

            CurrentBiome = GetBiome(ParseBiome(progress.currentBiome));

            if (CurrentBiome == null && content.biomes.Count > 0)
            {
                CurrentBiome = content.biomes[0];
            }

            NotifyStateChanged();
        }

        public bool SpendEnergy(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (progress.energy < amount)
            {
                RaiseNotification("Недостаточно энергии.");
                return false;
            }

            bool spentFromFullEnergy = progress.energy >= EnergyRegenerationSystem.MaxEnergy;
            progress.energy -= amount;
            if (spentFromFullEnergy)
            {
                progress.lastEnergyUtc = DateTime.UtcNow.ToString(
                    "o",
                    CultureInfo.InvariantCulture);
            }

            Save();
            return true;
        }

        public void AddEnergy(int amount)
        {
            progress.energy = Mathf.Clamp(
                progress.energy + amount,
                0,
                EnergyRegenerationSystem.MaxEnergy);
            Save();
        }

        public string GetEnergyClockUtc()
        {
            return progress.lastEnergyUtc;
        }

        public void ApplyEnergyRegeneration(int amount, DateTime clockUtc)
        {
            progress.energy = Mathf.Clamp(
                progress.energy + Mathf.Max(0, amount),
                0,
                EnergyRegenerationSystem.MaxEnergy);
            progress.lastEnergyUtc = clockUtc.ToUniversalTime().ToString(
                "o",
                CultureInfo.InvariantCulture);
            Save();
        }

        public void SaveStarterBoost(float playedSeconds, int rewardIndex)
        {
            progress.starterBoostPlaySeconds = Mathf.Clamp(playedSeconds, 0f, 15f * 60f);
            progress.starterBoostRewardIndex = Mathf.Max(0, rewardIndex);
            Save();
        }

        public int GetStarterBoostRewardIndex()
        {
            return Mathf.Max(0, progress.starterBoostRewardIndex);
        }

        public void AddCoins(int amount)
        {
            progress.coins = Mathf.Max(0, progress.coins + amount);
            Save();
        }

        public bool CompleteIntro(int energyReward, int coinReward)
        {
            if (progress.introCompleted)
            {
                return false;
            }

            progress.introCompleted = true;
            progress.energy = Mathf.Clamp(
                progress.energy + Mathf.Max(0, energyReward),
                0,
                EnergyRegenerationSystem.MaxEnergy);
            progress.coins = Mathf.Max(0, progress.coins + Mathf.Max(0, coinReward));
            Save();
            return true;
        }

        public void SaveDailyReward(string utcDate, int streakDay)
        {
            progress.lastDailyRewardUtcDate = utcDate ?? string.Empty;
            progress.dailyRewardStreak = Mathf.Clamp(streakDay, 0, 7);
            Save();
        }

        public void RegisterDailyRewardFirstLaunch(string utcDate)
        {
            if (progress.dailyRewardFirstLaunchRegistered)
            {
                return;
            }

            progress.dailyRewardFirstLaunchRegistered = true;
            progress.dailyRewardFirstLaunchUtcDate = utcDate ?? string.Empty;
            Save();
        }

        public void MarkPetMigrationComplete(int migrationVersion)
        {
            int normalized = Mathf.Max(0, migrationVersion);
            if (progress.petMigrationVersion >= normalized)
            {
                return;
            }

            progress.petMigrationVersion = normalized;
            Save();
        }

        public void SaveNow()
        {
            Save();
        }

        public void RegisterExploration()
        {
            progress.explorationCount++;
            Save();
            if (ExplorationRegistered != null)
            {
                ExplorationRegistered();
            }
        }

        public bool AddCreature(CreatureData creature)
        {
            if (creature == null || string.IsNullOrEmpty(creature.id))
            {
                return false;
            }

            bool firstDiscovery = !IsCreatureFound(creature.id);
            if (firstDiscovery)
            {
                progress.discoveredSpecies.Add(creature.id);
            }

            creatureCounts[creature.id] = GetCreatureCount(creature.id) + 1;
            lifetimeCreatureCounts[creature.id] =
                GetLifetimeCreatureCount(creature.id) + 1;
            progress.totalCreaturesFound++;
            int reward = GetDiscoveryReward(creature.rarity, firstDiscovery);
            progress.coins += reward;
            Save();
            if (CreatureRegistered != null)
            {
                CreatureRegistered(creature, firstDiscovery);
            }

            return firstDiscovery;
        }

        public void AddItem(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount == 0)
            {
                return;
            }

            itemCounts[itemId] = Mathf.Max(0, GetItemCount(itemId) + amount);
            Save();
        }

        public void AddCreatureCopies(string creatureId, int amount)
        {
            CreatureData creature = GetCreature(creatureId);
            if (string.IsNullOrEmpty(creatureId) || amount <= 0 || creature == null)
            {
                return;
            }

            bool firstDiscovery = !IsCreatureFound(creatureId);
            creatureCounts[creatureId] = GetCreatureCount(creatureId) + amount;
            lifetimeCreatureCounts[creatureId] =
                GetLifetimeCreatureCount(creatureId) + amount;
            if (firstDiscovery)
            {
                progress.discoveredSpecies.Add(creatureId);
            }

            progress.totalCreaturesFound += amount;
            Save();
            if (CreatureRegistered != null)
            {
                CreatureRegistered(creature, firstDiscovery);
            }
        }

        public bool ConsumeCreatureCopies(string creatureId, int amount)
        {
            if (string.IsNullOrEmpty(creatureId) || amount <= 0 ||
                GetCreatureCount(creatureId) < amount)
            {
                return false;
            }

            creatureCounts[creatureId] -= amount;
            Save();
            return true;
        }

        public bool ConsumeItem(string itemId, int amount = 1)
        {
            if (GetItemCount(itemId) < amount)
            {
                return false;
            }

            itemCounts[itemId] -= amount;
            Save();
            return true;
        }

        public void AddTrack(TrackType trackType)
        {
            string key = trackType.ToString();
            trackCounts[key] = GetTrackCount(trackType) + 1;
            Save();
        }

        public void RegisterMutation()
        {
            progress.mutationCount++;
            Save();
        }

        public int GetCreatureCount(string creatureId)
        {
            int value;
            return creatureCounts.TryGetValue(creatureId, out value) ? value : 0;
        }

        public int GetLifetimeCreatureCount(string creatureId)
        {
            int value;
            return lifetimeCreatureCounts.TryGetValue(creatureId, out value)
                ? value
                : 0;
        }

        public string GetEvolutionRootSpeciesId(string speciesId)
        {
            return EvolutionProgressUtility.GetRootSpeciesId(
                content,
                speciesId);
        }

        public int GetItemCount(string itemId)
        {
            int value;
            return itemCounts.TryGetValue(itemId, out value) ? value : 0;
        }

        public int GetTrackCount(TrackType trackType)
        {
            int value;
            return trackCounts.TryGetValue(trackType.ToString(), out value) ? value : 0;
        }

        public int GetLargestTrackCount()
        {
            return trackCounts.Count == 0 ? 0 : trackCounts.Values.Max();
        }

        public bool IsCreatureFound(string creatureId)
        {
            return progress.discoveredSpecies.Contains(creatureId);
        }

        public bool IsBiomeUnlocked(BiomeType biomeType)
        {
            return progress.unlockedBiomes.Contains(biomeType.ToString());
        }

        public bool SetCurrentBiome(BiomeType biomeType)
        {
            if (!IsBiomeUnlocked(biomeType))
            {
                RaiseNotification("Сначала откройте этот биом.");
                return false;
            }

            BiomeData biome = GetBiome(biomeType);
            if (biome == null)
            {
                return false;
            }

            CurrentBiome = biome;
            progress.currentBiome = biomeType.ToString();
            Save();
            return true;
        }

        public bool TryUnlockBiome(BiomeData biome)
        {
            if (biome == null)
            {
                return false;
            }

            if (IsBiomeUnlocked(biome.type))
            {
                return SetCurrentBiome(biome.type);
            }

            if (progress.coins < biome.unlockPrice)
            {
                RaiseNotification("Нужно ещё монет: " + (biome.unlockPrice - progress.coins));
                return false;
            }

            progress.coins -= biome.unlockPrice;
            progress.unlockedBiomes.Add(biome.type.ToString());
            CurrentBiome = biome;
            progress.currentBiome = biome.type.ToString();
            RaiseNotification("Открыт биом: " + biome.biomeName);
            Save();
            if (BiomeUnlocked != null)
            {
                BiomeUnlocked(biome);
            }

            return true;
        }

        public bool AreAppearanceConditionsMet(CreatureData creature)
        {
            if (creature == null)
            {
                return false;
            }

            if (creature.allowedTimes != null && creature.allowedTimes.Count > 0 &&
                !creature.allowedTimes.Contains(CurrentTimeOfDay))
            {
                return false;
            }

            if (creature.allowedWeather != null && creature.allowedWeather.Count > 0 &&
                !creature.allowedWeather.Contains(CurrentWeather))
            {
                return false;
            }

            foreach (AppearanceCondition condition in creature.appearanceConditions)
            {
                if (!IsConditionMet(condition))
                {
                    return false;
                }
            }

            return true;
        }

        public int CountFoundCreaturesOfElement(CreatureElement element)
        {
            return content.creatures.Count(creature =>
                creature.element == element && IsCreatureFound(creature.id));
        }

        public int GetBiomeCreatureTotal(BiomeType biome)
        {
            BiomeData data = GetBiome(biome);
            return data == null || data.availableCreatures == null
                ? 0
                : data.availableCreatures
                    .Where(creature => creature != null && creature.appearanceChance > 0.001f)
                    .Select(creature => creature.id)
                    .Distinct()
                    .Count();
        }

        public int GetBiomeCreatureFound(BiomeType biome)
        {
            BiomeData data = GetBiome(biome);
            return data == null || data.availableCreatures == null
                ? 0
                : data.availableCreatures
                    .Where(creature => creature != null &&
                                       creature.appearanceChance > 0.001f &&
                                       IsCreatureFound(creature.id))
                    .Select(creature => creature.id)
                    .Distinct()
                    .Count();
        }

        public bool IsBiomeEncyclopediaComplete(BiomeType biome)
        {
            int total = GetBiomeCreatureTotal(biome);
            return total > 0 && GetBiomeCreatureFound(biome) >= total;
        }

        public int GetUnlockedBiomeCount()
        {
            return content.biomes.Count(biome =>
                biome != null && IsBiomeUnlocked(biome.type));
        }

        public int GetDiscoveredSpeciesCount()
        {
            return content.creatures.Count(creature =>
                creature != null && IsCreatureFound(creature.id));
        }

        public float GetEncyclopediaCompletion01()
        {
            int total = content.creatures.Count(creature =>
                creature != null && !string.IsNullOrEmpty(creature.id));
            return total > 0
                ? Mathf.Clamp01((float)GetDiscoveredSpeciesCount() / total)
                : 0f;
        }

        public CreatureData GetCreature(string id)
        {
            return content.creatures.Find(creature => creature != null && creature.id == id);
        }

        public ItemData GetItem(string id)
        {
            return content.items.Find(item => item != null && item.id == id);
        }

        public AccessoryData GetAccessory(string id)
        {
            return content.accessories.Find(accessory => accessory != null && accessory.id == id);
        }

        public SpeciesEvolutionData GetEvolution(string speciesId)
        {
            return content.speciesEvolutions.Find(evolution =>
                evolution != null && evolution.baseSpeciesId == speciesId);
        }

        public BiomeData GetBiome(BiomeType type)
        {
            return content.biomes.Find(biome => biome != null && biome.type == type);
        }

        public List<CreatureInstance> GetSavedPets()
        {
            return progress != null && progress.pets != null
                ? new List<CreatureInstance>(progress.pets)
                : new List<CreatureInstance>();
        }

        public void SavePets(IList<CreatureInstance> pets)
        {
            progress.pets = pets != null
                ? new List<CreatureInstance>(pets)
                : new List<CreatureInstance>();
            Save();
        }

        public List<string> GetSavedAccessories()
        {
            return progress != null && progress.accessories != null
                ? new List<string>(progress.accessories)
                : new List<string>();
        }

        public void SaveAccessories(IList<string> accessoryIds)
        {
            progress.accessories = accessoryIds != null
                ? new List<string>(accessoryIds)
                : new List<string>();
            Save();
        }

        public List<CreatureNestProgress> GetSavedCreatureNests()
        {
            return progress.creatureNests != null
                ? progress.creatureNests
                    .Where(entry => entry != null)
                    .Select(entry => entry.Clone())
                    .ToList()
                : new List<CreatureNestProgress>();
        }

        public void SaveCreatureNests(IList<CreatureNestProgress> nests)
        {
            progress.creatureNests = nests != null
                ? nests.Select(entry => entry != null ? entry.Clone() : null)
                    .Where(entry => entry != null)
                    .ToList()
                : new List<CreatureNestProgress>();
            Save();
        }

        public bool CompleteSignatureSet(string setId)
        {
            if (string.IsNullOrEmpty(setId) || progress.completedSignatureSets.Contains(setId))
            {
                return false;
            }

            progress.completedSignatureSets.Add(setId);
            Save();
            return true;
        }

        public bool IsSignatureSetCompleted(string setId)
        {
            return !string.IsNullOrEmpty(setId) &&
                   progress.completedSignatureSets.Contains(setId);
        }

        public IReadOnlyList<string> GetCompletedSignatureSets()
        {
            return progress.completedSignatureSets.AsReadOnly();
        }

        public void SaveActiveSignatureBonuses(IList<string> bonusIds)
        {
            progress.activeSignatureBonuses = bonusIds != null
                ? bonusIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList()
                : new List<string>();
            Save();
        }

        public IReadOnlyList<string> GetActiveSignatureBonuses()
        {
            return progress.activeSignatureBonuses.AsReadOnly();
        }

        public bool RegisterBiomeEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || progress.foundBiomeEvents.Contains(eventId))
            {
                return false;
            }

            progress.foundBiomeEvents.Add(eventId);
            Save();
            return true;
        }

        public IReadOnlyList<string> GetFoundBiomeEvents()
        {
            return progress.foundBiomeEvents.AsReadOnly();
        }

        public IReadOnlyList<string> GetUnlockedAchievements()
        {
            return progress.unlockedAchievements.AsReadOnly();
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            return !string.IsNullOrEmpty(achievementId) &&
                   progress.unlockedAchievements.Contains(achievementId);
        }

        public bool UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) || IsAchievementUnlocked(achievementId))
            {
                return false;
            }

            progress.unlockedAchievements.Add(achievementId);
            Save();
            return true;
        }

        public void SaveWorldEnvironment(
            TimeOfDay timeOfDay,
            WeatherType weather,
            float worldTime01,
            float weatherTimer)
        {
            progress.timeOfDay = timeOfDay.ToString();
            progress.weather = weather.ToString();
            progress.worldTime01 = Mathf.Repeat(worldTime01, 1f);
            progress.weatherTimer = Mathf.Max(0f, weatherTimer);
            Save();
        }

        public bool IsQuestClaimed(string questId)
        {
            return progress.claimedQuests.Contains(questId);
        }

        public bool IsHintPurchased(string creatureId)
        {
            return GetHintLevel(creatureId) > 0;
        }

        public bool TryBuyHint(string creatureId, int price)
        {
            return TryBuyNextHint(creatureId);
        }

        public int GetHintLevel(string creatureId)
        {
            StringIntEntry entry = progress.hintLevels.Find(item =>
                item != null && item.id == creatureId);
            return entry != null ? Mathf.Clamp(entry.value, 0, 4) : 0;
        }

        public int GetPurchasedHintCount()
        {
            return progress.hintLevels.Sum(entry =>
                entry != null ? Mathf.Clamp(entry.value, 0, 4) : 0);
        }

        public int GetNextHintCost(CreatureData creature)
        {
            return creature == null
                ? 0
                : CalculateHintCost(
                    creature.rarity,
                    GetPurchasedHintCount() + 1);
        }

        public static int CalculateHintCost(
            CreatureRarity rarity,
            int purchaseNumber)
        {
            int[] baseCosts = { 50, 150, 400, 1000, 2500, 5000 };
            int index = Mathf.Max(1, purchaseNumber) - 1;
            int baseCost = index < baseCosts.Length
                ? baseCosts[index]
                : baseCosts[baseCosts.Length - 1] +
                  (index - baseCosts.Length + 1) * 1500;
            float rarityMultiplier;
            switch (rarity)
            {
                case CreatureRarity.Rare:
                    rarityMultiplier = 1.25f;
                    break;
                case CreatureRarity.Epic:
                    rarityMultiplier = 1.5f;
                    break;
                case CreatureRarity.Legendary:
                    rarityMultiplier = 2f;
                    break;
                case CreatureRarity.Secret:
                    rarityMultiplier = 2.5f;
                    break;
                default:
                    rarityMultiplier = 1f;
                    break;
            }

            return Mathf.CeilToInt(baseCost * rarityMultiplier / 10f) * 10;
        }

        public bool TryBuyNextHint(string creatureId)
        {
            CreatureData creature = GetCreature(creatureId);
            int level = GetHintLevel(creatureId);
            if (creature == null || level >= 4)
            {
                return false;
            }

            int price = GetNextHintCost(creature);
            if (progress.coins < price)
            {
                RaiseNotification("Недостаточно монет для подсказки.");
                return false;
            }

            progress.coins -= price;
            StringIntEntry entry = progress.hintLevels.Find(item =>
                item != null && item.id == creatureId);
            if (entry == null)
            {
                entry = new StringIntEntry(creatureId, 0);
                progress.hintLevels.Add(entry);
            }

            entry.value = Mathf.Clamp(level + 1, 0, 4);
            if (!progress.purchasedHints.Contains(creatureId))
            {
                progress.purchasedHints.Add(creatureId);
            }

            Save();
            RaiseNotification(
                "Открыт уровень подсказки " + entry.value + "/4 за " +
                price + " монет.");
            return true;
        }

        public void ClaimQuest(QuestData quest)
        {
            if (quest == null || IsQuestClaimed(quest.id))
            {
                return;
            }

            progress.claimedQuests.Add(quest.id);
            int coinReward = IsStarterBoostActive ? quest.rewardCoins * 2 : quest.rewardCoins;
            int energyReward = quest.rewardEnergy + (IsStarterBoostActive ? 2 : 0);
            progress.coins += coinReward;
            progress.energy = Mathf.Clamp(
                progress.energy + energyReward,
                0,
                EnergyRegenerationSystem.MaxEnergy);
            RaiseNotification("Награда получена: +" + coinReward + " монет, +" +
                              energyReward + " энергии" +
                              (IsStarterBoostActive ? " • Буст новичка" : ""));
            Save();
        }

        public string GetPseudoOnlineText(CreatureData creature)
        {
            uint hash = StableHash(creature == null ? "unknown" : creature.id);
            if ((hash & 1u) == 0u)
            {
                float percent = 0.1f + (hash % 879u) / 100f;
                return percent.ToString("0.0") + "% игроков нашли этого монстра";
            }

            int count = 120 + (int)(hash % 4881u);
            return "Сегодня найдено " + count + " существ этого вида";
        }

        public string GetTrackSummary()
        {
            return "Лапы " + GetTrackCount(TrackType.Paws) +
                   "  |  Шерсть " + GetTrackCount(TrackType.Fur) +
                   "  |  Скорлупа " + GetTrackCount(TrackType.EggShell) +
                   "  |  Звуки " + GetTrackCount(TrackType.StrangeSound) +
                   "  |  Логова " + GetTrackCount(TrackType.Lair);
        }

        public void ResetProgress()
        {
            SaveSystem.Delete();
            progress = SaveSystem.Load();
            EnsureProgressDefaults();
            creatureCounts.Clear();
            lifetimeCreatureCounts.Clear();
            itemCounts.Clear();
            trackCounts.Clear();
            CurrentBiome = GetBiome(BiomeType.Forest);
            NotifyStateChanged();
            if (ProgressReset != null)
            {
                ProgressReset();
            }
        }

        public void RaiseNotification(string message)
        {
            if (NotificationRaised != null)
            {
                NotificationRaised(message);
            }
        }

        private bool IsConditionMet(AppearanceCondition condition)
        {
            if (condition == null)
            {
                return true;
            }

            switch (condition.type)
            {
                case AppearanceConditionType.CurrentBiome:
                    return CurrentBiome != null && CurrentBiome.type == condition.requiredBiome;
                case AppearanceConditionType.HasItem:
                    return GetItemCount(condition.requiredItemId) > 0;
                case AppearanceConditionType.FoundCreaturesOfElement:
                    return CountFoundCreaturesOfElement(condition.requiredElement) >= condition.requiredCount;
                case AppearanceConditionType.ExplorationCount:
                    return progress.explorationCount >= condition.requiredCount;
                default:
                    return true;
            }
        }

        private void Save()
        {
            progress.creatures = WriteEntries(creatureCounts);
            progress.lifetimeCreatures =
                WriteEntries(lifetimeCreatureCounts);
            progress.items = WriteEntries(itemCounts);
            progress.tracks = WriteEntries(trackCounts);
            SaveSystem.Save(progress);
            Debug.Log("Monstrology progress saved.");
            NotifyStateChanged();

            if (YandexGamesBridge.Instance != null)
            {
                YandexGamesBridge.Instance.SaveProgress();
            }

            CloudSaveCoordinator cloud =
                FindObjectOfType<CloudSaveCoordinator>();
            if (cloud != null)
            {
                cloud.NotifyLocalSave();
            }
        }

        private void NotifyStateChanged()
        {
            if (StateChanged != null)
            {
                StateChanged();
            }
        }

        private void EnsureProgressDefaults()
        {
            int loadedVersion = progress.version;
            bool hasExistingProgress = loadedVersion > 0 ||
                                       progress.explorationCount > 0 ||
                                       progress.totalCreaturesFound > 0 ||
                                       (progress.pets != null && progress.pets.Count > 0);
            if (loadedVersion < 7 && hasExistingProgress)
            {
                progress.introCompleted = true;
            }

            if (progress.unlockedBiomes == null)
            {
                progress.unlockedBiomes = new List<string>();
            }

            if (!progress.unlockedBiomes.Contains(BiomeType.Forest.ToString()))
            {
                progress.unlockedBiomes.Add(BiomeType.Forest.ToString());
            }

            progress.creatures = progress.creatures ?? new List<StringIntEntry>();
            progress.lifetimeCreatures =
                progress.lifetimeCreatures ?? new List<StringIntEntry>();
            progress.discoveredSpecies = progress.discoveredSpecies ?? new List<string>();
            progress.items = progress.items ?? new List<StringIntEntry>();
            progress.tracks = progress.tracks ?? new List<StringIntEntry>();
            progress.claimedQuests = progress.claimedQuests ?? new List<string>();
            progress.purchasedHints = progress.purchasedHints ?? new List<string>();
            progress.hintLevels = progress.hintLevels ?? new List<StringIntEntry>();
            foreach (string creatureId in progress.purchasedHints)
            {
                if (!string.IsNullOrEmpty(creatureId) &&
                    !progress.hintLevels.Exists(entry =>
                        entry != null && entry.id == creatureId))
                {
                    progress.hintLevels.Add(new StringIntEntry(creatureId, 1));
                }
            }

            progress.hintLevels.RemoveAll(entry =>
                entry == null || string.IsNullOrEmpty(entry.id));
            foreach (StringIntEntry hint in progress.hintLevels)
            {
                hint.value = Mathf.Clamp(hint.value, 0, 4);
                if (hint.value > 0 && !progress.purchasedHints.Contains(hint.id))
                {
                    progress.purchasedHints.Add(hint.id);
                }
            }
            progress.pets = progress.pets ?? new List<CreatureInstance>();
            progress.accessories = progress.accessories ?? new List<string>();
            progress.unlockedAchievements = progress.unlockedAchievements ?? new List<string>();
            progress.creatureNests = progress.creatureNests ?? new List<CreatureNestProgress>();
            progress.completedSignatureSets =
                progress.completedSignatureSets ?? new List<string>();
            progress.activeSignatureBonuses =
                progress.activeSignatureBonuses ?? new List<string>();
            progress.foundBiomeEvents = progress.foundBiomeEvents ?? new List<string>();
            progress.lastDailyRewardUtcDate = progress.lastDailyRewardUtcDate ?? string.Empty;
            progress.dailyRewardStreak = Mathf.Clamp(progress.dailyRewardStreak, 0, 7);
            progress.dailyRewardFirstLaunchUtcDate =
                progress.dailyRewardFirstLaunchUtcDate ?? string.Empty;
            if (loadedVersion < 10 &&
                !progress.dailyRewardFirstLaunchRegistered)
            {
                progress.dailyRewardFirstLaunchRegistered = true;
                DateTime accountCreated;
                progress.dailyRewardFirstLaunchUtcDate =
                    DateTime.TryParse(
                        progress.accountCreatedUtc,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out accountCreated)
                        ? accountCreated.ToUniversalTime().ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture)
                        : DateTime.UtcNow.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture);
            }
            progress.petMigrationVersion =
                Mathf.Max(0, progress.petMigrationVersion);
            progress.saveRevision = Math.Max(0L, progress.saveRevision);
            progress.updatedAtUtc = progress.updatedAtUtc ?? string.Empty;
            string now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            progress.lastEnergyUtc = string.IsNullOrEmpty(progress.lastEnergyUtc)
                ? now
                : progress.lastEnergyUtc;
            progress.accountCreatedUtc = string.IsNullOrEmpty(progress.accountCreatedUtc)
                ? now
                : progress.accountCreatedUtc;
            progress.starterBoostPlaySeconds =
                Mathf.Clamp(progress.starterBoostPlaySeconds, 0f, 15f * 60f);
            progress.starterBoostRewardIndex = Mathf.Max(0, progress.starterBoostRewardIndex);
            progress.energy = Mathf.Clamp(
                progress.energy,
                0,
                EnergyRegenerationSystem.MaxEnergy);
            progress.timeOfDay = string.IsNullOrEmpty(progress.timeOfDay)
                ? TimeOfDay.Day.ToString()
                : progress.timeOfDay;
            progress.weather = string.IsNullOrEmpty(progress.weather)
                ? WeatherType.Sunny.ToString()
                : progress.weather;
            progress.worldTime01 = Mathf.Repeat(progress.worldTime01, 1f);
            progress.weatherTimer = Mathf.Max(0f, progress.weatherTimer);
            progress.version = Mathf.Max(progress.version, 10);
        }

        private void MigrateLifetimeCreatureCounts()
        {
            foreach (KeyValuePair<string, int> entry in creatureCounts)
            {
                int currentLifetime = GetLifetimeCreatureCount(entry.Key);
                lifetimeCreatureCounts[entry.Key] =
                    Mathf.Max(currentLifetime, entry.Value);
            }

            if (progress.pets == null)
            {
                return;
            }

            foreach (CreatureInstance pet in progress.pets)
            {
                if (pet == null || string.IsNullOrEmpty(pet.speciesId))
                {
                    continue;
                }

                string root = EvolutionProgressUtility.GetRootSpeciesId(
                    content,
                    pet.speciesId);
                int stage = Mathf.Max(
                    pet.evolutionStage,
                    EvolutionProgressUtility.GetSpeciesStage(
                        content,
                        pet.speciesId));
                pet.evolutionRootSpeciesId = string.IsNullOrEmpty(root)
                    ? pet.speciesId
                    : root;
                pet.evolutionStage = stage;
                if (stage > 0)
                {
                    int floor = EvolutionProgressUtility
                        .GetThresholdForTransition(stage - 1);
                    lifetimeCreatureCounts[pet.evolutionRootSpeciesId] =
                        Mathf.Max(
                            GetLifetimeCreatureCount(
                                pet.evolutionRootSpeciesId),
                            floor);
                }
            }
        }

        private void EnsureContentDefaults()
        {
            content.creatures = content.creatures ?? new List<CreatureData>();
            content.biomes = content.biomes ?? new List<BiomeData>();
            content.items = content.items ?? new List<ItemData>();
            content.accessories = content.accessories ?? new List<AccessoryData>();
            content.quests = content.quests ?? new List<QuestData>();
            content.mutationRecipes = content.mutationRecipes ?? new List<MutationRecipe>();
            content.breedingRecipes = content.breedingRecipes ?? new List<BreedingRecipeData>();
            content.speciesEvolutions = content.speciesEvolutions ?? new List<SpeciesEvolutionData>();
            content.signatureSets = content.signatureSets ?? new List<SignatureSetData>();
            content.biomeEvents = content.biomeEvents ?? new List<BiomeEventData>();
            content.creatureNests = content.creatureNests ?? new List<CreatureNestData>();
        }

        private static void ReadEntries(IEnumerable<StringIntEntry> entries, IDictionary<string, int> target)
        {
            target.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (StringIntEntry entry in entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id))
                {
                    target[entry.id] = entry.value;
                }
            }
        }

        private static List<StringIntEntry> WriteEntries(Dictionary<string, int> source)
        {
            return source.Select(pair => new StringIntEntry(pair.Key, pair.Value)).ToList();
        }

        private static BiomeType ParseBiome(string value)
        {
            BiomeType result;
            return Enum.TryParse(value, out result) ? result : BiomeType.Forest;
        }

        private static TimeOfDay ParseTimeOfDay(string value)
        {
            TimeOfDay result;
            return Enum.TryParse(value, out result) ? result : TimeOfDay.Day;
        }

        private static WeatherType ParseWeather(string value)
        {
            WeatherType result;
            return Enum.TryParse(value, out result) ? result : WeatherType.Sunny;
        }

        private static int GetDiscoveryReward(CreatureRarity rarity, bool firstDiscovery)
        {
            int baseReward;
            switch (rarity)
            {
                case CreatureRarity.Rare:
                    baseReward = 9;
                    break;
                case CreatureRarity.Epic:
                    baseReward = 16;
                    break;
                case CreatureRarity.Legendary:
                    baseReward = 28;
                    break;
                case CreatureRarity.Secret:
                    baseReward = 40;
                    break;
                default:
                    baseReward = 5;
                    break;
            }

            return firstDiscovery ? baseReward * 2 : baseReward;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }

                return hash;
            }
        }
    }
}
