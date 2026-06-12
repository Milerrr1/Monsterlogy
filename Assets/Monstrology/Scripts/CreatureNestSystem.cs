using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class CreatureNestProgress
    {
        public string nestId;
        public string speciesId;
        public int level = 1;
        public int claims;
        public string lastClaimUtc;

        public CreatureNestProgress Clone()
        {
            return new CreatureNestProgress
            {
                nestId = nestId,
                speciesId = speciesId,
                level = level,
                claims = claims,
                lastClaimUtc = lastClaimUtc
            };
        }
    }

    [CreateAssetMenu(fileName = "CreatureNest", menuName = "Monstrology/Creature Nest")]
    public class CreatureNestData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public string speciesId;
        public BiomeType biome;
        [Min(0.05f)] public float rewardCooldownHours = 3f;
        [Min(1)] public int baseCopyReward = 1;
        [Min(0)] public int baseResourceReward = 1;
        [Range(0f, 1f)] public float rareAccessoryChance = 0.08f;
        [Range(0f, 1f)] public float specialCreatureChance = 0.12f;
        public string specialCreatureId;
        [Min(1)] public int claimsPerLevel = 3;
        [Min(1)] public int maxLevel = 5;
    }

    public class CreatureNestSystem : MonoBehaviour
    {
        private readonly List<CreatureNestProgress> nests = new List<CreatureNestProgress>();
        private GameManager game;
        private AccessoryInventoryManager inventory;

        public event Action<string> NestDiscovered;
        public event Action<string> NestClaimed;

        public void Initialize(GameManager gameManager, AccessoryInventoryManager inventoryManager)
        {
            game = gameManager;
            inventory = inventoryManager;
            nests.Clear();
            if (game != null)
            {
                nests.AddRange(game.GetSavedCreatureNests());
            }

            Normalize();
        }

        public IReadOnlyList<CreatureNestProgress> GetAllNests()
        {
            return nests.AsReadOnly();
        }

        public CreatureNestProgress GetNest(string nestId)
        {
            return nests.Find(entry => entry != null && entry.nestId == nestId);
        }

        public CreatureNestProgress GetNestForSpecies(string speciesId)
        {
            return nests.Find(entry => entry != null && entry.speciesId == speciesId);
        }

        public CreatureNestData GetNestData(string nestId)
        {
            return game == null
                ? null
                : game.Content.creatureNests.Find(data => data != null && data.id == nestId);
        }

        public int GetDiscoveredCount(BiomeType biome)
        {
            return nests.Count(entry =>
            {
                CreatureNestData data = GetData(entry);
                return data != null && data.biome == biome;
            });
        }

        public int GetTotalNestCount(BiomeType biome)
        {
            return game == null
                ? 0
                : game.Content.creatureNests.Count(data => data != null && data.biome == biome);
        }

        public bool Discover(string speciesId, out CreatureNestProgress progress)
        {
            progress = null;
            if (game == null || string.IsNullOrEmpty(speciesId))
            {
                return false;
            }

            CreatureNestData data = game.Content.creatureNests.Find(entry =>
                entry != null && entry.speciesId == speciesId);
            if (data == null)
            {
                return false;
            }

            progress = GetNest(data.id);
            if (progress != null)
            {
                return false;
            }

            progress = new CreatureNestProgress
            {
                nestId = data.id,
                speciesId = speciesId,
                level = 1,
                claims = 0,
                lastClaimUtc = DateTime.UtcNow
                    .AddHours(-Mathf.Max(0.05f, data.rewardCooldownHours))
                    .ToString("o", CultureInfo.InvariantCulture)
            };
            nests.Add(progress);
            Save();
            if (NestDiscovered != null)
            {
                NestDiscovered(data.id);
            }

            return true;
        }

        public bool IsRewardReady(CreatureNestProgress progress)
        {
            CreatureNestData data = GetData(progress);
            if (data == null)
            {
                return false;
            }

            DateTime lastClaim;
            if (!DateTime.TryParse(
                    progress.lastClaimUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out lastClaim))
            {
                return true;
            }

            return DateTime.UtcNow >= lastClaim.ToUniversalTime()
                .AddHours(Mathf.Max(0.05f, data.rewardCooldownHours));
        }

        public TimeSpan GetTimeUntilReady(CreatureNestProgress progress)
        {
            CreatureNestData data = GetData(progress);
            DateTime lastClaim;
            if (data == null || !DateTime.TryParse(
                    progress.lastClaimUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out lastClaim))
            {
                return TimeSpan.Zero;
            }

            DateTime readyAt = lastClaim.ToUniversalTime()
                .AddHours(Mathf.Max(0.05f, data.rewardCooldownHours));
            return readyAt > DateTime.UtcNow ? readyAt - DateTime.UtcNow : TimeSpan.Zero;
        }

        public bool Claim(string nestId, out string message)
        {
            CreatureNestProgress progress = GetNest(nestId);
            CreatureNestData data = GetData(progress);
            if (progress == null || data == null)
            {
                message = "Логовище не найдено.";
                return false;
            }

            if (!IsRewardReady(progress))
            {
                TimeSpan remaining = GetTimeUntilReady(progress);
                message = "Награда будет готова через " +
                          Mathf.CeilToInt((float)remaining.TotalMinutes) + " мин.";
                return false;
            }

            int copies = Mathf.Max(1, data.baseCopyReward + (progress.level - 1) / 2);
            game.AddCreatureCopies(data.speciesId, copies);

            ItemData resource = game.Content.items.Find(item =>
                item != null && item.kind == ItemKind.UpgradeResource &&
                (item.requiredSpeciesId == data.speciesId ||
                 item.requiredSpeciesId == GetEvolutionRoot(data.speciesId)));
            int resources = resource != null
                ? Mathf.Max(0, data.baseResourceReward + progress.level / 3)
                : 0;
            if (resource != null && resources > 0)
            {
                game.AddItem(resource.id, resources);
            }

            AccessoryData rareAccessory = null;
            if (inventory != null && UnityEngine.Random.value < data.rareAccessoryChance)
            {
                List<AccessoryData> candidates = game.Content.accessories.FindAll(accessory =>
                    accessory != null && !inventory.HasAccessory(accessory.id) &&
                    ((accessory.IsSignature && accessory.IsSignatureForBiome(data.biome)) ||
                     (!accessory.IsSignature && accessory.rarity >= PetRarity.Rare)));
                if (candidates.Count > 0)
                {
                    rareAccessory = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    inventory.AddAccessory(rareAccessory);
                }
            }

            CreatureData specialCreature = null;
            if (UnityEngine.Random.value < data.specialCreatureChance)
            {
                string specialId = string.IsNullOrEmpty(data.specialCreatureId)
                    ? data.speciesId
                    : data.specialCreatureId;
                specialCreature = game.GetCreature(specialId);
                if (specialCreature != null)
                {
                    game.AddCreature(specialCreature);
                }
            }

            progress.claims++;
            progress.level = Mathf.Clamp(
                1 + progress.claims / Mathf.Max(1, data.claimsPerLevel),
                1,
                Mathf.Max(1, data.maxLevel));
            progress.lastClaimUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Save();

            message = data.displayName + ": +" + copies + " коп." +
                      (resource != null ? ", +" + resources + " " + resource.itemName : "") +
                      (rareAccessory != null ? ", " + rareAccessory.displayName : "") +
                      (specialCreature != null
                          ? ", особая встреча: " + specialCreature.creatureName
                          : "");
            if (NestClaimed != null)
            {
                NestClaimed(data.id);
            }

            return true;
        }

        public int ClaimReadyInBiome(BiomeType biome)
        {
            int claimed = 0;
            List<string> ids = nests
                .Where(entry =>
                {
                    CreatureNestData data = GetData(entry);
                    return data != null && data.biome == biome && IsRewardReady(entry);
                })
                .Select(entry => entry.nestId)
                .ToList();
            foreach (string id in ids)
            {
                string message;
                if (Claim(id, out message))
                {
                    claimed++;
                    game.RaiseNotification(message);
                }
            }

            return claimed;
        }

        public int GetReadyCount(BiomeType biome)
        {
            return nests.Count(entry =>
            {
                CreatureNestData data = GetData(entry);
                return data != null && data.biome == biome && IsRewardReady(entry);
            });
        }

        private CreatureNestData GetData(CreatureNestProgress progress)
        {
            return progress == null ? null : GetNestData(progress.nestId);
        }

        private string GetEvolutionRoot(string speciesId)
        {
            CreatureInstance pet = FindObjectOfType<CreatureCollectionManager>()
                ?.GetPetBySpecies(speciesId);
            return pet != null && !string.IsNullOrEmpty(pet.evolutionRootSpeciesId)
                ? pet.evolutionRootSpeciesId
                : speciesId;
        }

        private void Normalize()
        {
            nests.RemoveAll(entry => entry == null || string.IsNullOrEmpty(entry.nestId));
            foreach (CreatureNestProgress entry in nests)
            {
                entry.level = Mathf.Max(1, entry.level);
                entry.claims = Mathf.Max(0, entry.claims);
                if (string.IsNullOrEmpty(entry.lastClaimUtc))
                {
                    entry.lastClaimUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                }
            }
        }

        private void Save()
        {
            if (game != null)
            {
                game.SaveCreatureNests(nests);
            }
        }
    }
}
