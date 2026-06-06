using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [CreateAssetMenu(fileName = "BiomeMap", menuName = "Monstrology/Biome Map")]
    public class BiomeMapData : ScriptableObject
    {
        [Header("Biome")]
        public string biomeId;
        public BiomeType biomeType;

        [Header("Map")]
        public GameObject mapPrefab;
        public Sprite background;
        public Color backgroundColor = new Color(0.18f, 0.38f, 0.28f);
        public Color lightingColor = Color.white;
        public Vector2 worldSize = new Vector2(30f, 18f);
        public Vector2 playerStart = Vector2.zero;

        [Header("Spawning")]
        [Tooltip("Optional prefab SpawnPoint references. Points inside the instantiated map prefab are discovered automatically.")]
        public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        [Min(0)] public int activeFindings = 7;
        [Min(0.25f)] public float respawnInterval = 7f;
        public List<CreatureData> possibleCreatures = new List<CreatureData>();

        public bool Matches(BiomeData biome)
        {
            if (biome == null)
            {
                return false;
            }

            return biome.type == biomeType ||
                   (!string.IsNullOrEmpty(biomeId) &&
                    string.Equals(biomeId, biome.type.ToString(), System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
