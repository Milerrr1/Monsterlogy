using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class AccessoryInventoryManager : MonoBehaviour
    {
        private readonly List<string> ownedAccessoryIds = new List<string>();
        private GameManager game;
        private CreatureCollectionManager collection;

        public event Action InventoryChanged;

        public void Initialize(GameManager gameManager, CreatureCollectionManager collectionManager)
        {
            if (game != null)
            {
                game.ProgressReset -= ReloadFromProgress;
            }

            game = gameManager;
            collection = collectionManager;
            if (game != null)
            {
                game.ProgressReset += ReloadFromProgress;
            }

            ReloadFromProgress();
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.ProgressReset -= ReloadFromProgress;
            }
        }

        private void ReloadFromProgress()
        {
            ownedAccessoryIds.Clear();

            if (game != null)
            {
                foreach (string accessoryId in game.GetSavedAccessories())
                {
                    if (!string.IsNullOrEmpty(accessoryId) && !ownedAccessoryIds.Contains(accessoryId))
                    {
                        ownedAccessoryIds.Add(accessoryId);
                    }
                }
            }

            RaiseChanged();
        }

        public IReadOnlyList<string> GetOwnedAccessoryIds()
        {
            return ownedAccessoryIds.AsReadOnly();
        }

        public bool AddAccessory(string accessoryId)
        {
            AccessoryData accessory = game != null ? game.GetAccessory(accessoryId) : null;
            if (accessory == null || string.IsNullOrEmpty(accessory.id))
            {
                return false;
            }

            bool isNew = !ownedAccessoryIds.Contains(accessory.id);
            if (isNew)
            {
                ownedAccessoryIds.Add(accessory.id);
                SaveInventory();
            }

            Debug.Log("Accessory found: " + accessory.id);
            return true;
        }

        public bool AddAccessory(AccessoryData accessory)
        {
            return accessory != null && AddAccessory(accessory.id);
        }

        public bool HasAccessory(string accessoryId)
        {
            return !string.IsNullOrEmpty(accessoryId) && ownedAccessoryIds.Contains(accessoryId);
        }

        public bool EquipAccessory(string petId, string accessoryId)
        {
            CreatureInstance pet = collection != null ? collection.GetPetById(petId) : null;
            AccessoryData accessory = game != null ? game.GetAccessory(accessoryId) : null;
            if (pet == null || accessory == null || !HasAccessory(accessoryId) ||
                !accessory.IsAllowedFor(pet.speciesId))
            {
                return false;
            }

            pet.equippedAccessories = pet.equippedAccessories ?? new List<EquippedAccessory>();
            EquippedAccessory equipped = pet.equippedAccessories.Find(entry =>
                entry != null && entry.slot == accessory.slot);
            if (equipped == null)
            {
                equipped = new EquippedAccessory { slot = accessory.slot };
                pet.equippedAccessories.Add(equipped);
            }

            equipped.equippedAccessoryId = accessory.id;
            collection.SaveNow();
            RaiseChanged();
            Debug.Log("Accessory equipped: " + accessory.id + " on " + pet.uniqueId);
            return true;
        }

        public bool UnequipAccessory(string petId, AccessorySlot slot)
        {
            CreatureInstance pet = collection != null ? collection.GetPetById(petId) : null;
            if (pet == null || pet.equippedAccessories == null)
            {
                return false;
            }

            EquippedAccessory equipped = pet.equippedAccessories.Find(entry =>
                entry != null && entry.slot == slot && !string.IsNullOrEmpty(entry.equippedAccessoryId));
            if (equipped == null)
            {
                return false;
            }

            pet.equippedAccessories.Remove(equipped);
            collection.SaveNow();
            RaiseChanged();
            return true;
        }

        public List<AccessoryData> GetEquippedAccessories(string petId)
        {
            List<AccessoryData> result = new List<AccessoryData>();
            CreatureInstance pet = collection != null ? collection.GetPetById(petId) : null;
            if (pet == null || pet.equippedAccessories == null || game == null)
            {
                return result;
            }

            foreach (EquippedAccessory equipped in pet.equippedAccessories)
            {
                AccessoryData accessory = equipped != null
                    ? game.GetAccessory(equipped.equippedAccessoryId)
                    : null;
                if (accessory != null)
                {
                    result.Add(accessory);
                }
            }

            return result;
        }

        private void SaveInventory()
        {
            if (game != null)
            {
                game.SaveAccessories(ownedAccessoryIds);
            }

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (InventoryChanged != null)
            {
                InventoryChanged();
            }
        }
    }
}
