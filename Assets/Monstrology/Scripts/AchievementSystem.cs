using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string title;
        public string description;
    }

    public class AchievementSystem : MonoBehaviour
    {
        private readonly List<AchievementDefinition> definitions = new List<AchievementDefinition>();
        private GameManager game;
        private CreatureCollectionManager collection;
        private BreedingSystem evolution;
        private CreatureNestSystem nests;
        private SignatureSetSystem signatureSets;
        private PetUpgradeSystem upgrades;

        public event Action AchievementsChanged;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            BreedingSystem evolutionSystem,
            CreatureNestSystem nestSystem = null,
            SignatureSetSystem signatureSetSystem = null,
            PetUpgradeSystem upgradeSystem = null)
        {
            Unsubscribe();
            game = gameManager;
            collection = collectionManager;
            evolution = evolutionSystem;
            nests = nestSystem != null ? nestSystem : FindObjectOfType<CreatureNestSystem>();
            signatureSets = signatureSetSystem != null
                ? signatureSetSystem
                : FindObjectOfType<SignatureSetSystem>();
            upgrades = upgradeSystem != null ? upgradeSystem : FindObjectOfType<PetUpgradeSystem>();
            BuildDefinitions();

            if (game != null)
            {
                game.CreatureRegistered += HandleCreatureRegistered;
                game.ExplorationRegistered += HandleExploration;
                game.ProgressReset += HandleProgressReset;
            }

            if (evolution != null)
            {
                evolution.SpeciesEvolved += HandleSpeciesEvolved;
            }

            if (nests != null)
            {
                nests.NestDiscovered += HandleNestDiscovered;
            }

            if (signatureSets != null)
            {
                signatureSets.FullSetCompleted += HandleFullSetCompleted;
            }

            if (upgrades != null)
            {
                upgrades.PetLeveled += HandlePetLeveled;
            }

            EvaluateAll();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public IReadOnlyList<AchievementDefinition> GetDefinitions()
        {
            return definitions.AsReadOnly();
        }

        public bool IsUnlocked(string id)
        {
            return game != null && game.IsAchievementUnlocked(id);
        }

        private void BuildDefinitions()
        {
            definitions.Clear();
            definitions.Add(new AchievementDefinition
            {
                id = "first_creature",
                title = "Первое существо",
                description = "Открыть первый вид."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "hundred_explorations",
                title = "Опытный исследователь",
                description = "Совершить 100 исследований."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "forest_collection",
                title = "Все существа леса",
                description = "Открыть все встречающиеся в лесу виды."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_legendary",
                title = "Первая легендарка",
                description = "Открыть легендарное или секретное существо."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_evolution",
                title = "Первая эволюция",
                description = "Эволюционировать вид."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_nest",
                title = "Первое логово",
                description = "Обнаружить первое логовище существа."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_full_set",
                title = "Первый полный комплект",
                description = "Собрать все три предмета сигнатурного комплекта."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_pet_level_10",
                title = "Опытный любимчик",
                description = "Повысить питомца до 10 уровня."
            });
            AddBiomeAchievement("complete_tundra", "Полная энциклопедия тундры", BiomeType.Tundra);
            AddBiomeAchievement("complete_volcano", "Полная энциклопедия вулкана", BiomeType.Volcano);
        }

        private void EvaluateAll()
        {
            if (game == null)
            {
                return;
            }

            if (game.TotalCreaturesFound > 0)
            {
                Unlock("first_creature");
            }

            if (game.ExplorationCount >= 100)
            {
                Unlock("hundred_explorations");
            }

            if (game.Content.creatures.Any(creature =>
                    creature != null && game.IsCreatureFound(creature.id) &&
                    (creature.rarity == CreatureRarity.Legendary ||
                     creature.rarity == CreatureRarity.Secret)))
            {
                Unlock("first_legendary");
            }

            List<CreatureData> forestCreatures = game.Content.creatures.FindAll(creature =>
                creature != null && creature.biome == BiomeType.Forest &&
                creature.appearanceChance > 0.001f);
            if (forestCreatures.Count > 0 &&
                forestCreatures.TrueForAll(creature => game.IsCreatureFound(creature.id)))
            {
                Unlock("forest_collection");
            }

            if (collection != null &&
                collection.GetAllPets().Any(pet => pet != null && pet.evolutionStage > 0))
            {
                Unlock("first_evolution");
            }

            if (nests != null && nests.GetAllNests().Count > 0)
            {
                Unlock("first_nest");
            }

            if (game.GetCompletedSignatureSets().Count > 0)
            {
                Unlock("first_full_set");
            }

            if (collection != null &&
                collection.GetAllPets().Any(pet => pet != null && pet.level >= 10))
            {
                Unlock("first_pet_level_10");
            }

            EvaluateBiomeAchievement("complete_tundra", BiomeType.Tundra);
            EvaluateBiomeAchievement("complete_volcano", BiomeType.Volcano);
        }

        private void HandleCreatureRegistered(CreatureData creature, bool firstDiscovery)
        {
            Unlock("first_creature");
            if (creature != null &&
                (creature.rarity == CreatureRarity.Legendary ||
                 creature.rarity == CreatureRarity.Secret))
            {
                Unlock("first_legendary");
            }

            EvaluateAll();
        }

        private void HandleExploration()
        {
            if (game != null && game.ExplorationCount >= 100)
            {
                Unlock("hundred_explorations");
            }
        }

        private void HandleSpeciesEvolved(string baseSpeciesId, string resultSpeciesId)
        {
            Unlock("first_evolution");
        }

        private void HandleNestDiscovered(string nestId)
        {
            Unlock("first_nest");
        }

        private void HandleFullSetCompleted(string setId)
        {
            Unlock("first_full_set");
        }

        private void HandlePetLeveled(CreatureInstance pet)
        {
            if (pet != null && pet.level >= 10)
            {
                Unlock("first_pet_level_10");
            }
        }

        private void AddBiomeAchievement(string id, string title, BiomeType biome)
        {
            definitions.Add(new AchievementDefinition
            {
                id = id,
                title = title,
                description = "Открыть всех существ биома «" + Localization.Biome(biome) + "»."
            });
        }

        private void EvaluateBiomeAchievement(string id, BiomeType biome)
        {
            if (game != null && game.IsBiomeEncyclopediaComplete(biome))
            {
                Unlock(id);
            }
        }

        private void HandleProgressReset()
        {
            EvaluateAll();
            RaiseChanged();
        }

        private void Unlock(string id)
        {
            if (game == null || !game.UnlockAchievement(id))
            {
                return;
            }

            AchievementDefinition definition = definitions.Find(entry => entry.id == id);
            game.RaiseNotification("Достижение: " +
                                   (definition != null ? definition.title : id));
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (AchievementsChanged != null)
            {
                AchievementsChanged();
            }
        }

        private void Unsubscribe()
        {
            if (game != null)
            {
                game.CreatureRegistered -= HandleCreatureRegistered;
                game.ExplorationRegistered -= HandleExploration;
                game.ProgressReset -= HandleProgressReset;
            }

            if (evolution != null)
            {
                evolution.SpeciesEvolved -= HandleSpeciesEvolved;
            }

            if (nests != null)
            {
                nests.NestDiscovered -= HandleNestDiscovered;
            }

            if (signatureSets != null)
            {
                signatureSets.FullSetCompleted -= HandleFullSetCompleted;
            }

            if (upgrades != null)
            {
                upgrades.PetLeveled -= HandlePetLeveled;
            }
        }
    }
}
