using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [CreateAssetMenu(fileName = "SignatureSet", menuName = "Monstrology/Signature Set")]
    public class SignatureSetData : ScriptableObject
    {
        public string id;
        public string displayName;
        public BiomeType biome;
        public List<string> accessoryIds = new List<string>();
        public List<string> signatureSpeciesIds = new List<string>();
        [Range(0f, 1f)] public float twoPieceResourceBonus = 0.1f;
        [Range(0f, 1f)] public float fullSetResourceBonus = 0.2f;
        [Range(0f, 1f)] public float fullSetRareCreatureBonus = 0.05f;

        public bool IsSignatureSpecies(string speciesId)
        {
            return !string.IsNullOrEmpty(speciesId) &&
                   signatureSpeciesIds != null &&
                   signatureSpeciesIds.Contains(speciesId);
        }
    }
}
