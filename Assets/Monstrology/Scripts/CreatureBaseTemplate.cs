using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [CreateAssetMenu(
        fileName = "CreatureBaseTemplate",
        menuName = "Monstrology/Creature Base Template")]
    public class CreatureBaseTemplate : ScriptableObject
    {
        public const int TargetTextureSize = 2048;
        public const float SafeMargin = 0.1f;
        public const float HeadHeightRatio = 0.4f;
        public const float BodyHeightRatio = 0.4f;
        public const float LegsHeightRatio = 0.2f;

        private static CreatureBaseTemplate active;

        [Header("Canvas standard")]
        [SerializeField] private Vector2Int canvasSize =
            new Vector2Int(TargetTextureSize, TargetTextureSize);
        [SerializeField, Range(0f, 0.25f)] private float safeMargin = SafeMargin;

        [Header("Front-facing proportions")]
        [SerializeField, Range(0f, 1f)] private float headHeight = HeadHeightRatio;
        [SerializeField, Range(0f, 1f)] private float bodyHeight = BodyHeightRatio;
        [SerializeField, Range(0f, 1f)] private float legsHeight = LegsHeightRatio;

        [Header("Normalized anchors")]
        [SerializeField] private Vector2 headAnchor = new Vector2(0.5f, 0.76f);
        [SerializeField] private Vector2 bodyAnchor = new Vector2(0.5f, 0.43f);
        [SerializeField] private Vector2 legAnchor = new Vector2(0.5f, 0.16f);

        [Header("Standard accessory bounds")]
        [SerializeField] private Vector2 headAccessorySize = new Vector2(0.72f, 0.32f);
        [SerializeField] private Vector2 bodyAccessorySize = new Vector2(0.76f, 0.46f);
        [SerializeField] private Vector2 legAccessorySize = new Vector2(0.68f, 0.24f);

        public static CreatureBaseTemplate Active
        {
            get
            {
                if (active == null)
                {
                    active = Resources.Load<CreatureBaseTemplate>("CreatureBaseTemplate");
                    if (active == null)
                    {
                        active = CreateInstance<CreatureBaseTemplate>();
                        active.name = "Runtime Creature Base Template";
                        active.hideFlags = HideFlags.DontSave;
                    }
                }

                return active;
            }
        }

        public Vector2Int CanvasSize { get { return canvasSize; } }
        public float SafeZoneMargin { get { return safeMargin; } }
        public float HeadRatio { get { return headHeight; } }
        public float BodyRatio { get { return bodyHeight; } }
        public float LegsRatio { get { return legsHeight; } }

        public static void Install(CreatureBaseTemplate template)
        {
            active = template != null
                ? template
                : Resources.Load<CreatureBaseTemplate>("CreatureBaseTemplate");
            if (active == null)
            {
                active = CreateInstance<CreatureBaseTemplate>();
                active.name = "Runtime Creature Base Template";
                active.hideFlags = HideFlags.DontSave;
            }
        }

        public Vector3 GetAnchorPosition(AccessorySlot slot)
        {
            Vector2 normalized;
            switch (slot)
            {
                case AccessorySlot.Head:
                    normalized = headAnchor;
                    break;
                case AccessorySlot.Body:
                    normalized = bodyAnchor;
                    break;
                default:
                    normalized = legAnchor;
                    break;
            }

            return new Vector3(normalized.x - 0.5f, normalized.y - 0.5f, 0f);
        }

        public Vector2 GetAccessorySize(AccessorySlot slot)
        {
            switch (slot)
            {
                case AccessorySlot.Head:
                    return headAccessorySize;
                case AccessorySlot.Body:
                    return bodyAccessorySize;
                default:
                    return legAccessorySize;
            }
        }

        public bool HasValidStandard()
        {
            return canvasSize.x == TargetTextureSize &&
                   canvasSize.y == TargetTextureSize &&
                   Mathf.Abs(safeMargin - SafeMargin) < 0.001f &&
                   Mathf.Abs(headHeight - HeadHeightRatio) < 0.001f &&
                   Mathf.Abs(bodyHeight - BodyHeightRatio) < 0.001f &&
                   Mathf.Abs(legsHeight - LegsHeightRatio) < 0.001f &&
                   Mathf.Abs(headHeight + bodyHeight + legsHeight - 1f) < 0.001f;
        }
    }

    [DisallowMultipleComponent]
    public class CreatureVisualRig : MonoBehaviour
    {
        public const string VisualName = "Visual";
        public const string HeadAnchorName = "HeadAnchor";
        public const string BodyAnchorName = "BodyAnchor";
        public const string LegAnchorName = "LegAnchor";

        [SerializeField] private SpriteRenderer creatureRenderer;
        [SerializeField] private Transform headAnchor;
        [SerializeField] private Transform bodyAnchor;
        [SerializeField] private Transform legAnchor;

        public SpriteRenderer CreatureRenderer { get { return creatureRenderer; } }
        public Transform HeadAnchor { get { return headAnchor; } }
        public Transform BodyAnchor { get { return bodyAnchor; } }
        public Transform LegAnchor { get { return legAnchor; } }

        private void Awake()
        {
            EnsureStructure();
        }

        private void Reset()
        {
            EnsureStructure();
        }

        public void EnsureStructure()
        {
            CreatureBaseTemplate template = CreatureBaseTemplate.Active;
            Transform visual = transform.Find(VisualName);
            if (visual == null)
            {
                GameObject visualObject = new GameObject(VisualName);
                visualObject.transform.SetParent(transform, false);
                visual = visualObject.transform;
            }

            creatureRenderer = visual.GetComponent<SpriteRenderer>();
            if (creatureRenderer == null)
            {
                creatureRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }

            headAnchor = EnsureAnchor(
                HeadAnchorName,
                template.GetAnchorPosition(AccessorySlot.Head));
            bodyAnchor = EnsureAnchor(
                BodyAnchorName,
                template.GetAnchorPosition(AccessorySlot.Body));
            legAnchor = EnsureAnchor(
                LegAnchorName,
                template.GetAnchorPosition(AccessorySlot.Legs));
        }

        public void ApplyCreature(
            CreatureData creature,
            Color fallbackColor,
            int sortingOrder = 19)
        {
            EnsureStructure();
            creatureRenderer.sprite = SpriteDatabase.Active.GetCreatureWorld(creature);
            creatureRenderer.color = SpriteDatabase.Active.HasCreatureArtwork(creature)
                ? Color.white
                : fallbackColor;
            creatureRenderer.sortingOrder = sortingOrder;
            FitRenderer(creatureRenderer, Vector2.one);
        }

        public void ApplyAccessories(
            IEnumerable<AccessoryData> accessories,
            int sortingOrder = 20)
        {
            EnsureStructure();
            ClearAnchor(headAnchor);
            ClearAnchor(bodyAnchor);
            ClearAnchor(legAnchor);
            if (accessories == null)
            {
                return;
            }

            int index = 0;
            foreach (AccessoryData accessory in accessories)
            {
                if (accessory == null)
                {
                    continue;
                }

                Transform anchor = GetAnchor(accessory.slot);
                GameObject visual = new GameObject(accessory.slot + "_" + accessory.id);
                visual.transform.SetParent(anchor, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = SpriteDatabase.Active.GetAccessory(accessory);
                renderer.color = SpriteDatabase.Active.HasAccessoryArtwork(accessory)
                    ? Color.white
                    : PetLocalization.RarityColor(accessory.rarity);
                renderer.sortingOrder = sortingOrder + index++;
                FitRenderer(
                    renderer,
                    CreatureBaseTemplate.Active.GetAccessorySize(accessory.slot));
            }
        }

        public static string GetAnchorName(AccessorySlot slot)
        {
            switch (slot)
            {
                case AccessorySlot.Head:
                    return HeadAnchorName;
                case AccessorySlot.Body:
                    return BodyAnchorName;
                default:
                    return LegAnchorName;
            }
        }

        public Transform GetAnchor(AccessorySlot slot)
        {
            EnsureStructure();
            switch (slot)
            {
                case AccessorySlot.Head:
                    return headAnchor;
                case AccessorySlot.Body:
                    return bodyAnchor;
                default:
                    return legAnchor;
            }
        }

        public bool HasValidStructure()
        {
            return creatureRenderer != null &&
                   creatureRenderer.transform != transform &&
                   headAnchor != null && headAnchor.name == HeadAnchorName &&
                   bodyAnchor != null && bodyAnchor.name == BodyAnchorName &&
                   legAnchor != null && legAnchor.name == LegAnchorName;
        }

        private Transform EnsureAnchor(string anchorName, Vector3 position)
        {
            Transform anchor = transform.Find(anchorName);
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject(anchorName);
                anchorObject.transform.SetParent(transform, false);
                anchor = anchorObject.transform;
            }

            anchor.localPosition = position;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static void ClearAnchor(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            for (int index = anchor.childCount - 1; index >= 0; index--)
            {
                Destroy(anchor.GetChild(index).gameObject);
            }
        }

        private static void FitRenderer(SpriteRenderer renderer, Vector2 targetSize)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            Vector2 size = renderer.sprite.bounds.size;
            float scale = Mathf.Min(
                targetSize.x / Mathf.Max(0.001f, size.x),
                targetSize.y / Mathf.Max(0.001f, size.y));
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.one * scale;
        }
    }
}
