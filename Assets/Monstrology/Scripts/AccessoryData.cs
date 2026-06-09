using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public enum AccessorySlot
    {
        Head,
        Body,
        Legs
    }

    [CreateAssetMenu(fileName = "Accessory", menuName = "Monstrology/Accessory")]
    public class AccessoryData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public PetRarity rarity;
        public AccessorySlot slot;
        public Sprite icon;
        [Min(0)] public int price;
        [Range(0f, 1f)] public float dropChance = 0.2f;
        public List<string> allowedSpeciesIds = new List<string>();
        public Vector2 visualOffset;
        public Vector2 visualScale = Vector2.one;

        public bool IsAllowedFor(string speciesId)
        {
            return allowedSpeciesIds == null ||
                   allowedSpeciesIds.Count == 0 ||
                   allowedSpeciesIds.Contains(speciesId);
        }
    }
}
