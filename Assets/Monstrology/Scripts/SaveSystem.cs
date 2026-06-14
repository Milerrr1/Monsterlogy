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
        public int version = 10;
        public int coins = SaveSystem.DefaultCoins;
        public int energy = SaveSystem.DefaultEnergy;
        public string currentBiome = BiomeType.Forest.ToString();
        public int explorationCount;
        public int mutationCount;
        public int totalCreaturesFound;
        public List<string> unlockedBiomes = new List<string>();
        public List<string> discoveredSpecies = new List<string>();
        public List<StringIntEntry> creatures = new List<StringIntEntry>();
        public List<StringIntEntry> lifetimeCreatures = new List<StringIntEntry>();
        public List<StringIntEntry> items = new List<StringIntEntry>();
        public List<StringIntEntry> tracks = new List<StringIntEntry>();
        public List<string> claimedQuests = new List<string>();
        public List<string> purchasedHints = new List<string>();
        public List<StringIntEntry> hintLevels = new List<StringIntEntry>();
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
        public bool introCompleted;
        public string lastDailyRewardUtcDate;
        public int dailyRewardStreak;
        public bool dailyRewardFirstLaunchRegistered;
        public string dailyRewardFirstLaunchUtcDate;
        public int petMigrationVersion;
        public long saveRevision;
        public string updatedAtUtc;
    }

    public static class SaveSystem
    {
        public const int DefaultCoins = 50;
        public const int DefaultEnergy = 75;
        private const string SaveKey = "Monstrology.Progress.v1";
        private const string BackupKey = "Monstrology.Progress.Backup.v1";

        public static bool HasLocalSave
        {
            get { return PlayerPrefs.HasKey(SaveKey); }
        }

        public static void Save(GameProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            progress.saveRevision = Math.Max(0L, progress.saveRevision) + 1L;
            progress.updatedAtUtc = DateTime.UtcNow.ToString("o");
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(progress));
            Flush();
        }

        public static void Flush()
        {
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
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.Save();
        }

        public static string GetStoredJson()
        {
            return PlayerPrefs.GetString(SaveKey, string.Empty);
        }

        public static string GetBackupJson()
        {
            return PlayerPrefs.GetString(BackupKey, string.Empty);
        }

        public static bool TryDeserialize(
            string json,
            out GameProgress progress)
        {
            progress = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                progress = JsonUtility.FromJson<GameProgress>(json);
                return progress != null;
            }
            catch (Exception)
            {
                progress = null;
                return false;
            }
        }

        public static int CompareFreshness(
            GameProgress first,
            GameProgress second)
        {
            if (ReferenceEquals(first, second))
            {
                return 0;
            }

            if (first == null)
            {
                return -1;
            }

            if (second == null)
            {
                return 1;
            }

            int revisionComparison =
                first.saveRevision.CompareTo(second.saveRevision);
            if (revisionComparison != 0)
            {
                return revisionComparison;
            }

            DateTime firstUpdated;
            DateTime secondUpdated;
            bool hasFirstDate =
                DateTime.TryParse(first.updatedAtUtc, out firstUpdated);
            bool hasSecondDate =
                DateTime.TryParse(second.updatedAtUtc, out secondUpdated);
            if (hasFirstDate != hasSecondDate)
            {
                return hasFirstDate ? 1 : -1;
            }

            if (!hasFirstDate)
            {
                return 0;
            }

            return firstUpdated.ToUniversalTime().CompareTo(
                secondUpdated.ToUniversalTime());
        }

        public static bool ImportIfNewer(string remoteJson)
        {
            GameProgress remote;
            if (!TryDeserialize(remoteJson, out remote))
            {
                return false;
            }

            string localJson = GetStoredJson();
            GameProgress local;
            if (TryDeserialize(localJson, out local) &&
                CompareFreshness(remote, local) <= 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(localJson))
            {
                PlayerPrefs.SetString(BackupKey, localJson);
            }

            PlayerPrefs.SetString(SaveKey, remoteJson);
            Flush();
            return true;
        }

        private static GameProgress CreateDefault()
        {
            GameProgress progress = new GameProgress();
            progress.unlockedBiomes.Add(BiomeType.Forest.ToString());
            return progress;
        }
    }
}
