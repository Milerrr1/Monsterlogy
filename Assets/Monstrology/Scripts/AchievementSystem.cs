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
        public string condition;
        public string reward;
        [Min(0)] public int coinReward;
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

            if (collection != null)
            {
                collection.CollectionChanged += HandleCollectionChanged;
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
                description = "Первая запись профессора Монстролога.",
                condition = "Открыть первый вид.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "hundred_explorations",
                title = "Опытный исследователь",
                description = "Карта мира уже покрыта заметками.",
                condition = "Совершить 100 исследований.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "forest_collection",
                title = "Все существа леса",
                description = "Лесная глава энциклопедии завершена.",
                condition = "Открыть все встречающиеся в лесу виды.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_legendary",
                title = "Первая легендарка",
                description = "Редчайшая встреча подтверждена.",
                condition = "Открыть легендарное или секретное существо.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_pet",
                title = "Первый питомец",
                description = "В экспедиции появился постоянный спутник.",
                condition = "Добавить первое существо в питомцы.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_favorite",
                title = "Первый любимчик",
                description = "Один питомец стал главным помощником.",
                condition = "Назначить первого любимчика.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_evolution",
                title = "Первая эволюция",
                description = "Открыта следующая форма вида.",
                condition = "Эволюционировать вид.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_nest",
                title = "Первое логово",
                description = "Найдена постоянная точка наблюдения.",
                condition = "Обнаружить первое логовище существа.",
                reward = "Уведомление и запись в журнале."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_full_set",
                title = "Первый полный комплект",
                description = "Тематический образ собран полностью.",
                condition = "Собрать все три предмета сигнатурного комплекта.",
                reward = "Уведомление и бонус комплекта."
            });
            definitions.Add(new AchievementDefinition
            {
                id = "first_pet_level_10",
                title = "Опытный любимчик",
                description = "Питомец заметно вырос за время экспедиции.",
                condition = "Повысить питомца до 10 уровня.",
                reward = "Уведомление и запись в журнале."
            });
            AddBiomeAchievement("complete_tundra", "Полная энциклопедия тундры", BiomeType.Tundra);
            AddBiomeAchievement("complete_volcano", "Полная энциклопедия вулкана", BiomeType.Volcano);
            foreach (AchievementDefinition definition in definitions)
            {
                definition.coinReward = GetCoinReward(definition.id);
                definition.reward = "+" + definition.coinReward +
                                    " монет и уведомление.";
            }
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

            if (collection != null && collection.Count > 0)
            {
                Unlock("first_pet");
            }

            if (collection != null && collection.GetFavorite() != null)
            {
                Unlock("first_favorite");
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

        private void HandleCollectionChanged()
        {
            EvaluateAll();
        }

        private void AddBiomeAchievement(string id, string title, BiomeType biome)
        {
            definitions.Add(new AchievementDefinition
            {
                id = id,
                title = title,
                description = "Энциклопедия биома заполнена.",
                condition = "Открыть всех существ биома «" + Localization.Biome(biome) + "».",
                reward = "Уведомление и запись в журнале."
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
            if (definition != null && definition.coinReward > 0)
            {
                game.AddCoins(definition.coinReward);
            }

            game.RaiseNotification("Достижение: " +
                                   (definition != null ? definition.title : id) +
                                   (definition != null && definition.coinReward > 0
                                       ? " • +" + definition.coinReward + " монет"
                                       : ""));
            RaiseChanged();
        }

        private static int GetCoinReward(string id)
        {
            switch (id)
            {
                case "first_creature":
                case "first_pet":
                case "first_favorite":
                    return 25;
                case "first_nest":
                case "first_full_set":
                case "first_evolution":
                    return 60;
                case "first_pet_level_10":
                case "hundred_explorations":
                    return 100;
                case "first_legendary":
                    return 150;
                case "forest_collection":
                case "complete_tundra":
                case "complete_volcano":
                    return 200;
                default:
                    return 40;
            }
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

            if (collection != null)
            {
                collection.CollectionChanged -= HandleCollectionChanged;
            }
        }
    }
}
