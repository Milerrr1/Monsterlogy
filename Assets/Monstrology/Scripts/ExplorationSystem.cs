using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public enum ExplorationResultType
    {
        Creature,
        Track,
        Item,
        Egg,
        Nothing,
        Accessory
    }

    public class ExplorationResult
    {
        public ExplorationResultType type;
        public string title;
        public string description;
        public Sprite icon;
        public Color accentColor;
        public CreatureData creature;
        public AccessoryData accessory;
        public bool firstSpeciesDiscovery;
    }

    public class ExplorationSystem : MonoBehaviour
    {
        public event Action<ExplorationResult> ExplorationCompleted;

        private GameManager game;
        private AccessoryInventoryManager accessoryInventory;

        public void Initialize(
            GameManager gameManager,
            AccessoryInventoryManager accessoryInventoryManager = null)
        {
            game = gameManager;
            accessoryInventory = accessoryInventoryManager;
        }

        public void Explore()
        {
            WorldExplorationManager world = FindObjectOfType<WorldExplorationManager>();
            if (world != null)
            {
                world.StartQuickSearch();
            }
        }

        public ExplorationResult ResolveWorldDiscovery(
            ExplorationResultType type,
            CreatureData creature = null,
            ItemData item = null,
            TrackType? track = null,
            AccessoryData accessory = null)
        {
            if (game == null || game.CurrentBiome == null)
            {
                return null;
            }

            game.RegisterExploration();
            ExplorationResult result;
            switch (type)
            {
                case ExplorationResultType.Creature:
                    result = creature != null ? ResolveCreature(creature) : ResolveTrack(SelectTrack());
                    break;
                case ExplorationResultType.Track:
                    result = ResolveTrack(track ?? SelectTrack());
                    break;
                case ExplorationResultType.Item:
                    result = item != null ? ResolveItem(item, false) : ResolveTrack(SelectTrack());
                    break;
                case ExplorationResultType.Egg:
                    result = item != null ? ResolveItem(item, true) : ResolveTrack(SelectTrack());
                    break;
                case ExplorationResultType.Accessory:
                    result = accessory != null
                        ? ResolveAccessory(accessory)
                        : ResolveTrack(SelectTrack());
                    break;
                default:
                    result = CreateNothingResult();
                    break;
            }

            RaiseCompleted(result);
            return result;
        }

        public CreatureData SelectCreature(IList<CreatureData> source)
        {
            if (game == null || source == null)
            {
                return null;
            }

            List<CreatureData> candidates = new List<CreatureData>();
            for (int index = 0; index < source.Count; index++)
            {
                CreatureData creature = source[index];
                if (creature != null && game.AreAppearanceConditionsMet(creature))
                {
                    candidates.Add(creature);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            bool trackBonus = game.GetLargestTrackCount() >= 3;
            List<float> weights = new List<float>();
            foreach (CreatureData creature in candidates)
            {
                float weight = Mathf.Max(0.01f, creature.appearanceChance) * GetRarityWeight(creature.rarity);
                if (trackBonus && creature.rarity != CreatureRarity.Common)
                {
                    weight *= 1.75f;
                }

                weights.Add(weight);
                totalWeight += weight;
            }

            float selection = UnityEngine.Random.value * totalWeight;
            for (int index = 0; index < candidates.Count; index++)
            {
                selection -= weights[index];
                if (selection <= 0f)
                {
                    return candidates[index];
                }
            }

            return candidates[candidates.Count - 1];
        }

        public ItemData SelectItem(bool egg)
        {
            if (game == null || game.CurrentBiome == null)
            {
                return null;
            }

            List<ItemData> candidates = game.Content.items.FindAll(item =>
                item != null && (egg ? item.kind == ItemKind.Egg : item.kind != ItemKind.Egg));

            if (candidates.Count == 0)
            {
                return null;
            }

            List<ItemData> preferred = candidates.FindAll(item => item.preferredBiome == game.CurrentBiome.type);
            List<ItemData> pool = preferred.Count > 0 && UnityEngine.Random.value < 0.7f ? preferred : candidates;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public TrackType SelectTrack()
        {
            return (TrackType)UnityEngine.Random.Range(0, (int)TrackType.Lair);
        }

        public AccessoryData SelectAccessory()
        {
            if (game == null || game.Content.accessories == null || game.Content.accessories.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            List<AccessoryData> candidates = new List<AccessoryData>();
            List<float> weights = new List<float>();
            foreach (AccessoryData accessory in game.Content.accessories)
            {
                if (accessory == null || string.IsNullOrEmpty(accessory.id))
                {
                    continue;
                }

                float rarityFactor = 1f / (1f + (int)accessory.rarity * 0.65f);
                float weight = Mathf.Max(0.001f, accessory.dropChance) * rarityFactor;
                candidates.Add(accessory);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            for (int index = 0; index < candidates.Count; index++)
            {
                roll -= weights[index];
                if (roll <= 0f)
                {
                    return candidates[index];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private ExplorationResult RollResult()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.46f)
            {
                return FindCreature();
            }

            if (roll < 0.70f)
            {
                return FindTrack();
            }

            if (roll < 0.84f)
            {
                return FindItem(false);
            }

            if (roll < 0.90f)
            {
                return FindItem(true);
            }

            if (roll < 0.97f)
            {
                AccessoryData accessory = SelectAccessory();
                return accessory != null ? ResolveAccessory(accessory) : CreateNothingResult();
            }

            return CreateNothingResult();
        }

        private ExplorationResult FindCreature()
        {
            CreatureData selected = SelectCreature(game.CurrentBiome.availableCreatures);
            if (selected == null)
            {
                return FindTrack();
            }

            return ResolveCreature(selected);
        }

        private ExplorationResult ResolveCreature(CreatureData selected)
        {
            bool firstDiscovery = game.AddCreature(selected);
            string discoveryText = firstDiscovery
                ? "Новая запись в энциклопедии!"
                : "Этот вид уже знаком. Новая встреча отмечена в журнале.";

            return new ExplorationResult
            {
                type = ExplorationResultType.Creature,
                title = firstDiscovery ? "Открытие: " + selected.creatureName : "Снова " + selected.creatureName,
                description = discoveryText + "\nРедкость: " + Localization.Rarity(selected.rarity) +
                              "\n" + game.GetPseudoOnlineText(selected),
                icon = selected.icon,
                accentColor = Localization.RarityColor(selected.rarity),
                creature = selected,
                firstSpeciesDiscovery = firstDiscovery
            };
        }

        private ExplorationResult FindTrack()
        {
            return ResolveTrack(SelectTrack());
        }

        private ExplorationResult ResolveTrack(TrackType track)
        {
            game.AddTrack(track);
            int count = game.GetTrackCount(track);
            string bonus = count >= 3
                ? "\nСледов этого типа уже " + count + ": шанс редких существ повышен!"
                : "\nСоберите 3 одинаковых следа, чтобы повысить шанс редкой встречи.";

            return new ExplorationResult
            {
                type = ExplorationResultType.Track,
                title = "Найден след",
                description = Localization.Track(track) + bonus,
                accentColor = new Color(0.91f, 0.72f, 0.29f)
            };
        }

        private ExplorationResult FindItem(bool egg)
        {
            ItemData selected = SelectItem(egg);
            if (selected == null)
            {
                return FindTrack();
            }

            return ResolveItem(selected, egg);
        }

        private ExplorationResult ResolveItem(ItemData selected, bool egg)
        {
            game.AddItem(selected.id);

            if (egg)
            {
                game.AddTrack(TrackType.EggShell);
            }

            return new ExplorationResult
            {
                type = egg ? ExplorationResultType.Egg : ExplorationResultType.Item,
                title = egg ? "Загадочное яйцо" : "Полезная находка",
                description = selected.itemName + "\n" + selected.description +
                              "\nВ инвентаре: " + game.GetItemCount(selected.id),
                icon = selected.icon,
                accentColor = egg
                    ? new Color(0.78f, 0.59f, 0.96f)
                    : new Color(0.35f, 0.78f, 0.62f)
            };
        }

        private ExplorationResult ResolveAccessory(AccessoryData selected)
        {
            if (accessoryInventory == null || !accessoryInventory.AddAccessory(selected))
            {
                return CreateNothingResult();
            }

            if (game != null)
            {
                game.RaiseNotification("Найден предмет гардероба: " + selected.displayName);
            }

            return new ExplorationResult
            {
                type = ExplorationResultType.Accessory,
                title = "Новая одежда",
                description = selected.displayName + "\n" + selected.description +
                              "\nГардероб  |  Редкость: " + PetLocalization.Rarity(selected.rarity),
                icon = selected.icon,
                accentColor = PetLocalization.RarityColor(selected.rarity),
                accessory = selected
            };
        }

        private static ExplorationResult CreateNothingResult()
        {
            return new ExplorationResult
            {
                type = ExplorationResultType.Nothing,
                title = "Тихо...",
                description = "Здесь ничего не нашлось, но исследование всё равно приблизило вас к секретам.",
                accentColor = new Color(0.55f, 0.58f, 0.64f)
            };
        }

        private void RaiseCompleted(ExplorationResult result)
        {
            if (ExplorationCompleted != null)
            {
                ExplorationCompleted(result);
            }
        }

        private static float GetRarityWeight(CreatureRarity rarity)
        {
            switch (rarity)
            {
                case CreatureRarity.Rare:
                    return 0.48f;
                case CreatureRarity.Epic:
                    return 0.2f;
                case CreatureRarity.Legendary:
                    return 0.075f;
                case CreatureRarity.Secret:
                    return 0.035f;
                default:
                    return 1f;
            }
        }
    }

    public static class Localization
    {
        public static string Rarity(CreatureRarity rarity)
        {
            switch (rarity)
            {
                case CreatureRarity.Rare: return "Редкое";
                case CreatureRarity.Epic: return "Эпическое";
                case CreatureRarity.Legendary: return "Легендарное";
                case CreatureRarity.Secret: return "Секретное";
                default: return "Обычное";
            }
        }

        public static string Biome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return "Пустыня";
                case BiomeType.Tundra: return "Тундра";
                case BiomeType.Volcano: return "Вулкан";
                case BiomeType.Ocean: return "Океан";
                case BiomeType.Space: return "Космос";
                default: return "Лес";
            }
        }

        public static string Track(TrackType track)
        {
            switch (track)
            {
                case TrackType.Fur: return "Клочок необычной шерсти";
                case TrackType.EggShell: return "Осколок яичной скорлупы";
                case TrackType.StrangeSound: return "Запись странного звука";
                case TrackType.ShinyStone: return "Блестящий камень";
                case TrackType.Lair: return "Обнаруженное логово";
                default: return "Цепочка маленьких лап";
            }
        }

        public static Color RarityColor(CreatureRarity rarity)
        {
            switch (rarity)
            {
                case CreatureRarity.Rare: return new Color(0.25f, 0.62f, 0.96f);
                case CreatureRarity.Epic: return new Color(0.68f, 0.38f, 0.94f);
                case CreatureRarity.Legendary: return new Color(1f, 0.66f, 0.16f);
                case CreatureRarity.Secret: return new Color(0.96f, 0.32f, 0.58f);
                default: return new Color(0.35f, 0.78f, 0.48f);
            }
        }
    }
}
