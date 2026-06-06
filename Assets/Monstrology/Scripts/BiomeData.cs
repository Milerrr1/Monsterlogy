using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public enum BiomeType
    {
        Forest,
        Desert,
        Tundra,
        Volcano,
        Ocean,
        Space
    }

    [CreateAssetMenu(fileName = "Biome", menuName = "Monstrology/Biome")]
    public class BiomeData : ScriptableObject
    {
        public BiomeType type;
        public string biomeName;
        [Min(0)] public int unlockPrice;
        [TextArea(2, 5)] public string description;
        public Sprite background;
        public Color fallbackColor = Color.white;
        public List<CreatureData> availableCreatures = new List<CreatureData>();
        [Tooltip("Optional 2D world map settings. If empty, WorldExplorationManager builds a lightweight placeholder map.")]
        public BiomeMapData mapData;
    }
}
