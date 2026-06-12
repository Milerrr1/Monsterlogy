using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Monstrology
{
    public class BetaReadinessCheck : MonoBehaviour
    {
        public string LastReport { get; private set; }

        [ContextMenu("Run Beta Readiness Check")]
        public bool RunBetaReadinessCheck()
        {
            GameManager game = FindObjectOfType<GameManager>();
            List<string> lines = new List<string>();
            bool ready = game != null;
            if (game == null)
            {
                lines.Add("✗ GameManager не инициализирован.");
                return Finish(false, lines);
            }

            ready &= CheckCreatures(game, lines);
            ready &= CheckBiomes(game, lines);
            ready &= CheckNests(game, lines);
            ready &= CheckAchievements(game, lines);
            ready &= CheckEvolutions(game, lines);
            ready &= CheckWardrobe(game, lines);
            ready &= CheckSprites(game, lines);
            ready &= CheckCreatureTemplate(lines);
            ready &= CheckEnergy(lines);
            ready &= CheckMoney(game, lines);
            ready &= CheckEncyclopedia(game, lines);
            ready &= CheckFavorite(lines);
            return Finish(ready, lines);
        }

        private static bool CheckCreatures(GameManager game, ICollection<string> lines)
        {
            int warnings = ContentValidator.Validate(game.Content);
            bool valid = game.Content.creatures.Count >= 5 &&
                         game.Content.creatures.All(creature =>
                             creature != null &&
                             !string.IsNullOrEmpty(creature.id) &&
                             !string.IsNullOrEmpty(creature.creatureName) &&
                             Enum.IsDefined(typeof(CreatureRarity), creature.rarity)) &&
                         warnings == 0;
            Add(lines, valid, "Монстры",
                game.Content.creatures.Count + " записей, недоступных: " + warnings);
            return valid;
        }

        private static bool CheckBiomes(GameManager game, ICollection<string> lines)
        {
            BiomeType[] expected = (BiomeType[])Enum.GetValues(typeof(BiomeType));
            bool valid = expected.All(type =>
            {
                BiomeData biome = game.GetBiome(type);
                return biome != null &&
                       biome.availableCreatures != null &&
                       biome.availableCreatures.Any(creature =>
                           creature != null &&
                           creature.appearanceChance > 0.001f);
            });
            Add(lines, valid, "Биомы",
                game.Content.biomes.Count + "/" + expected.Length +
                " настроено, каждый имеет доступные виды");
            return valid;
        }

        private static bool CheckNests(GameManager game, ICollection<string> lines)
        {
            List<CreatureData> roots = GetEvolutionRoots(game.Content);
            List<CreatureData> missing = roots.Where(root =>
                !game.Content.creatureNests.Exists(nest =>
                    nest != null &&
                    nest.speciesId == root.id &&
                    !string.IsNullOrEmpty(nest.id) &&
                    !string.IsNullOrEmpty(nest.displayName) &&
                    !string.IsNullOrEmpty(nest.description) &&
                    nest.rewardCooldownHours > 0f)).ToList();
            List<BiomeType> biomesWithoutNests = Enum.GetValues(typeof(BiomeType))
                .Cast<BiomeType>()
                .Where(biome => !roots.Any(root =>
                    root.biome == biome &&
                    game.Content.creatureNests.Exists(nest =>
                        nest != null && nest.speciesId == root.id)))
                .ToList();
            bool valid = missing.Count == 0 &&
                         biomesWithoutNests.Count == 0 &&
                         FindObjectOfType<NestPanel>(true) != null &&
                         FindObjectOfType<CreatureNestSystem>() != null;
            Add(lines, valid, "Логовища",
                game.Content.creatureNests.Count + " настроено для " +
                roots.Count + " базовых видов" +
                (missing.Count > 0
                    ? "; отсутствуют: " +
                      string.Join(", ", missing.Select(root => root.creatureName))
                    : "") +
                (biomesWithoutNests.Count > 0
                    ? "; без логовищ: " +
                      string.Join(", ", biomesWithoutNests.Select(Localization.Biome))
                    : ""));
            return valid;
        }

        private static bool CheckAchievements(GameManager game, ICollection<string> lines)
        {
            AchievementSystem system = FindObjectOfType<AchievementSystem>();
            IReadOnlyList<AchievementDefinition> definitions =
                system != null ? system.GetDefinitions() : null;
            string[] requiredIds =
            {
                "first_creature",
                "hundred_explorations",
                "forest_collection",
                "first_legendary",
                "first_pet",
                "first_favorite",
                "first_evolution",
                "first_nest",
                "first_full_set",
                "first_pet_level_10",
                "complete_tundra",
                "complete_volcano"
            };
            bool supportingContent =
                game.Content.creatures.Any(creature =>
                    creature != null &&
                    (creature.rarity == CreatureRarity.Legendary ||
                     creature.rarity == CreatureRarity.Secret)) &&
                game.GetBiomeCreatureTotal(BiomeType.Forest) > 0 &&
                game.GetBiomeCreatureTotal(BiomeType.Tundra) > 0 &&
                game.GetBiomeCreatureTotal(BiomeType.Volcano) > 0 &&
                game.Content.creatureNests.Count > 0 &&
                game.Content.signatureSets.Count > 0 &&
                game.Content.items.Any(item =>
                    item != null && item.kind == ItemKind.UpgradeResource) &&
                FindObjectOfType<BreedingSystem>() != null &&
                FindObjectOfType<PetUpgradeSystem>() != null;
            bool valid = definitions != null &&
                         definitions.Count >= 10 &&
                         definitions.Select(definition => definition.id)
                             .Distinct().Count() == definitions.Count &&
                         requiredIds.All(id =>
                             definitions.Any(definition => definition.id == id)) &&
                         definitions.All(definition =>
                             definition != null &&
                             !string.IsNullOrEmpty(definition.id) &&
                             !string.IsNullOrEmpty(definition.title) &&
                             !string.IsNullOrEmpty(definition.description) &&
                             !string.IsNullOrEmpty(definition.condition) &&
                             !string.IsNullOrEmpty(definition.reward) &&
                             definition.coinReward > 0) &&
                         supportingContent;
            Add(lines, valid, "Достижения",
                (definitions != null ? definitions.Count : 0) +
                " определений с условиями, наградами и достижимым контентом");
            return valid;
        }

        private static bool CheckEvolutions(GameManager game, ICollection<string> lines)
        {
            List<CreatureData> roots = GetEvolutionRoots(game.Content);
            List<string> broken = new List<string>();
            foreach (CreatureData root in roots)
            {
                int forms = 1;
                string current = root.id;
                HashSet<string> visited = new HashSet<string>();
                while (visited.Add(current))
                {
                    SpeciesEvolutionData evolution = game.GetEvolution(current);
                    if (evolution == null ||
                        evolution.requiredCopies <= 0 ||
                        game.GetCreature(evolution.resultSpeciesId) == null)
                    {
                        break;
                    }

                    forms++;
                    current = evolution.resultSpeciesId;
                }

                if (forms < 3)
                {
                    broken.Add(root.creatureName);
                }
            }

            bool valid = broken.Count == 0 &&
                         FindObjectOfType<BreedingSystem>() != null &&
                         FindObjectOfType<BreedingPanel>(true) != null;
            Add(lines, valid, "Эволюции",
                roots.Count + " базовых цепочек имеют минимум три формы" +
                (broken.Count > 0 ? "; нарушены: " + string.Join(", ", broken) : ""));
            return valid;
        }

        private static bool CheckWardrobe(GameManager game, ICollection<string> lines)
        {
            BiomeType[] biomes = (BiomeType[])Enum.GetValues(typeof(BiomeType));
            List<string> brokenSets = new List<string>();
            foreach (BiomeType biome in biomes)
            {
                SignatureSetData set = game.Content.signatureSets.Find(entry =>
                    entry != null && entry.biome == biome);
                if (set == null || set.accessoryIds == null ||
                    set.accessoryIds.Count < 3 ||
                    !ContainsAllSlots(game, set) ||
                    set.signatureSpeciesIds == null ||
                    set.signatureSpeciesIds.Count == 0 ||
                    set.signatureSpeciesIds.Any(id =>
                    {
                        CreatureData creature = game.GetCreature(id);
                        return creature == null || creature.biome != biome;
                    }))
                {
                    brokenSets.Add(Localization.Biome(biome));
                }
            }

            bool dropsValid = game.Content.accessories.All(accessory =>
                accessory != null &&
                !string.IsNullOrEmpty(accessory.id) &&
                accessory.dropChance > 0f &&
                (accessory.IsSignature
                    ? biomes.Count(biome =>
                        ExplorationSystem.CanAccessoryDropInBiome(accessory, biome)) == 1
                    : biomes.All(biome =>
                        ExplorationSystem.CanAccessoryDropInBiome(accessory, biome))));
            bool valid = brokenSets.Count == 0 &&
                         dropsValid &&
                         FindObjectOfType<AccessoryInventoryManager>() != null;
            Add(lines, valid, "Гардероб",
                game.Content.accessories.Count + " предметов, " +
                game.Content.signatureSets.Count + " наборов" +
                (brokenSets.Count > 0
                    ? "; проблемные биомы: " + string.Join(", ", brokenSets)
                    : ""));
            return valid;
        }

        private static bool CheckEnergy(ICollection<string> lines)
        {
            bool valid = EnergyRegenerationSystem.MaxEnergy == 100 &&
                         EnergyRegenerationSystem.RegenerationSeconds == 45 &&
                         WorldExplorationManager.ExplorerCompassEnergyCost == 3 &&
                         FindObjectOfType<EnergyRegenerationSystem>() != null;
            Add(lines, valid, "Энергия",
                "максимум 100, +1/45 сек., компас 3");
            return valid;
        }

        private static bool CheckSprites(GameManager game, ICollection<string> lines)
        {
            SpriteDatabase database = SpriteDatabase.Active;
            bool contentResolved = database != null &&
                                   game.Content.creatures.All(creature =>
                                       database.GetCreaturePortrait(creature) != null &&
                                       database.GetCreatureWorld(creature) != null &&
                                       database.GetCreatureEvolution(creature) != null) &&
                                   game.Content.items.All(item =>
                                       database.GetItem(item) != null) &&
                                   game.Content.accessories.All(accessory =>
                                       database.GetAccessory(accessory) != null) &&
                                   game.Content.creatureNests.All(nest =>
                                       database.GetNest(nest) != null) &&
                                   game.Content.biomeEvents.All(eventData =>
                                       database.GetSpecialEvent(eventData) != null) &&
                                   game.Content.biomes.All(biome =>
                                       database.GetBiomeBackground(
                                           biome,
                                           biome != null ? biome.mapData : null) != null) &&
                                   Enum.GetValues(typeof(TrackType))
                                       .Cast<TrackType>()
                                       .All(track => database.GetTrack(track) != null);
            bool separateWorldRenderers = WorldPickup.ActivePickups.All(pickup =>
                pickup != null &&
                pickup.VisualRenderer != null &&
                pickup.VisualRenderer.transform != pickup.transform);
            bool valid = contentResolved &&
                         separateWorldRenderers &&
                         database.GetPlayer() != null &&
                         database.GetCompassArrow() != null &&
                         database.GetRoundedPanel() != null;
            Add(lines, valid, "Графика",
                "SpriteDatabase разрешает существ, логовища, следы, предметы, одежду, биомы и UI");
            return valid;
        }

        private static bool CheckCreatureTemplate(ICollection<string> lines)
        {
            CreatureVisualValidator validator =
                FindObjectOfType<CreatureVisualValidator>();
            bool valid = validator != null &&
                         validator.ValidateCreatureTemplate() &&
                         !string.IsNullOrEmpty(validator.LastReport) &&
                         validator.LastReport.Contains(
                             "CREATURE_TEMPLATE_VALIDATION_PASS");
            Add(lines, valid, "Шаблон существ",
                "2048x2048, safe zone 10%, пропорции 40/40/20 и единые якоря гардероба");
            return valid;
        }

        private static bool CheckMoney(GameManager game, ICollection<string> lines)
        {
            int[] expectedPrices = { 0, 300, 1000, 3000, 8000, 20000 };
            BiomeType[] biomes = (BiomeType[])Enum.GetValues(typeof(BiomeType));
            bool biomePrices = biomes.Length == expectedPrices.Length;
            for (int index = 0; biomePrices && index < biomes.Length; index++)
            {
                BiomeData biome = game.GetBiome(biomes[index]);
                biomePrices = biome != null &&
                              biome.unlockPrice == expectedPrices[index];
            }

            int[] hintCosts = { 50, 150, 400, 1000, 2500, 5000 };
            bool hintPrices = hintCosts.Select((cost, index) =>
                    GameManager.CalculateHintCost(
                        CreatureRarity.Common, index + 1) == cost)
                .All(value => value);
            int questIncome = game.Content.quests
                .Where(quest => quest != null)
                .Sum(quest => Mathf.Max(0, quest.rewardCoins) * 2);
            bool valid = biomePrices && hintPrices &&
                         SaveSystem.DefaultCoins <= 100 &&
                         questIncome < expectedPrices.Sum();
            Add(lines, valid, "Деньги",
                "биомы 0/300/1000/3000/8000/20000, подсказки " +
                string.Join("/", hintCosts) +
                ", старт " + SaveSystem.DefaultCoins);
            return valid;
        }

        private static bool CheckEncyclopedia(GameManager game, ICollection<string> lines)
        {
            int warnings =
                ContentValidator.ValidateEncyclopediaAvailability(game.Content);
            bool valid = warnings == 0 &&
                         game.Content.creatures.All(creature =>
                             creature != null &&
                             game.GetBiome(creature.biome) != null);
            Add(lines, valid, "Энциклопедия",
                game.Content.creatures.Count + "/" +
                game.Content.creatures.Count + " клеток достижимы");
            return valid;
        }

        private static bool CheckFavorite(ICollection<string> lines)
        {
            FollowPetController follower = FindObjectOfType<FollowPetController>();
            CreatureCollectionManager collection =
                FindObjectOfType<CreatureCollectionManager>();
            AccessoryInventoryManager inventory =
                FindObjectOfType<AccessoryInventoryManager>();
            FavoriteHelperSystem helper = FindObjectOfType<FavoriteHelperSystem>();
            bool valid = follower != null && collection != null && inventory != null &&
                         helper != null &&
                         follower.transform.root.GetComponent<MonstrologyBootstrap>() != null;
            Add(lines, valid, "Любимчик",
                "сохранение, обновление, следование и визуалы одежды подключены");
            return valid;
        }

        private bool Finish(bool ready, ICollection<string> lines)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== MONSTROLOGY BETA READINESS REPORT ===");
            foreach (string line in lines)
            {
                report.AppendLine(line);
            }

            report.Append(ready
                ? "MONSTROLOGY_BETA_READINESS_PASS"
                : "MONSTROLOGY_BETA_READINESS_FAIL");
            LastReport = report.ToString();
            if (ready)
            {
                Debug.Log(LastReport);
            }
            else
            {
                Debug.LogWarning(LastReport);
            }

            return ready;
        }

        private static List<CreatureData> GetEvolutionRoots(GameContent content)
        {
            HashSet<string> resultIds = new HashSet<string>(
                content.speciesEvolutions
                    .Where(evolution => evolution != null)
                    .Select(evolution => evolution.resultSpeciesId)
                    .Where(id => !string.IsNullOrEmpty(id)));
            return content.creatures.Where(creature =>
                creature != null &&
                !string.IsNullOrEmpty(creature.id) &&
                !resultIds.Contains(creature.id)).ToList();
        }

        private static bool ContainsAllSlots(
            GameManager game,
            SignatureSetData set)
        {
            HashSet<AccessorySlot> slots = new HashSet<AccessorySlot>();
            foreach (string accessoryId in set.accessoryIds)
            {
                AccessoryData accessory = game.GetAccessory(accessoryId);
                if (accessory != null &&
                    accessory.IsSignatureForBiome(set.biome))
                {
                    slots.Add(accessory.slot);
                }
            }

            return Enum.GetValues(typeof(AccessorySlot))
                .Cast<AccessorySlot>()
                .All(slots.Contains);
        }

        private static void Add(
            ICollection<string> lines,
            bool success,
            string section,
            string details)
        {
            lines.Add((success ? "✓ " : "✗ ") + section + ": " + details);
        }
    }
}
