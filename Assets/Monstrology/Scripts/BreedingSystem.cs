using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class BreedingSystem : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;

        public string LastMessage { get; private set; }
        public event Action<string, string> SpeciesEvolved;

        public void Initialize(GameManager gameManager, CreatureCollectionManager collectionManager)
        {
            game = gameManager;
            collection = collectionManager;
        }

        public bool CanBreed(CreatureInstance parentA, CreatureInstance parentB)
        {
            string reason;
            return CanBreed(parentA, parentB, out reason);
        }

        public bool CanBreed(CreatureInstance parentA, CreatureInstance parentB, out string reason)
        {
            if (parentA == null || parentB == null)
            {
                reason = "Выберите двух питомцев одного вида.";
                return false;
            }

            if (parentA.uniqueId == parentB.uniqueId)
            {
                reason = "Нужны два разных экземпляра одного вида.";
                return false;
            }

            if (parentA.speciesId != parentB.speciesId)
            {
                reason = "Разные виды не скрещиваются.";
                return false;
            }

            return CanEvolve(parentA.speciesId, out reason);
        }

        public bool CanEvolve(string speciesId)
        {
            string reason;
            return CanEvolve(speciesId, out reason);
        }

        public bool CanEvolve(string speciesId, out string reason)
        {
            SpeciesEvolutionData evolution = game != null ? game.GetEvolution(speciesId) : null;
            if (evolution == null || string.IsNullOrEmpty(evolution.resultSpeciesId))
            {
                reason = "У этого вида пока нет следующей формы.";
                return false;
            }

            if (game.GetCreature(evolution.resultSpeciesId) == null)
            {
                reason = "Следующая форма не найдена в контенте.";
                return false;
            }

            CreatureInstance pet = FindPetForEvolution(speciesId);
            if (pet == null)
            {
                reason = "Для эволюции нужен питомец текущей формы.";
                return false;
            }

            string rootSpeciesId = string.IsNullOrEmpty(
                pet.evolutionRootSpeciesId)
                ? game.GetEvolutionRootSpeciesId(speciesId)
                : pet.evolutionRootSpeciesId;
            int copies = game.GetLifetimeCreatureCount(rootSpeciesId);
            if (copies < evolution.requiredCopies)
            {
                reason = "Недостаточно копий: " + copies + " / " + evolution.requiredCopies + ".";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public CreatureInstance Breed(CreatureInstance parentA, CreatureInstance parentB)
        {
            string reason;
            if (!CanBreed(parentA, parentB, out reason))
            {
                LastMessage = reason;
                return null;
            }

            return EvolveSpecies(parentA.speciesId);
        }

        public CreatureInstance EvolveSpecies(string speciesId)
        {
            string reason;
            if (!CanEvolve(speciesId, out reason))
            {
                LastMessage = reason;
                return null;
            }

            SpeciesEvolutionData evolution = game.GetEvolution(speciesId);
            CreatureInstance pet = FindPetForEvolution(speciesId);
            CreatureData oldSpecies = game.GetCreature(speciesId);
            CreatureData resultSpecies = game.GetCreature(evolution.resultSpeciesId);
            if (pet == null)
            {
                pet = collection.AddPet(evolution.resultSpeciesId);
            }
            else
            {
                bool usedDefaultName = string.IsNullOrEmpty(pet.customName) ||
                                       (oldSpecies != null && pet.customName == oldSpecies.creatureName);
                pet.speciesId = evolution.resultSpeciesId;
                pet.evolutionRootSpeciesId = string.IsNullOrEmpty(pet.evolutionRootSpeciesId)
                    ? game.GetEvolutionRootSpeciesId(speciesId)
                    : pet.evolutionRootSpeciesId;
                pet.evolutionStage++;
                pet.generation = Mathf.Max(pet.generation, pet.evolutionStage);
                if (usedDefaultName)
                {
                    pet.customName = resultSpecies.creatureName;
                }

                collection.SaveNow();
            }

            game.AddCreature(resultSpecies);
            if (SpeciesEvolved != null)
            {
                SpeciesEvolved(speciesId, evolution.resultSpeciesId);
            }

            LastMessage = "Открыта новая форма: " + resultSpecies.creatureName + ".";
            Debug.Log("Species evolved: " + speciesId + " -> " + evolution.resultSpeciesId);
            return pet;
        }

        public string CalculateOffspringSpecies(CreatureInstance parentA, CreatureInstance parentB)
        {
            if (parentA == null || parentB == null || parentA.speciesId != parentB.speciesId)
            {
                return string.Empty;
            }

            SpeciesEvolutionData evolution = game != null ? game.GetEvolution(parentA.speciesId) : null;
            return evolution != null ? evolution.resultSpeciesId : string.Empty;
        }

        public PetRarity CalculateOffspringRarity(CreatureInstance parentA, CreatureInstance parentB)
        {
            if (parentA == null || parentB == null)
            {
                return PetRarity.Common;
            }

            return (PetRarity)Mathf.Max((int)parentA.rarity, (int)parentB.rarity);
        }

        public CreatureGenetics CalculateOffspringGenetics(
            CreatureInstance parentA,
            CreatureInstance parentB)
        {
            return CreatureGenetics.Inherit(
                parentA != null ? parentA.genetics : null,
                parentB != null ? parentB.genetics : null);
        }

        public CreatureInstance CreateOffspringInstance(
            CreatureInstance parentA,
            CreatureInstance parentB,
            string speciesId,
            PetRarity rarity,
            CreatureGenetics genetics)
        {
            LastMessage = "Создание случайных потомков отключено. Используйте эволюцию вида.";
            return null;
        }

        public BreedingRecipeData FindRecipe(string speciesA, string speciesB)
        {
            return null;
        }

        public float GetSuccessChance(CreatureInstance parentA, CreatureInstance parentB)
        {
            return parentA != null && parentB != null && parentA.speciesId == parentB.speciesId ? 1f : 0f;
        }

        public string GetPotentialSpecies(CreatureInstance parentA, CreatureInstance parentB)
        {
            return CalculateOffspringSpecies(parentA, parentB);
        }

        private CreatureInstance FindPetForEvolution(string speciesId)
        {
            if (collection == null)
            {
                return null;
            }

            foreach (CreatureInstance pet in collection.GetAllPets())
            {
                if (pet != null && pet.speciesId == speciesId)
                {
                    return pet;
                }
            }

            return null;
        }
    }
}
