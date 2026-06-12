using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public static class EvolutionFallbackBuilder
    {
        private const int MinimumFormsPerRoot = 3;

        public static int EnsureFallbackChains(GameContent content)
        {
            if (content == null)
            {
                return 0;
            }

            content.creatures = content.creatures ?? new List<CreatureData>();
            content.speciesEvolutions =
                content.speciesEvolutions ?? new List<SpeciesEvolutionData>();

            HashSet<string> resultIds = new HashSet<string>(
                content.speciesEvolutions
                    .Where(evolution => evolution != null)
                    .Select(evolution => evolution.resultSpeciesId)
                    .Where(id => !string.IsNullOrEmpty(id)));
            List<CreatureData> roots = content.creatures
                .Where(creature => creature != null &&
                                   !string.IsNullOrEmpty(creature.id) &&
                                   !resultIds.Contains(creature.id))
                .ToList();

            int created = 0;
            foreach (CreatureData root in roots)
            {
                CreatureData current = root;
                int currentForm = 1;
                while (current != null && currentForm < MinimumFormsPerRoot)
                {
                    SpeciesEvolutionData existing = content.speciesEvolutions.Find(evolution =>
                        evolution != null && evolution.baseSpeciesId == current.id);
                    CreatureData next = existing != null
                        ? content.creatures.Find(creature =>
                            creature != null && creature.id == existing.resultSpeciesId)
                        : null;
                    if (next != null)
                    {
                        current = next;
                        currentForm++;
                        continue;
                    }

                    int nextForm = currentForm + 1;
                    string generatedId = BuildGeneratedId(content, root.id, nextForm);
                    next = CreateFallbackForm(root, generatedId, nextForm);
                    content.creatures.Add(next);

                    SpeciesEvolutionData evolution = existing;
                    if (evolution == null)
                    {
                        evolution = ScriptableObject.CreateInstance<SpeciesEvolutionData>();
                        evolution.hideFlags = HideFlags.DontSave;
                        evolution.name = root.id + "_fallback_evolution_" + currentForm;
                        content.speciesEvolutions.Add(evolution);
                    }

                    evolution.baseSpeciesId = current.id;
                    evolution.requiredCopies = CopiesForTransition(currentForm);
                    evolution.resultSpeciesId = next.id;

                    current = next;
                    currentForm = nextForm;
                    created++;
                }
            }

            AssociateCreaturesWithDeclaredBiomes(content);
            if (created > 0)
            {
                Debug.Log("EvolutionFallbackBuilder: created " + created +
                          " missing evolution form(s).");
            }

            return created;
        }

        private static void AssociateCreaturesWithDeclaredBiomes(GameContent content)
        {
            if (content.biomes == null)
            {
                return;
            }

            foreach (CreatureData creature in content.creatures)
            {
                if (creature == null)
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
                }
            }
        }

        private static CreatureData CreateFallbackForm(
            CreatureData root,
            string id,
            int form)
        {
            CreatureData creature = ScriptableObject.CreateInstance<CreatureData>();
            creature.hideFlags = HideFlags.DontSave;
            creature.name = id;
            creature.id = id;
            creature.creatureName = root.creatureName + " " + Roman(form);
            creature.description =
                "Эволюционная форма вида «" + root.creatureName +
                "», открываемая за собранные копии.";
            creature.rarity = UpgradeRarity(root.rarity, form - 1);
            creature.element = root.element;
            creature.biome = root.biome;
            creature.icon = root.icon;
            creature.portraitSprite = root.portraitSprite;
            creature.worldSprite = root.worldSprite;
            creature.evolutionSprite = root.evolutionSprite;
            creature.appearanceChance = 0f;
            return creature;
        }

        private static string BuildGeneratedId(
            GameContent content,
            string rootId,
            int form)
        {
            string suffix = form == 2 ? "_ii" : form == 3 ? "_iii" : "_iv";
            string candidate = rootId + suffix;
            if (!content.creatures.Exists(creature =>
                    creature != null && creature.id == candidate))
            {
                return candidate;
            }

            candidate = rootId + "_evolution" + suffix;
            int number = 2;
            while (content.creatures.Exists(creature =>
                       creature != null && creature.id == candidate))
            {
                candidate = rootId + "_evolution" + suffix + "_" + number;
                number++;
            }

            return candidate;
        }

        private static int CopiesForTransition(int currentForm)
        {
            if (currentForm <= 1)
            {
                return 100;
            }

            if (currentForm == 2)
            {
                return 250;
            }

            return 500;
        }

        private static CreatureRarity UpgradeRarity(
            CreatureRarity rarity,
            int steps)
        {
            return (CreatureRarity)Mathf.Clamp(
                (int)rarity + Mathf.Max(0, steps),
                (int)CreatureRarity.Common,
                (int)CreatureRarity.Legendary);
        }

        private static string Roman(int form)
        {
            switch (form)
            {
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                default: return form.ToString();
            }
        }
    }
}
