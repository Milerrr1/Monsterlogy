using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class SignatureSetSystem : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private AccessoryInventoryManager inventory;

        public event Action<string> FullSetCompleted;
        public event Action SetStateChanged;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            AccessoryInventoryManager inventoryManager)
        {
            Unsubscribe();
            game = gameManager;
            collection = collectionManager;
            inventory = inventoryManager;
            if (inventory != null)
            {
                inventory.InventoryChanged += Recalculate;
            }

            if (collection != null)
            {
                collection.CollectionChanged += Recalculate;
            }

            Recalculate();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public int GetOwnedPieceCount(SignatureSetData set)
        {
            if (set == null || inventory == null || set.accessoryIds == null)
            {
                return 0;
            }

            int count = 0;
            foreach (string accessoryId in set.accessoryIds)
            {
                if (inventory.HasAccessory(accessoryId))
                {
                    count++;
                }
            }

            return count;
        }

        public int GetEquippedPieceCount(CreatureInstance pet, SignatureSetData set)
        {
            if (pet == null || set == null || pet.equippedAccessories == null)
            {
                return 0;
            }

            int count = 0;
            foreach (EquippedAccessory equipped in pet.equippedAccessories)
            {
                if (equipped != null && set.accessoryIds.Contains(equipped.equippedAccessoryId))
                {
                    count++;
                }
            }

            return count;
        }

        public float GetResourceFindBonus(BiomeType biome)
        {
            CreatureInstance favorite = collection != null ? collection.GetFavorite() : null;
            if (favorite == null || game == null)
            {
                return 0f;
            }

            float bonus = 0f;
            foreach (SignatureSetData set in game.Content.signatureSets)
            {
                if (set == null || set.biome != biome)
                {
                    continue;
                }

                int equipped = GetEquippedPieceCount(favorite, set);
                if (equipped >= set.accessoryIds.Count && set.accessoryIds.Count > 0)
                {
                    bonus = Mathf.Max(bonus, set.fullSetResourceBonus);
                    if (set.IsSignatureSpecies(favorite.speciesId))
                    {
                        bonus += 0.05f;
                    }
                }
                else if (equipped >= 2)
                {
                    bonus = Mathf.Max(bonus, set.twoPieceResourceBonus);
                }
            }

            return bonus;
        }

        public float GetRareCreatureBonus(BiomeType biome)
        {
            CreatureInstance favorite = collection != null ? collection.GetFavorite() : null;
            if (favorite == null || game == null)
            {
                return 0f;
            }

            float bonus = 0f;
            foreach (SignatureSetData set in game.Content.signatureSets)
            {
                if (set == null || set.biome != biome ||
                    GetEquippedPieceCount(favorite, set) < set.accessoryIds.Count ||
                    set.accessoryIds.Count == 0)
                {
                    continue;
                }

                bonus = Mathf.Max(bonus, set.fullSetRareCreatureBonus);
                if (set.IsSignatureSpecies(favorite.speciesId))
                {
                    bonus += 0.02f;
                }
            }

            return bonus;
        }

        public bool HasFullSetVisual(CreatureInstance pet)
        {
            if (pet == null || game == null)
            {
                return false;
            }

            foreach (SignatureSetData set in game.Content.signatureSets)
            {
                if (set != null && set.accessoryIds.Count > 0 &&
                    GetEquippedPieceCount(pet, set) >= set.accessoryIds.Count)
                {
                    return true;
                }
            }

            return false;
        }

        private void Recalculate()
        {
            if (game == null)
            {
                return;
            }

            List<string> activeBonuses = new List<string>();
            CreatureInstance favorite = collection != null ? collection.GetFavorite() : null;
            foreach (SignatureSetData set in game.Content.signatureSets)
            {
                if (set == null || set.accessoryIds == null || set.accessoryIds.Count == 0)
                {
                    continue;
                }

                bool complete = GetOwnedPieceCount(set) >= set.accessoryIds.Count;
                if (complete && game.CompleteSignatureSet(set.id) && FullSetCompleted != null)
                {
                    FullSetCompleted(set.id);
                }

                int equipped = GetEquippedPieceCount(favorite, set);
                if (equipped >= 2)
                {
                    activeBonuses.Add(set.id + ":2");
                }

                if (equipped >= set.accessoryIds.Count)
                {
                    activeBonuses.Add(set.id + ":full");
                    if (favorite != null && set.IsSignatureSpecies(favorite.speciesId))
                    {
                        activeBonuses.Add(set.id + ":signature");
                    }
                }
            }

            game.SaveActiveSignatureBonuses(activeBonuses);
            if (SetStateChanged != null)
            {
                SetStateChanged();
            }
        }

        private void Unsubscribe()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Recalculate;
            }

            if (collection != null)
            {
                collection.CollectionChanged -= Recalculate;
            }
        }
    }
}
