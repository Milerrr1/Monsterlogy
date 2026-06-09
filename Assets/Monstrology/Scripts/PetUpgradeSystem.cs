using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public class PetUpgradeSystem : MonoBehaviour
    {
        public const int MaxLevel = 100;

        private GameManager game;
        private CreatureCollectionManager collection;

        public string LastMessage { get; private set; }

        public void Initialize(GameManager gameManager, CreatureCollectionManager collectionManager)
        {
            game = gameManager;
            collection = collectionManager;
        }

        public int GetRequiredResourceCount(CreatureInstance pet)
        {
            return pet == null ? 0 : GetRequiredResourceCount(pet.level);
        }

        public int GetRequiredResourceCount(int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, 1, MaxLevel);
            return level >= MaxLevel ? 0 : 1 + level * (level - 1) / 2;
        }

        public ItemData GetRequiredResource(CreatureInstance pet)
        {
            if (game == null || pet == null)
            {
                return null;
            }

            ItemData exact = game.Content.items.FirstOrDefault(item =>
                item != null && item.kind == ItemKind.UpgradeResource &&
                item.requiredSpeciesId == pet.speciesId);
            if (exact != null)
            {
                return exact;
            }

            return game.Content.items.FirstOrDefault(item =>
                item != null && item.kind == ItemKind.UpgradeResource &&
                item.requiredSpeciesId == pet.evolutionRootSpeciesId);
        }

        public bool CanUpgrade(CreatureInstance pet)
        {
            string reason;
            return CanUpgrade(pet, out reason);
        }

        public bool CanUpgrade(CreatureInstance pet, out string reason)
        {
            if (pet == null)
            {
                reason = "Выберите питомца.";
                return false;
            }

            if (pet.level >= MaxLevel)
            {
                reason = "Достигнут максимальный уровень 100.";
                return false;
            }

            ItemData resource = GetRequiredResource(pet);
            if (resource == null)
            {
                reason = "Прокачка для этого питомца пока недоступна.";
                return false;
            }

            int required = GetRequiredResourceCount(pet);
            int owned = game.GetItemCount(resource.id);
            if (owned < required)
            {
                reason = "Недостаточно ресурса: " + owned + " / " + required + ".";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool UpgradePet(CreatureInstance pet)
        {
            string reason;
            if (!CanUpgrade(pet, out reason))
            {
                LastMessage = reason;
                return false;
            }

            ItemData resource = GetRequiredResource(pet);
            int required = GetRequiredResourceCount(pet);
            if (!game.ConsumeItem(resource.id, required))
            {
                LastMessage = "Не удалось списать ресурс.";
                return false;
            }

            pet.level = Mathf.Min(MaxLevel, pet.level + 1);
            pet.experience = 0;
            collection.SaveNow();
            LastMessage = "Уровень повышен до " + pet.level + ".";
            Debug.Log("Pet upgraded: " + pet.uniqueId + " -> level " + pet.level);
            return true;
        }
    }
}
