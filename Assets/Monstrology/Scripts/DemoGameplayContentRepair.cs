using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public static class DemoGameplayContentRepair
    {
        public static int Repair(GameContent content)
        {
            if (content == null)
            {
                return 0;
            }

            content.creatures =
                content.creatures ?? new List<CreatureData>();
            content.items = content.items ?? new List<ItemData>();
            content.accessories =
                content.accessories ?? new List<AccessoryData>();

            int repairs = 0;
            foreach (ItemData item in content.items)
            {
                if (item == null ||
                    string.IsNullOrEmpty(item.requiredSpeciesId))
                {
                    continue;
                }

                CreatureData linkedCreature = content.creatures.Find(creature =>
                    creature != null &&
                    creature.id == item.requiredSpeciesId);
                if (linkedCreature != null &&
                    item.preferredBiome != linkedCreature.biome)
                {
                    item.preferredBiome = linkedCreature.biome;
                    repairs++;
                }
            }

            AccessoryData forestBoots = content.accessories.Find(accessory =>
                accessory != null && accessory.id == "forest_boots");
            if (forestBoots != null)
            {
                repairs += Set(ref forestBoots.slot, AccessorySlot.Legs);
                repairs += Set(ref forestBoots.setId, "forest_set");
                repairs += Set(
                    ref forestBoots.signatureSetId,
                    "forest_set");
                repairs += Set(
                    ref forestBoots.signatureBiome,
                    BiomeType.Forest);
                repairs += Set(
                    ref forestBoots.signatureBiomeId,
                    BiomeType.Forest.ToString());
                repairs += Set(ref forestBoots.isSignature, true);
                repairs += Set(ref forestBoots.overrideUiVisual, true);
                repairs += Set(
                    ref forestBoots.uiVisualOffset,
                    new Vector2(0f, 0.06f));
                repairs += Set(
                    ref forestBoots.uiVisualScale,
                    new Vector2(6f, 6f));
                repairs += Set(ref forestBoots.overrideWorldVisual, true);
                repairs += Set(
                    ref forestBoots.worldVisualOffset,
                    new Vector2(0f, 0.06f));
                repairs += Set(
                    ref forestBoots.worldVisualScale,
                    new Vector2(6f, 3f));
            }

            return repairs;
        }

        private static int Set<T>(ref T target, T value)
        {
            if (EqualityComparer<T>.Default.Equals(target, value))
            {
                return 0;
            }

            target = value;
            return 1;
        }
    }
}
