using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public static class ContentValidator
    {
        public static int Validate(GameContent content)
        {
            int warnings = 0;
            if (content == null)
            {
                Debug.LogWarning("ContentValidator: GameContent is null.");
                return 1;
            }

            HashSet<string> creatureIds = new HashSet<string>();
            foreach (CreatureData creature in content.creatures)
            {
                if (creature == null)
                {
                    Warn("The encyclopedia contains a missing creature reference.", ref warnings);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(creature.id))
                {
                    Warn("A creature has no id: " + creature.name, ref warnings);
                    continue;
                }

                if (!creatureIds.Add(creature.id))
                {
                    Warn("Duplicate creature id: " + creature.id, ref warnings);
                }

                if (string.IsNullOrWhiteSpace(creature.creatureName))
                {
                    Warn("Creature has no display name: " + creature.id, ref warnings);
                }

                if (!Enum.IsDefined(typeof(BiomeType), creature.biome))
                {
                    Warn("Creature has an invalid biome: " + creature.id, ref warnings);
                }

                if (!Enum.IsDefined(typeof(CreatureRarity), creature.rarity))
                {
                    Warn("Creature has an invalid rarity: " + creature.id, ref warnings);
                }
            }

            foreach (CreatureData creature in content.creatures)
            {
                if (creature == null || string.IsNullOrEmpty(creature.id))
                {
                    continue;
                }

                bool availableInBiome = content.biomes.Exists(biome =>
                    biome != null && biome.availableCreatures != null &&
                    biome.availableCreatures.Contains(creature));
                bool obtainableByEvolution = content.speciesEvolutions.Exists(evolution =>
                    evolution != null && evolution.resultSpeciesId == creature.id);
                if (!availableInBiome && !obtainableByEvolution)
                {
                    Warn("Creature cannot be obtained from a biome or evolution: " +
                         creature.id, ref warnings);
                }
            }

            foreach (BiomeData biome in content.biomes)
            {
                if (biome == null || biome.availableCreatures == null)
                {
                    continue;
                }

                foreach (CreatureData creature in biome.availableCreatures)
                {
                    if (creature == null || !content.creatures.Contains(creature))
                    {
                        Warn("Biome contains a creature missing from the encyclopedia: " +
                             biome.biomeName, ref warnings);
                    }
                }
            }

            foreach (SpeciesEvolutionData evolution in content.speciesEvolutions)
            {
                if (evolution == null)
                {
                    Warn("Missing SpeciesEvolutionData reference.", ref warnings);
                    continue;
                }

                if (!creatureIds.Contains(evolution.baseSpeciesId))
                {
                    Warn("Evolution base species does not exist: " +
                         evolution.baseSpeciesId, ref warnings);
                }

                if (!creatureIds.Contains(evolution.resultSpeciesId))
                {
                    Warn("Evolution result species does not exist: " +
                         evolution.resultSpeciesId, ref warnings);
                }
            }

            foreach (ItemData item in content.items)
            {
                if (item == null ||
                    string.IsNullOrEmpty(item.requiredSpeciesId))
                {
                    continue;
                }

                if (!creatureIds.Contains(item.requiredSpeciesId))
                {
                    Warn(
                        "Item links to a missing creature: " +
                        item.id +
                        " -> " +
                        item.requiredSpeciesId,
                        ref warnings);
                }
            }

            foreach (AccessoryData accessory in content.accessories)
            {
                if (accessory == null || !accessory.IsSignature)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(accessory.EffectiveSetId))
                {
                    Warn("Signature accessory has no setId: " + accessory.id, ref warnings);
                }

                bool hasBiome = content.biomes.Exists(biome =>
                    biome != null && accessory.IsSignatureForBiome(biome.type));
                if (!hasBiome)
                {
                    Warn("Signature accessory has an invalid biome: " +
                         accessory.id, ref warnings);
                }
            }

            HashSet<string> evolutionResultIds = new HashSet<string>(
                content.speciesEvolutions
                    .Where(evolution => evolution != null)
                    .Select(evolution => evolution.resultSpeciesId)
                    .Where(id => !string.IsNullOrEmpty(id)));
            foreach (CreatureData root in content.creatures.Where(creature =>
                         creature != null &&
                         !string.IsNullOrEmpty(creature.id) &&
                         !evolutionResultIds.Contains(creature.id)))
            {
                CreatureNestData nest = content.creatureNests.Find(entry =>
                    entry != null && entry.speciesId == root.id);
                if (nest == null)
                {
                    Warn("Base creature has no nest: " + root.id, ref warnings);
                }
                else if (string.IsNullOrEmpty(nest.id) ||
                         string.IsNullOrEmpty(nest.displayName) ||
                         string.IsNullOrEmpty(nest.description))
                {
                    Warn("Creature nest is incomplete: " + root.id, ref warnings);
                }
            }

            foreach (BiomeData biome in content.biomes)
            {
                SignatureSetData set = content.signatureSets.Find(entry =>
                    entry != null && biome != null && entry.biome == biome.type);
                if (biome == null || set == null || set.accessoryIds == null)
                {
                    Warn("Biome has no signature set: " +
                         (biome != null ? biome.biomeName : "<null>"), ref warnings);
                    continue;
                }

                HashSet<AccessorySlot> slots = new HashSet<AccessorySlot>();
                foreach (string accessoryId in set.accessoryIds)
                {
                    AccessoryData accessory = content.accessories.Find(entry =>
                        entry != null && entry.id == accessoryId);
                    if (accessory != null && accessory.IsSignatureForBiome(biome.type))
                    {
                        slots.Add(accessory.slot);
                    }
                }

                if (Enum.GetValues(typeof(AccessorySlot))
                    .Cast<AccessorySlot>()
                    .Any(slot => !slots.Contains(slot)))
                {
                    Warn("Signature set does not cover all slots: " +
                         set.id, ref warnings);
                }
            }

            warnings += ValidateEncyclopediaAvailability(content);
            Debug.Log("ContentValidator: completed with " + warnings + " warning(s).");
            return warnings;
        }

        public static int ValidateEncyclopediaAvailability(GameContent content)
        {
            int warnings = 0;
            if (content == null)
            {
                Debug.LogWarning("EncyclopediaValidator: GameContent is null.");
                return 1;
            }

            List<CreatureData> creatures = content.creatures ?? new List<CreatureData>();
            List<BiomeData> biomes = content.biomes ?? new List<BiomeData>();
            List<SpeciesEvolutionData> evolutions =
                content.speciesEvolutions ?? new List<SpeciesEvolutionData>();
            HashSet<string> reachable = new HashSet<string>();

            foreach (CreatureData creature in creatures)
            {
                if (creature == null || string.IsNullOrEmpty(creature.id))
                {
                    continue;
                }

                bool declaredBiomeExists = biomes.Exists(biome =>
                    biome != null && biome.type == creature.biome);
                if (!declaredBiomeExists)
                {
                    WarnAvailability(
                        "Creature " + DisplayName(creature) +
                        " ссылается на отсутствующий BiomeData.",
                        ref warnings);
                }

                List<BiomeData> directBiomes = biomes.FindAll(biome =>
                    biome != null && biome.availableCreatures != null &&
                    biome.availableCreatures.Exists(entry =>
                        entry != null && entry.id == creature.id));
                bool conditionsPossible = ConditionsCanBeMet(
                    creature,
                    directBiomes,
                    content);
                bool directDrop = directBiomes.Count > 0 &&
                                  creature.appearanceChance > 0f &&
                                  conditionsPossible;
                bool nestDrop = content.creatureNests != null &&
                                content.creatureNests.Exists(nest =>
                                    nest != null && nest.speciesId == creature.id);
                bool eventDrop = directDrop && content.biomeEvents != null &&
                                 content.biomeEvents.Exists(eventData =>
                                     eventData != null &&
                                     directBiomes.Exists(biome =>
                                         biome.type == eventData.biome));

                if (directBiomes.Count > 0 && creature.appearanceChance <= 0f &&
                    !nestDrop &&
                    !evolutions.Exists(evolution =>
                        evolution != null &&
                        evolution.resultSpeciesId == creature.id))
                {
                    WarnAvailability(
                        "Creature " + DisplayName(creature) +
                        " имеет шанс выпадения 0.",
                        ref warnings);
                }

                if (directDrop || nestDrop || eventDrop)
                {
                    reachable.Add(creature.id);
                }
            }

            bool changed;
            do
            {
                changed = false;
                foreach (SpeciesEvolutionData evolution in evolutions)
                {
                    if (evolution == null ||
                        string.IsNullOrEmpty(evolution.baseSpeciesId) ||
                        string.IsNullOrEmpty(evolution.resultSpeciesId) ||
                        !reachable.Contains(evolution.baseSpeciesId))
                    {
                        continue;
                    }

                    changed |= reachable.Add(evolution.resultSpeciesId);
                }
            }
            while (changed);

            foreach (CreatureData creature in creatures)
            {
                if (creature != null &&
                    !string.IsNullOrEmpty(creature.id) &&
                    !reachable.Contains(creature.id))
                {
                    WarnAvailability(
                        "Creature " + DisplayName(creature) +
                        " не может быть найден ни в одном биоме.",
                        ref warnings);
                }
            }

            HashSet<string> resultIds = new HashSet<string>(
                evolutions
                    .Where(evolution => evolution != null)
                    .Select(evolution => evolution.resultSpeciesId)
                    .Where(id => !string.IsNullOrEmpty(id)));
            foreach (CreatureData root in creatures.Where(creature =>
                         creature != null &&
                         !string.IsNullOrEmpty(creature.id) &&
                         !resultIds.Contains(creature.id)))
            {
                if (!evolutions.Exists(evolution =>
                        evolution != null &&
                        evolution.baseSpeciesId == root.id &&
                        !string.IsNullOrEmpty(evolution.resultSpeciesId)))
                {
                    WarnAvailability(
                        "Creature " + DisplayName(root) +
                        " не имеет следующей эволюционной формы.",
                        ref warnings);
                }
            }

            Debug.Log("EncyclopediaValidator: completed with " + warnings +
                      " warning(s). Reachable creatures: " + reachable.Count +
                      "/" + creatures.Count + ".");
            return warnings;
        }

        private static bool ConditionsCanBeMet(
            CreatureData creature,
            ICollection<BiomeData> directBiomes,
            GameContent content)
        {
            if (creature.allowedTimes != null &&
                creature.allowedTimes.Any(time =>
                    !Enum.IsDefined(typeof(TimeOfDay), time)))
            {
                return false;
            }

            if (creature.allowedWeather != null &&
                creature.allowedWeather.Any(weather =>
                    !Enum.IsDefined(typeof(WeatherType), weather)))
            {
                return false;
            }

            if (creature.appearanceConditions == null)
            {
                return true;
            }

            foreach (AppearanceCondition condition in creature.appearanceConditions)
            {
                if (condition == null)
                {
                    continue;
                }

                switch (condition.type)
                {
                    case AppearanceConditionType.CurrentBiome:
                        if (!directBiomes.Any(biome =>
                                biome.type == condition.requiredBiome))
                        {
                            return false;
                        }
                        break;
                    case AppearanceConditionType.HasItem:
                        if (string.IsNullOrEmpty(condition.requiredItemId) ||
                            content.items == null ||
                            !content.items.Exists(item =>
                                item != null &&
                                item.id == condition.requiredItemId))
                        {
                            return false;
                        }
                        break;
                    case AppearanceConditionType.FoundCreaturesOfElement:
                        int elementCount = content.creatures.Count(entry =>
                            entry != null &&
                            entry.element == condition.requiredElement);
                        if (condition.requiredCount <= 0 ||
                            elementCount < condition.requiredCount)
                        {
                            return false;
                        }
                        break;
                    case AppearanceConditionType.ExplorationCount:
                        if (condition.requiredCount < 0)
                        {
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        private static string DisplayName(CreatureData creature)
        {
            return creature != null && !string.IsNullOrEmpty(creature.creatureName)
                ? creature.creatureName
                : creature != null ? creature.id : "<null>";
        }

        private static void Warn(string message, ref int warnings)
        {
            warnings++;
            Debug.LogWarning("ContentValidator: " + message);
        }

        private static void WarnAvailability(string message, ref int warnings)
        {
            warnings++;
            Debug.LogWarning("EncyclopediaValidator: " + message);
        }
    }
}
