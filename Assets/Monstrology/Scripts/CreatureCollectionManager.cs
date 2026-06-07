using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Monstrology
{
    public class CreatureCollectionManager : MonoBehaviour
    {
        public const int ExplorationExperience = 5;
        public const int CreatureExperience = 15;
        public const int BiomeUnlockExperience = 40;

        private readonly List<CreatureInstance> pets = new List<CreatureInstance>();
        private GameManager game;

        public event Action CollectionChanged;
        public int Count { get { return pets.Count; } }

        public void Initialize(GameManager gameManager)
        {
            if (game != null)
            {
                Unsubscribe();
            }

            game = gameManager;
            ReloadFromProgress();

            if (game != null)
            {
                game.ExplorationRegistered += HandleExploration;
                game.CreatureRegistered += HandleCreatureRegistered;
                game.BiomeUnlocked += HandleBiomeUnlocked;
                game.ProgressReset += HandleProgressReset;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public CreatureInstance AddPet(string speciesId, string customName = null)
        {
            if (game == null || string.IsNullOrWhiteSpace(speciesId))
            {
                return null;
            }

            CreatureData species = game.GetCreature(speciesId);
            if (species == null)
            {
                return null;
            }

            CreatureInstance pet = new CreatureInstance
            {
                uniqueId = Guid.NewGuid().ToString("N"),
                speciesId = species.id,
                customName = string.IsNullOrWhiteSpace(customName)
                    ? species.creatureName
                    : customName.Trim(),
                level = 1,
                experience = 0,
                rarity = RollRarity(species),
                ageInDays = 0,
                isFavorite = false,
                obtainedDate = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                personalityType = (PersonalityType)UnityEngine.Random.Range(
                    0, Enum.GetValues(typeof(PersonalityType)).Length)
            };

            pets.Add(pet);
            SaveCollection();
            return pet;
        }

        public bool RemovePet(string uniqueId)
        {
            CreatureInstance pet = GetPetById(uniqueId);
            if (pet == null)
            {
                return false;
            }

            pets.Remove(pet);
            SaveCollection();
            return true;
        }

        public CreatureInstance GetPetById(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                return null;
            }

            return pets.Find(pet => pet != null && pet.uniqueId == uniqueId);
        }

        public IReadOnlyList<CreatureInstance> GetAllPets()
        {
            RefreshAges();
            return pets.AsReadOnly();
        }

        public CreatureInstance GetFavorite()
        {
            return pets.Find(pet => pet != null && pet.isFavorite);
        }

        public bool SetFavorite(string uniqueId)
        {
            CreatureInstance selected = GetPetById(uniqueId);
            if (selected == null)
            {
                return false;
            }

            foreach (CreatureInstance pet in pets)
            {
                if (pet != null)
                {
                    pet.isFavorite = pet == selected;
                }
            }

            SaveCollection();
            return true;
        }

        public bool RenamePet(string uniqueId, string customName)
        {
            CreatureInstance pet = GetPetById(uniqueId);
            if (pet == null)
            {
                return false;
            }

            CreatureData species = game != null ? game.GetCreature(pet.speciesId) : null;
            pet.customName = string.IsNullOrWhiteSpace(customName)
                ? species != null ? species.creatureName : pet.speciesId
                : customName.Trim();
            SaveCollection();
            return true;
        }

        public void AddExperienceToAll(int amount)
        {
            if (amount <= 0 || pets.Count == 0)
            {
                return;
            }

            foreach (CreatureInstance pet in pets)
            {
                AddExperience(pet, amount);
            }

            SaveCollection();
        }

        public int GetExperienceForNextLevel(CreatureInstance pet)
        {
            return pet == null ? 100 : 100 + Mathf.Max(0, pet.level - 1) * 50;
        }

        private void AddExperience(CreatureInstance pet, int amount)
        {
            if (pet == null)
            {
                return;
            }

            pet.experience = Mathf.Max(0, pet.experience + amount);
            int required = GetExperienceForNextLevel(pet);
            while (pet.experience >= required)
            {
                pet.experience -= required;
                pet.level++;
                required = GetExperienceForNextLevel(pet);
            }
        }

        private void HandleExploration()
        {
            AddExperienceToAll(ExplorationExperience);
        }

        private void HandleCreatureRegistered(CreatureData creature, bool firstDiscovery)
        {
            AddExperienceToAll(CreatureExperience);
        }

        private void HandleBiomeUnlocked(BiomeData biome)
        {
            AddExperienceToAll(BiomeUnlockExperience);
        }

        private void HandleProgressReset()
        {
            ReloadFromProgress();
        }

        private void ReloadFromProgress()
        {
            pets.Clear();
            if (game != null)
            {
                List<CreatureInstance> savedPets = game.GetSavedPets();
                if (savedPets != null)
                {
                    pets.AddRange(savedPets.FindAll(pet => pet != null));
                }
            }

            NormalizePets();
            RaiseCollectionChanged();
        }

        private void NormalizePets()
        {
            bool favoriteFound = false;
            for (int index = pets.Count - 1; index >= 0; index--)
            {
                CreatureInstance pet = pets[index];
                if (pet == null || string.IsNullOrEmpty(pet.speciesId))
                {
                    pets.RemoveAt(index);
                    continue;
                }

                if (string.IsNullOrEmpty(pet.uniqueId))
                {
                    pet.uniqueId = Guid.NewGuid().ToString("N");
                }

                pet.level = Mathf.Max(1, pet.level);
                pet.experience = Mathf.Max(0, pet.experience);
                pet.accessorySlots = pet.accessorySlots ?? new List<AccessorySlot>();
                pet.breedingData = pet.breedingData ?? new CreatureBreedingData();
                pet.geneticsData = pet.geneticsData ?? new CreatureGeneticsData();
                pet.RefreshAge();

                if (pet.isFavorite)
                {
                    pet.isFavorite = !favoriteFound;
                    favoriteFound = true;
                }
            }
        }

        private void RefreshAges()
        {
            foreach (CreatureInstance pet in pets)
            {
                if (pet != null)
                {
                    pet.RefreshAge();
                }
            }
        }

        private void SaveCollection()
        {
            NormalizePets();
            if (game != null)
            {
                game.SavePets(pets);
            }

            RaiseCollectionChanged();
        }

        private void RaiseCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged();
            }
        }

        private void Unsubscribe()
        {
            if (game == null)
            {
                return;
            }

            game.ExplorationRegistered -= HandleExploration;
            game.CreatureRegistered -= HandleCreatureRegistered;
            game.BiomeUnlocked -= HandleBiomeUnlocked;
            game.ProgressReset -= HandleProgressReset;
        }

        private static PetRarity RollRarity(CreatureData species)
        {
            float rarityBonus = 0f;
            if (species != null)
            {
                switch (species.rarity)
                {
                    case CreatureRarity.Rare:
                        rarityBonus = 8f;
                        break;
                    case CreatureRarity.Epic:
                        rarityBonus = 16f;
                        break;
                    case CreatureRarity.Legendary:
                        rarityBonus = 25f;
                        break;
                    case CreatureRarity.Secret:
                        rarityBonus = 32f;
                        break;
                }
            }

            float roll = Mathf.Clamp(UnityEngine.Random.Range(0f, 100f) + rarityBonus, 0f, 100f);
            if (roll >= 99f) return PetRarity.Mythic;
            if (roll >= 94f) return PetRarity.Legendary;
            if (roll >= 82f) return PetRarity.Epic;
            if (roll >= 62f) return PetRarity.Rare;
            if (roll >= 34f) return PetRarity.Uncommon;
            return PetRarity.Common;
        }
    }
}
