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
        [Header("Signature clothing")]
        public bool isSignature;
        public string signatureBiomeId;
        public string signatureSpeciesId;
        public string setId;

        [Header("Legacy signature fields")]
        public string signatureSetId;
        public BiomeType signatureBiome;
        [Min(0)] public int price;
        [Range(0f, 1f)] public float dropChance = 0.2f;
        public List<string> allowedSpeciesIds = new List<string>();
        [Header("Legacy visual fields")]
        [Tooltip("Kept for old assets. Standard wardrobe rendering uses Head/Body/Leg anchors.")]
        public Vector2 visualOffset;
        [Tooltip("Kept for old assets. Standard wardrobe rendering uses template slot sizes.")]
        public Vector2 visualScale = Vector2.one;

        public bool IsSignature
        {
            get
            {
                return isSignature ||
                       !string.IsNullOrEmpty(setId) ||
                       !string.IsNullOrEmpty(signatureSetId);
            }
        }

        public string EffectiveSetId
        {
            get { return !string.IsNullOrEmpty(setId) ? setId : signatureSetId; }
        }

        public string EffectiveSignatureSpeciesId
        {
            get { return signatureSpeciesId ?? string.Empty; }
        }

        public bool IsAllowedFor(string speciesId)
        {
            return allowedSpeciesIds == null ||
                   allowedSpeciesIds.Count == 0 ||
                   allowedSpeciesIds.Contains(speciesId);
        }

        public bool IsSignatureForBiome(BiomeType biome)
        {
            if (!IsSignature)
            {
                return false;
            }

            BiomeType parsed;
            if (!string.IsNullOrEmpty(signatureBiomeId) &&
                System.Enum.TryParse(signatureBiomeId, true, out parsed))
            {
                return parsed == biome;
            }

            return signatureBiome == biome;
        }
    }
}
