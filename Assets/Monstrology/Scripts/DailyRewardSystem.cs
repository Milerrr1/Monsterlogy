using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Monstrology
{
    public class DailyRewardSystem : MonoBehaviour
    {
        private const string DateFormat = "yyyy-MM-dd";

        private GameManager game;
        private AccessoryInventoryManager inventory;

        public int NextRewardDay
        {
            get
            {
                DateTime lastClaim;
                if (!TryGetLastClaim(out lastClaim) ||
                    lastClaim.Date != DateTime.UtcNow.Date.AddDays(-1))
                {
                    return 1;
                }

                return game.DailyRewardStreak % 7 + 1;
            }
        }

        public bool CanClaimToday
        {
            get
            {
                DateTime lastClaim;
                return !TryGetLastClaim(out lastClaim) ||
                       lastClaim.Date < DateTime.UtcNow.Date;
            }
        }

        public void Initialize(
            GameManager gameManager,
            AccessoryInventoryManager inventoryManager)
        {
            game = gameManager;
            inventory = inventoryManager;
        }

        public string GetRewardDescription(int day)
        {
            switch (Mathf.Clamp(day, 1, 7))
            {
                case 1: return "15 энергии";
                case 2: return "120 монет";
                case 3: return "2 ресурса текущего биома";
                case 4: return "предмет гардероба";
                case 5: return "35 энергии";
                case 6: return "3 редких ресурса";
                default: return "редкая одежда";
            }
        }

        public bool Claim(out string message)
        {
            message = "Ежедневная награда уже получена.";
            if (game == null || !CanClaimToday)
            {
                return false;
            }

            int day = NextRewardDay;
            GrantReward(day, out message);
            game.SaveDailyReward(
                DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture),
                day);
            game.RaiseNotification("Ежедневная награда: " + message + ".");
            return true;
        }

        private void GrantReward(int day, out string message)
        {
            switch (day)
            {
                case 1:
                    game.AddEnergy(15);
                    message = "+15 энергии";
                    return;
                case 2:
                    game.AddCoins(120);
                    message = "+120 монет";
                    return;
                case 3:
                    message = GrantResource(2, false);
                    return;
                case 4:
                    message = GrantAccessory(false);
                    return;
                case 5:
                    game.AddEnergy(35);
                    message = "+35 энергии";
                    return;
                case 6:
                    message = GrantResource(3, true);
                    return;
                default:
                    message = GrantAccessory(true);
                    return;
            }
        }

        private string GrantResource(int amount, bool rare)
        {
            List<ItemData> candidates = game.Content.items.FindAll(item =>
                item != null &&
                item.kind != ItemKind.Egg &&
                (!rare || item.kind == ItemKind.Material) &&
                (game.CurrentBiome == null || item.preferredBiome == game.CurrentBiome.type));
            if (candidates.Count == 0)
            {
                candidates = game.Content.items.FindAll(item =>
                    item != null && item.kind != ItemKind.Egg);
            }

            if (candidates.Count == 0)
            {
                game.AddCoins(rare ? 180 : 80);
                return rare ? "+180 монет" : "+80 монет";
            }

            ItemData selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            game.AddItem(selected.id, amount);
            return "+" + amount + " " + selected.itemName;
        }

        private string GrantAccessory(bool rare)
        {
            List<AccessoryData> candidates = game.Content.accessories.FindAll(accessory =>
                accessory != null &&
                inventory != null &&
                !inventory.HasAccessory(accessory.id) &&
                (!rare || accessory.rarity >= PetRarity.Rare));
            if (candidates.Count == 0 && rare)
            {
                candidates = game.Content.accessories.FindAll(accessory =>
                    accessory != null && inventory != null &&
                    !inventory.HasAccessory(accessory.id));
            }

            if (candidates.Count == 0)
            {
                game.AddCoins(rare ? 250 : 100);
                return rare ? "+250 монет" : "+100 монет";
            }

            AccessoryData selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            inventory.AddAccessory(selected);
            return selected.displayName;
        }

        private bool TryGetLastClaim(out DateTime date)
        {
            date = DateTime.MinValue;
            return game != null && DateTime.TryParseExact(
                game.LastDailyRewardUtcDate,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out date);
        }
    }
}
