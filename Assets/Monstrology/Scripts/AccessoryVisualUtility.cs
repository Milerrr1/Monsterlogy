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

            Dictionary<AccessorySlot, RectTransform> anchors =
                BuildAnchors(root.transform);
            List<AccessoryData> equipped = inventory.GetEquippedAccessories(pet.uniqueId);
            foreach (AccessoryData accessory in equipped)
            {
                Image visual = UIFactory.Image(
                    accessory.slot + "_" + accessory.id,
                    anchors[accessory.slot],
                    SpriteDatabase.Active.HasAccessoryArtwork(accessory)
                        ? Color.white
                        : PetLocalization.RarityColor(accessory.rarity));
                visual.sprite = SpriteDatabase.Active.GetAccessory(accessory);
                visual.preserveAspect = true;
                visual.raycastTarget = false;
                PlaceVisual(visual.rectTransform, accessory);
            }
        }

        private static Dictionary<AccessorySlot, RectTransform> BuildAnchors(Transform root)
        {
            Dictionary<AccessorySlot, RectTransform> anchors =
                new Dictionary<AccessorySlot, RectTransform>();
            anchors.Add(
                AccessorySlot.Head,
                CreateAnchor(root, AccessorySlot.Head));
            anchors.Add(
                AccessorySlot.Body,
                CreateAnchor(root, AccessorySlot.Body));
            anchors.Add(
                AccessorySlot.Legs,
                CreateAnchor(root, AccessorySlot.Legs));
            return anchors;
        }

        private static RectTransform CreateAnchor(Transform root, AccessorySlot slot)
        {
            string name = CreatureVisualRig.GetAnchorName(slot);
            GameObject anchorObject = UIFactory.Object(name, root);
            RectTransform anchor = anchorObject.GetComponent<RectTransform>();
            Vector3 localPosition = CreatureBaseTemplate.Active.GetAnchorPosition(slot);
            Vector2 normalized = new Vector2(
                localPosition.x + 0.5f,
                localPosition.y + 0.5f);
            UIFactory.SetRect(
                anchor,
                normalized,
                normalized,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            return anchor;
        }

        private static void PlaceVisual(
            RectTransform rect,
            AccessoryData accessory)
        {
            AccessorySlot slot = accessory.slot;
            Vector2 normalizedSize =
                CreatureBaseTemplate.Active.GetAccessorySize(slot);
            RectTransform portrait = rect.parent.parent as RectTransform;
            Vector2 parentSize = portrait != null && portrait.rect.size != Vector2.zero
                ? portrait.rect.size
                : new Vector2(100f, 100f);
            Vector2 size = new Vector2(
                parentSize.x * normalizedSize.x *
                    Mathf.Max(
                        0.01f,
                        Mathf.Abs(accessory.GetUiVisualScale().x)),
                parentSize.y * normalizedSize.y *
                    Mathf.Max(
                        0.01f,
                        Mathf.Abs(accessory.GetUiVisualScale().y)));
            Vector2 visualOffset = accessory.GetUiVisualOffset();
            Vector2 offset = new Vector2(
                parentSize.x * normalizedSize.x * visualOffset.x,
                parentSize.y * normalizedSize.y * visualOffset.y);
            UIFactory.SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                offset,
                size);
        }
    }
}
