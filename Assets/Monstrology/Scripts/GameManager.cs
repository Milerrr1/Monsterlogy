using System;
using System.Collections.Generic;
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
        ShinyStone
    }

    [Serializable]
    public class GameContent
    {
        public List<CreatureData> creatures = new List<CreatureData>();
        public List<BiomeData> biomes = new List<BiomeData>();
        public List<ItemData> items = new List<ItemData>();
        public List<QuestData> quests = new List<QuestData>();
        public List<MutationRecipe> mutationRecipes = new List<MutationRecipe>();
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
        public int ExplorationCount { get { return progress.explorationCount; } }
        public int MutationCount { get { return progress.mutationCount; } }
        public int TotalCreaturesFound { get { return progress.totalCreaturesFound; } }
        public BiomeData CurrentBiome { get; private set; }

        private GameProgress progress;
        private readonly Dictionary<string, int> creatureCounts = new Dictionary<string, int>();
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

        public void Initialize(GameContent gameContent)
        {
            content = gameContent ?? new GameContent();
            progress = SaveSystem.Load();
            EnsureProgressDefaults();
            ReadEntries(progress.creatures, creatureCounts);
            ReadEntries(progress.items, itemCounts);
            ReadEntries(progress.tracks, trackCounts);
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

            progress.energy -= amount;
            Save();
            return true;
        }

        public void AddEnergy(int amount)
        {
            progress.energy = Mathf.Max(0, progress.energy + amount);
            Save();
        }

        public void AddCoins(int amount)
        {
            progress.coins = Mathf.Max(0, progress.coins + amount);
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

            bool firstDiscovery = GetCreatureCount(creature.id) == 0;
            creatureCounts[creature.id] = GetCreatureCount(creature.id) + 1;
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
            return GetCreatureCount(creatureId) > 0;
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

        public CreatureData GetCreature(string id)
        {
            return content.creatures.Find(creature => creature != null && creature.id == id);
        }

        public ItemData GetItem(string id)
        {
            return content.items.Find(item => item != null && item.id == id);
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

        public bool IsQuestClaimed(string questId)
        {
            return progress.claimedQuests.Contains(questId);
        }

        public bool IsHintPurchased(string creatureId)
        {
            return progress.purchasedHints.Contains(creatureId);
        }

        public bool TryBuyHint(string creatureId, int price)
        {
            if (IsHintPurchased(creatureId))
            {
                return true;
            }

            if (progress.coins < price)
            {
                RaiseNotification("Недостаточно монет для подсказки.");
                return false;
            }

            progress.coins -= price;
            progress.purchasedHints.Add(creatureId);
            Save();
            return true;
        }

        public void ClaimQuest(QuestData quest)
        {
            if (quest == null || IsQuestClaimed(quest.id))
            {
                return;
            }

            progress.claimedQuests.Add(quest.id);
            progress.coins += quest.rewardCoins;
            progress.energy += quest.rewardEnergy;
            RaiseNotification("Награда получена: +" + quest.rewardCoins + " монет, +" +
                              quest.rewardEnergy + " энергии");
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
                   "  |  Камни " + GetTrackCount(TrackType.ShinyStone);
        }

        public void ResetProgress()
        {
            SaveSystem.Delete();
            progress = SaveSystem.Load();
            EnsureProgressDefaults();
            creatureCounts.Clear();
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
            progress.items = WriteEntries(itemCounts);
            progress.tracks = WriteEntries(trackCounts);
            SaveSystem.Save(progress);
            NotifyStateChanged();

            if (YandexGamesBridge.Instance != null)
            {
                YandexGamesBridge.Instance.SaveProgress();
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
            if (progress.unlockedBiomes == null)
            {
                progress.unlockedBiomes = new List<string>();
            }

            if (!progress.unlockedBiomes.Contains(BiomeType.Forest.ToString()))
            {
                progress.unlockedBiomes.Add(BiomeType.Forest.ToString());
            }

            progress.creatures = progress.creatures ?? new List<StringIntEntry>();
            progress.items = progress.items ?? new List<StringIntEntry>();
            progress.tracks = progress.tracks ?? new List<StringIntEntry>();
            progress.claimedQuests = progress.claimedQuests ?? new List<string>();
            progress.purchasedHints = progress.purchasedHints ?? new List<string>();
            progress.pets = progress.pets ?? new List<CreatureInstance>();
            progress.version = Mathf.Max(progress.version, 2);
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
