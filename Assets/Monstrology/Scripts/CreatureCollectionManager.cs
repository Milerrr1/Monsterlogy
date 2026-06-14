using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Monstrology
{
    public class CreatureCollectionManager : MonoBehaviour
    {
        public const int ExplorationExperience = 5;
        public const int CreatureExperience = 15;
        public const int BiomeUnlockExperience = 40;
        public const int MaxCustomNameLength = 24;
        public const int CurrentPetMigrationVersion = 1;

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
                game.ProgressReset += HandleProgressReset;
                game.CreatureRegistered += HandleCreatureRegistered;
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

            CreatureInstance existing = GetPetBySpecies(speciesId);
            if (existing != null)
            {
                return existing;
            }

            CreatureInstance pet = new CreatureInstance
            {
                uniqueId = Guid.NewGuid().ToString("N"),
                speciesId = species.id,
                evolutionRootSpeciesId = species.id,
                evolutionStage = 0,
                customName = SanitizeCustomName(
                    customName,
                    species.creatureName),
                level = 1,
                experience = 0,
                rarity = RollRarity(species),
                ageInDays = 0,
                isFavorite = false,
                obtainedDate = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                genetics = null,
                equippedAccessories = new List<EquippedAccessory>(),
                breedingCooldownEndTime = string.Empty,
                parentsIds = new List<string>(),
                generation = 0,
                isWildCaught = true,
                totalBreedCount = 0,
                personalityType = (PersonalityType)UnityEngine.Random.Range(
                    0, Enum.GetValues(typeof(PersonalityType)).Length)
            };
            pet.genetics = CreatureGenetics.CreateWild(pet.rarity);

            pets.Add(pet);
            SaveCollection();
            Debug.Log("Pet created: " + pet.uniqueId + " (" + pet.speciesId + ")");
            return pet;
        }

        public bool AddPetInstance(CreatureInstance pet)
        {
            if (game == null || pet == null || string.IsNullOrWhiteSpace(pet.speciesId) ||
                game.GetCreature(pet.speciesId) == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(pet.uniqueId))
            {
                pet.uniqueId = Guid.NewGuid().ToString("N");
            }

            if (GetPetById(pet.uniqueId) != null)
            {
                return false;
            }

            CreatureInstance existingSpecies = GetPetBySpecies(pet.speciesId);
            if (existingSpecies != null)
            {
                MergePetData(existingSpecies, pet);
                SaveCollection();
                return true;
            }

            pets.Add(pet);
            SaveCollection();
            Debug.Log("Pet created: " + pet.uniqueId + " (" + pet.speciesId + ")");
            return true;
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

        public CreatureInstance GetPetBySpecies(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId))
            {
                return null;
            }

            return pets.Find(pet => pet != null && pet.speciesId == speciesId);
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
            Debug.Log("Favorite pet selected: " + selected.uniqueId);
            return true;
        }

        public void ClearPets()
        {
            pets.Clear();
            SaveCollection();
        }

        public void SaveNow()
        {
            SaveCollection();
        }

        public bool RenamePet(string uniqueId, string customName)
        {
            CreatureInstance pet = GetPetById(uniqueId);
            if (pet == null)
            {
                return false;
            }

            CreatureData species = game != null ? game.GetCreature(pet.speciesId) : null;
            pet.customName = SanitizeCustomName(
                customName,
                species != null ? species.creatureName : pet.speciesId);
            SaveCollection();
            return true;
        }

        public static string SanitizeCustomName(
            string value,
            string fallback)
        {
            StringBuilder builder = new StringBuilder();
            bool previousWasWhitespace = false;
            string source = value ?? string.Empty;
            foreach (char character in source)
            {
                if (char.IsControl(character))
                {
                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    if (builder.Length > 0 && !previousWasWhitespace)
                    {
                        builder.Append(' ');
                    }

                    previousWasWhitespace = true;
                    continue;
                }

                builder.Append(character);
                previousWasWhitespace = false;
                if (builder.Length >= MaxCustomNameLength)
                {
                    break;
                }
            }

            string normalized = builder.ToString().Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }

            string safeFallback = string.IsNullOrWhiteSpace(fallback)
                ? "Pet"
                : fallback.Trim();
            return safeFallback.Length > MaxCustomNameLength
                ? safeFallback.Substring(0, MaxCustomNameLength)
                : safeFallback;
        }

        public void AddExperienceToAll(int amount)
        {
            // Legacy API retained for save and integration compatibility.
            // Pet levels are now increased only by PetUpgradeSystem resources.
        }

        public int GetExperienceForNextLevel(CreatureInstance pet)
        {
            return pet == null ? 100 : 100 + Mathf.Max(0, pet.level - 1) * 50;
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
            RestoreMissingDiscoveredPets();
            if (game != null)
            {
                game.MarkPetMigrationComplete(
                    CurrentPetMigrationVersion);
            }
            RaiseCollectionChanged();
        }

        private void HandleCreatureRegistered(
            CreatureData creature,
            bool firstDiscovery)
        {
            if (creature == null ||
                string.IsNullOrEmpty(creature.id) ||
                GetPetBySpecies(creature.id) != null)
            {
                return;
            }

            AddPet(creature.id);
        }

        private void RestoreMissingDiscoveredPets()
        {
            if (game == null || game.Content == null ||
                game.Content.creatures == null)
            {
                return;
            }

            foreach (CreatureData species in game.Content.creatures)
            {
                if (species == null ||
                    string.IsNullOrEmpty(species.id) ||
                    !game.IsCreatureFound(species.id) ||
                    GetPetBySpecies(species.id) != null)
                {
                    continue;
                }

                CreatureInstance restored = AddPet(species.id);
                if (restored != null && species.id == "vacuum_rhino")
                {
                    Debug.Log(
                        "[PetMigration] Restored missing pet entry for " +
                        "vacuum_rhino.");
                }
            }
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

                pet.level = Mathf.Clamp(pet.level, 1, PetUpgradeSystem.MaxLevel);
                pet.experience = Mathf.Max(0, pet.experience);
                CreatureData species = game != null
                    ? game.GetCreature(pet.speciesId)
                    : null;
                pet.customName = SanitizeCustomName(
                    pet.customName,
                    species != null
                        ? species.creatureName
                        : pet.speciesId);
                pet.evolutionRootSpeciesId = string.IsNullOrEmpty(pet.evolutionRootSpeciesId)
                    ? pet.speciesId
                    : pet.evolutionRootSpeciesId;
                pet.evolutionStage = Mathf.Max(0, pet.evolutionStage);
                pet.accessorySlots = pet.accessorySlots ?? new List<EquippedAccessory>();
                pet.breedingData = pet.breedingData ?? new CreatureBreedingData();
                pet.geneticsData = pet.geneticsData ?? new CreatureGeneticsData();
                pet.equippedAccessories = pet.equippedAccessories ?? new List<EquippedAccessory>();
                if (pet.equippedAccessories.Count == 0 && pet.accessorySlots.Count > 0)
                {
                    pet.equippedAccessories.AddRange(pet.accessorySlots.FindAll(slot => slot != null));
                }

                MigrateEquipmentSlots(pet);
                pet.genetics = pet.genetics ?? CreatureGenetics.CreateWild(pet.rarity);
                pet.genetics.Normalize();
                pet.parentsIds = pet.parentsIds ?? new List<string>();
                pet.breedingCooldownEndTime = pet.breedingCooldownEndTime ?? string.Empty;
                pet.generation = Mathf.Max(0, pet.generation);
                pet.totalBreedCount = Mathf.Max(0, pet.totalBreedCount);
                if (pet.generation == 0 && pet.parentsIds.Count == 0)
                {
                    pet.isWildCaught = true;
                }

                pet.RefreshAge();

                if (pet.isFavorite)
                {
                    pet.isFavorite = !favoriteFound;
                    favoriteFound = true;
                }
            }

            MergeDuplicateSpecies();
        }

        private void MergeDuplicateSpecies()
        {
            Dictionary<string, CreatureInstance> unique = new Dictionary<string, CreatureInstance>();
            Dictionary<string, int> convertedCopies = new Dictionary<string, int>();
            for (int index = pets.Count - 1; index >= 0; index--)
            {
                CreatureInstance pet = pets[index];
                CreatureInstance existing;
                if (!unique.TryGetValue(pet.speciesId, out existing))
                {
                    unique.Add(pet.speciesId, pet);
                    continue;
                }

                MergePetData(existing, pet);
                pets.RemoveAt(index);
                int count;
                convertedCopies.TryGetValue(pet.speciesId, out count);
                convertedCopies[pet.speciesId] = count + 1;
            }

            if (game == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> entry in convertedCopies)
            {
                game.AddCreatureCopies(entry.Key, entry.Value);
            }
        }

        private static void MergePetData(CreatureInstance target, CreatureInstance source)
        {
            if (target == null || source == null)
            {
                return;
            }

            bool sourceIsStronger = source.level > target.level ||
                (source.level == target.level && source.experience > target.experience);
            if (source.level > target.level)
            {
                target.level = source.level;
                target.experience = source.experience;
            }
            else if (source.level == target.level)
            {
                target.experience = Mathf.Max(target.experience, source.experience);
            }

            target.rarity = (PetRarity)Mathf.Max((int)target.rarity, (int)source.rarity);
            target.evolutionStage = Mathf.Max(target.evolutionStage, source.evolutionStage);
            target.generation = Mathf.Max(target.generation, source.generation);
            target.totalBreedCount = Mathf.Max(target.totalBreedCount, source.totalBreedCount);
            target.isFavorite = target.isFavorite || source.isFavorite;

            if (sourceIsStronger)
            {
                target.personalityType = source.personalityType;
                target.ageInDays = Mathf.Max(target.ageInDays, source.ageInDays);
                if (!string.IsNullOrWhiteSpace(source.customName))
                {
                    target.customName = source.customName;
                }

                if (source.genetics != null)
                {
                    target.genetics = new CreatureGenetics
                    {
                        sizeGene = source.genetics.sizeGene,
                        colorGene = source.genetics.colorGene,
                        energyGene = source.genetics.energyGene,
                        luckGene = source.genetics.luckGene,
                        mutationGene = source.genetics.mutationGene
                    };
                    target.genetics.Normalize();
                }
            }

            if (!sourceIsStronger && string.IsNullOrWhiteSpace(target.customName) &&
                !string.IsNullOrWhiteSpace(source.customName))
            {
                target.customName = source.customName;
            }

            target.equippedAccessories = target.equippedAccessories ?? new List<EquippedAccessory>();
            if (source.equippedAccessories != null)
            {
                foreach (EquippedAccessory sourceAccessory in source.equippedAccessories)
                {
                    if (sourceAccessory == null ||
                        string.IsNullOrEmpty(sourceAccessory.equippedAccessoryId))
                    {
                        continue;
                    }

                    EquippedAccessory existing = target.equippedAccessories.Find(entry =>
                        entry != null && entry.slot == sourceAccessory.slot);
                    if (existing == null)
                    {
                        target.equippedAccessories.Add(new EquippedAccessory
                        {
                            slot = sourceAccessory.slot,
                            equippedAccessoryId = sourceAccessory.equippedAccessoryId
                        });
                    }
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

            game.ProgressReset -= HandleProgressReset;
            game.CreatureRegistered -= HandleCreatureRegistered;
        }

        private void MigrateEquipmentSlots(CreatureInstance pet)
        {
            List<EquippedAccessory> migrated = new List<EquippedAccessory>();
            foreach (EquippedAccessory equipped in pet.equippedAccessories)
            {
                AccessoryData accessory = equipped != null && game != null
                    ? game.GetAccessory(equipped.equippedAccessoryId)
                    : null;
                if (accessory == null)
                {
                    continue;
                }

                EquippedAccessory existing = migrated.Find(entry => entry.slot == accessory.slot);
                if (existing == null)
                {
                    migrated.Add(new EquippedAccessory
                    {
                        slot = accessory.slot,
                        equippedAccessoryId = accessory.id
                    });
                }
                else
                {
                    existing.equippedAccessoryId = accessory.id;
                }
            }

            pet.equippedAccessories = migrated;
            pet.accessorySlots = new List<EquippedAccessory>(migrated);
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
