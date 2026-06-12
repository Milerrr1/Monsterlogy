using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public static class ContentAvailabilityRepair
    {
        public static int Repair(GameContent content)
        {
            if (content == null)
            {
                return 0;
            }

            content.creatures = content.creatures ?? new List<CreatureData>();
            content.biomes = content.biomes ?? new List<BiomeData>();
            content.items = content.items ?? new List<ItemData>();
            content.speciesEvolutions =
                content.speciesEvolutions ?? new List<SpeciesEvolutionData>();

            HashSet<string> evolutionResults = new HashSet<string>(
                content.speciesEvolutions
                    .Where(evolution => evolution != null)
                    .Select(evolution => evolution.resultSpeciesId)
                    .Where(id => !string.IsNullOrEmpty(id)));
            int repairs = 0;

            foreach (CreatureData creature in content.creatures)
            {
                if (creature == null || string.IsNullOrEmpty(creature.id))
                {
                    continue;
                }

                BiomeData biome = content.biomes.Find(entry =>
                    entry != null && entry.type == creature.biome);
                if (biome == null)
                {
                    continue;
                }

                biome.availableCreatures = biome.availableCreatures ??
                                           new List<CreatureData>();
                if (!biome.availableCreatures.Exists(entry =>
                        entry != null && entry.id == creature.id))
                {
                    biome.availableCreatures.Add(creature);
                    repairs++;
                }

                if (!evolutionResults.Contains(creature.id) &&
                    biome.mapData != null)
                {
                    biome.mapData.possibleCreatures =
                        biome.mapData.possibleCreatures ??
                        new List<CreatureData>();
                    if (!biome.mapData.possibleCreatures.Exists(entry =>
                            entry != null && entry.id == creature.id))
                    {
                        biome.mapData.possibleCreatures.Add(creature);
                        repairs++;
                    }
                }

                if (!evolutionResults.Contains(creature.id) &&
                    creature.appearanceChance <= 0f)
                {
                    creature.appearanceChance = 0.05f;
                    repairs++;
                }

                repairs += RepairConditions(creature, content);
            }

            if (repairs > 0)
            {
                Debug.Log("ContentAvailabilityRepair: applied " + repairs +
                          " availability repair(s).");
            }

            return repairs;
        }

        private static int RepairConditions(
            CreatureData creature,
            GameContent content)
        {
            if (creature.appearanceConditions == null)
            {
                creature.appearanceConditions = new List<AppearanceCondition>();
                return 1;
            }

            int repairs = 0;
            for (int index = creature.appearanceConditions.Count - 1;
                 index >= 0;
                 index--)
            {
                AppearanceCondition condition =
                    creature.appearanceConditions[index];
                if (condition == null)
                {
                    creature.appearanceConditions.RemoveAt(index);
                    repairs++;
                    continue;
                }

                switch (condition.type)
                {
                    case AppearanceConditionType.CurrentBiome:
                        if (condition.requiredBiome != creature.biome)
                        {
                            condition.requiredBiome = creature.biome;
                            repairs++;
                        }
                        break;
                    case AppearanceConditionType.HasItem:
                        bool itemExists = !string.IsNullOrEmpty(
                                              condition.requiredItemId) &&
                                          content.items.Exists(item =>
                                              item != null &&
                                              item.id ==
                                              condition.requiredItemId);
                        if (!itemExists)
                        {
                            creature.appearanceConditions.RemoveAt(index);
                            repairs++;
                        }
                        break;
                    case AppearanceConditionType.FoundCreaturesOfElement:
                        int available = content.creatures.Count(entry =>
                            entry != null &&
                            entry.element == condition.requiredElement);
                        int fixedCount = Mathf.Clamp(
                            condition.requiredCount,
                            1,
                            Mathf.Max(1, available));
                        if (fixedCount != condition.requiredCount)
                        {
                            condition.requiredCount = fixedCount;
                            repairs++;
                        }
                        break;
                    case AppearanceConditionType.ExplorationCount:
                        if (condition.requiredCount < 1)
                        {
                            condition.requiredCount = 1;
                            repairs++;
                        }
                        break;
                }
            }

            return repairs;
        }
    }
}
