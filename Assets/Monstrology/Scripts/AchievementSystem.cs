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

        public event Action AchievementsChanged;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            BreedingSystem evolutionSystem)
        {
            Unsubscribe();
            game = gameManager;
            collection = collectionManager;
            evolution = evolutionSystem;
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
        }
    }
}
