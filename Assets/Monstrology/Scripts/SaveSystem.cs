using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class StringIntEntry
    {
        public string id;
        public int value;

        public StringIntEntry(string id, int value)
        {
            this.id = id;
            this.value = value;
        }
    }

    [Serializable]
    public class GameProgress
    {
        public int version = 6;
        public int coins = 120;
        public int energy = 20;
        public string currentBiome = BiomeType.Forest.ToString();
        public int explorationCount;
        public int mutationCount;
        public int totalCreaturesFound;
        public List<string> unlockedBiomes = new List<string>();
        public List<string> discoveredSpecies = new List<string>();
        public List<StringIntEntry> creatures = new List<StringIntEntry>();
        public List<StringIntEntry> items = new List<StringIntEntry>();
        public List<StringIntEntry> tracks = new List<StringIntEntry>();
        public List<string> claimedQuests = new List<string>();
        public List<string> purchasedHints = new List<string>();
        public List<CreatureInstance> pets = new List<CreatureInstance>();
        public List<string> accessories = new List<string>();
        public string timeOfDay = TimeOfDay.Day.ToString();
        public string weather = WeatherType.Sunny.ToString();
        [Range(0f, 1f)] public float worldTime01 = 0.3f;
        public float weatherTimer;
        public List<string> unlockedAchievements = new List<string>();
        public string lastEnergyUtc;
        public string accountCreatedUtc;
        public float starterBoostPlaySeconds;
        public int starterBoostRewardIndex;
        public List<CreatureNestProgress> creatureNests = new List<CreatureNestProgress>();
        public List<string> completedSignatureSets = new List<string>();
        public List<string> activeSignatureBonuses = new List<string>();
        public List<string> foundBiomeEvents = new List<string>();
    }

    public static class SaveSystem
    {
        private const string SaveKey = "Monstrology.Progress.v1";

        public static void Save(GameProgress progress)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        public static GameProgress Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return CreateDefault();
            }

            try
            {
                GameProgress progress = JsonUtility.FromJson<GameProgress>(PlayerPrefs.GetString(SaveKey));
                return progress ?? CreateDefault();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Monstrology save could not be read: " + exception.Message);
                return CreateDefault();
            }
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private static GameProgress CreateDefault()
        {
            GameProgress progress = new GameProgress();
            progress.unlockedBiomes.Add(BiomeType.Forest.ToString());
            return progress;
        }
    }
}
