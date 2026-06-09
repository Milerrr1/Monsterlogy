using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public static class AccessoryVisualUtility
    {
        public static void BuildUiVisuals(
            Transform portrait,
            CreatureInstance pet,
            AccessoryInventoryManager inventory)
        {
            if (portrait == null || pet == null || inventory == null)
            {
                return;
            }

            GameObject root = UIFactory.Object("EquippedAccessories", portrait);
            UIFactory.Stretch(root.GetComponent<RectTransform>());

            List<AccessoryData> equipped = inventory.GetEquippedAccessories(pet.uniqueId);
            foreach (AccessoryData accessory in equipped)
            {
                Image visual = UIFactory.Image(
                    accessory.slot + "_" + accessory.id,
                    root.transform,
                    accessory.icon != null
                        ? Color.white
                        : PetLocalization.RarityColor(accessory.rarity));
                visual.sprite = accessory.icon != null
                    ? accessory.icon
                    : WorldPlaceholderSprites.Square;
                visual.preserveAspect = true;
                visual.raycastTarget = false;
                PlaceVisual(visual.rectTransform, accessory.slot);
            }
        }

        private static void PlaceVisual(RectTransform rect, AccessorySlot slot)
        {
            switch (slot)
            {
                case AccessorySlot.Head:
                    UIFactory.SetRect(rect,
                        new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f),
                        new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 34f));
                    break;
                case AccessorySlot.Body:
                    UIFactory.SetRect(rect,
                        new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f),
                        new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 48f));
                    break;
                default:
                    UIFactory.SetRect(rect,
                        new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f),
                        new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 28f));
                    break;
            }
        }
    }
}
