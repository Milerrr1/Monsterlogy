using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public static class NestFallbackBuilder
    {
        public static int EnsureBaseSpeciesNests(GameContent content)
        {
            if (content == null)
            {
                return 0;
            }

            content.creatures = content.creatures ?? new List<CreatureData>();
            content.creatureNests =
                content.creatureNests ?? new List<CreatureNestData>();
            content.speciesEvolutions =
                content.speciesEvolutions ?? new List<SpeciesEvolutionData>();

            HashSet<string> evolutionResults = new HashSet<string>(
                content.speciesEvolutions
                    .Where(evolution => evolution != null)
                    .Select(evolution => evolution.resultSpeciesId)
                    .Where(id => !string.IsNullOrEmpty(id)));
            List<CreatureData> roots = content.creatures.Where(creature =>
                creature != null &&
                !string.IsNullOrEmpty(creature.id) &&
                !evolutionResults.Contains(creature.id)).ToList();

            int created = 0;
            foreach (CreatureData root in roots)
            {
                CreatureNestData existing = content.creatureNests.Find(nest =>
                    nest != null && nest.speciesId == root.id);
                if (existing != null)
                {
                    Normalize(existing, root);
                    continue;
                }

                CreatureNestData nest =
                    ScriptableObject.CreateInstance<CreatureNestData>();
                nest.hideFlags = HideFlags.DontSave;
                nest.name = root.id + "_fallback_nest";
                nest.id = UniqueNestId(content, root.id + "_nest");
                nest.displayName = "Логово " + root.creatureName;
                nest.description =
                    "Здесь часто встречаются " + root.creatureName + ".";
                nest.speciesId = root.id;
                nest.biome = root.biome;
                nest.rewardCooldownHours = CooldownForRarity(root.rarity);
                nest.baseCopyReward = 1;
                nest.baseResourceReward = 1;
                nest.rareAccessoryChance = 0.08f;
                nest.specialCreatureChance = SpecialChance(root.rarity);
                nest.specialCreatureId = root.id;
                content.creatureNests.Add(nest);
                created++;
            }

            if (created > 0)
            {
                Debug.Log("NestFallbackBuilder: created " + created +
                          " missing base-species nest(s).");
            }

            return created;
        }

        private static void Normalize(
            CreatureNestData nest,
            CreatureData creature)
        {
            if (string.IsNullOrEmpty(nest.displayName))
            {
                nest.displayName = "Логово " + creature.creatureName;
            }

            if (string.IsNullOrEmpty(nest.description))
            {
                nest.description =
                    "Здесь часто встречаются " + creature.creatureName + ".";
            }

            if (string.IsNullOrEmpty(nest.specialCreatureId))
            {
                nest.specialCreatureId = creature.id;
            }
        }

        private static string UniqueNestId(
            GameContent content,
            string preferred)
        {
            string candidate = preferred;
            int suffix = 2;
            while (content.creatureNests.Exists(nest =>
                       nest != null && nest.id == candidate))
            {
                candidate = preferred + "_" + suffix;
                suffix++;
            }

            return candidate;
        }

        private static float CooldownForRarity(CreatureRarity rarity)
        {
            switch (rarity)
            {
                case CreatureRarity.Legendary:
                case CreatureRarity.Secret:
                    return 4f;
                case CreatureRarity.Epic:
                    return 3.5f;
                case CreatureRarity.Rare:
                    return 3f;
                default:
                    return 2.5f;
            }
        }

        private static float SpecialChance(CreatureRarity rarity)
        {
            return rarity == CreatureRarity.Legendary ||
                   rarity == CreatureRarity.Secret
                ? 0.06f
                : 0.12f;
        }
    }
}
