using UnityEngine;

namespace Monstrology
{
    public enum ItemKind
    {
        Material,
        MutationCatalyst,
        Egg,
        Hint
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Monstrology/Item")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string itemName;
        [TextArea(2, 4)] public string description;
        public ItemKind kind;
        public Sprite icon;
        public BiomeType preferredBiome;
    }
}
