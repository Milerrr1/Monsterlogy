using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Monstrology
{
    public enum PetRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum PersonalityType
    {
        Curious,
        Lazy,
        Friendly,
        Aggressive,
        Shy,
        Playful
    }

    [Serializable]
    public class AccessorySlot
    {
        public string slotId;
        public string equippedAccessoryId;
    }

    [Serializable]
    public class CreatureBreedingData
    {
        public int dataVersion = 1;
    }

    [Serializable]
    public class CreatureGeneticsData
    {
        public int dataVersion = 1;
    }

    [Serializable]
    public class CreatureInstance
    {
        public string uniqueId;
        public string speciesId;
        public string customName;
        [Min(1)] public int level = 1;
        [Min(0)] public int experience;
        public PetRarity rarity;
        [Min(0)] public int ageInDays;
        public bool isFavorite;
        public string obtainedDate;
        public List<AccessorySlot> accessorySlots = new List<AccessorySlot>();
        public PersonalityType personalityType;

        public CreatureBreedingData breedingData = new CreatureBreedingData();
        public CreatureGeneticsData geneticsData = new CreatureGeneticsData();

        public string GetDisplayName(CreatureData species)
        {
            if (!string.IsNullOrWhiteSpace(customName))
            {
                return customName.Trim();
            }

            return species != null && !string.IsNullOrWhiteSpace(species.creatureName)
                ? species.creatureName
                : speciesId;
        }

        public int RefreshAge()
        {
            DateTime obtained;
            if (!DateTime.TryParse(
                    obtainedDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out obtained))
            {
                obtained = DateTime.UtcNow;
                obtainedDate = obtained.ToString("o", CultureInfo.InvariantCulture);
            }

            ageInDays = Mathf.Max(0, (int)(DateTime.UtcNow.Date - obtained.ToUniversalTime().Date).TotalDays);
            return ageInDays;
        }
    }

    public static class PetLocalization
    {
        public static string Rarity(PetRarity rarity)
        {
            switch (rarity)
            {
                case PetRarity.Uncommon: return "Необычный";
                case PetRarity.Rare: return "Редкий";
                case PetRarity.Epic: return "Эпический";
                case PetRarity.Legendary: return "Легендарный";
                case PetRarity.Mythic: return "Мифический";
                default: return "Обычный";
            }
        }

        public static Color RarityColor(PetRarity rarity)
        {
            switch (rarity)
            {
                case PetRarity.Uncommon: return new Color(0.38f, 0.82f, 0.5f);
                case PetRarity.Rare: return new Color(0.3f, 0.65f, 1f);
                case PetRarity.Epic: return new Color(0.7f, 0.42f, 0.96f);
                case PetRarity.Legendary: return new Color(1f, 0.68f, 0.2f);
                case PetRarity.Mythic: return new Color(1f, 0.34f, 0.66f);
                default: return new Color(0.76f, 0.8f, 0.86f);
            }
        }

        public static string Personality(PersonalityType personality)
        {
            switch (personality)
            {
                case PersonalityType.Lazy: return "Ленивый";
                case PersonalityType.Friendly: return "Дружелюбный";
                case PersonalityType.Aggressive: return "Задиристый";
                case PersonalityType.Shy: return "Застенчивый";
                case PersonalityType.Playful: return "Игривый";
                default: return "Любопытный";
            }
        }
    }
}
