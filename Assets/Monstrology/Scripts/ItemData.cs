using UnityEngine;

namespace Monstrology
{
    public enum ItemKind
    {
        Material,
        MutationCatalyst,
        Egg,
        Hint,
        UpgradeResource
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Monstrology/Item")]
    public class ItemData : ScriptableObject
    {
        public const string HiddenName = "Неизвестный материал";
        public const string HiddenDescription =
            "Странный материал неизвестного происхождения. " +
            "Возможно, его назначение станет понятно позже.";

        public string id;
        public string itemName;
        [TextArea(2, 4)] public string description;
        public ItemKind kind;
        public Sprite icon;
        public BiomeType preferredBiome;
        [Tooltip("Species upgraded by this resource. Empty for non-upgrade items.")]
        public string requiredSpeciesId;

        public bool IsIdentityRevealed(GameManager game)
        {
            return string.IsNullOrEmpty(requiredSpeciesId) ||
                   game != null && game.IsCreatureFound(requiredSpeciesId);
        }

        public string GetVisibleName(GameManager game)
        {
            return GetVisibleName(IsIdentityRevealed(game));
        }

        public string GetVisibleName(bool identityRevealed)
        {
            return identityRevealed || string.IsNullOrEmpty(requiredSpeciesId)
                ? itemName
                : HiddenName;
        }

        public string GetVisibleDescription(GameManager game)
        {
            return GetVisibleDescription(IsIdentityRevealed(game));
        }

        public string GetVisibleDescription(bool identityRevealed)
        {
            return identityRevealed || string.IsNullOrEmpty(requiredSpeciesId)
                ? description
                : HiddenDescription;
        }
    }
}
