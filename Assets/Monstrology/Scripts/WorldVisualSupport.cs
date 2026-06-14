using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [DisallowMultipleComponent]
    public class WorldYSorter : MonoBehaviour
    {
        [SerializeField] private bool updateWhileMoving;
        [SerializeField] private int baseOrder = 1000;
        [SerializeField, Min(1f)] private float ordersPerWorldUnit = 10f;
        [SerializeField, Min(0.001f)] private float movementThreshold = 0.025f;

        private readonly List<RendererEntry> renderers =
            new List<RendererEntry>();
        private float lastY = float.PositiveInfinity;

        public void Configure(bool dynamicObject, int configuredBaseOrder = 1000)
        {
            updateWhileMoving = dynamicObject;
            baseOrder = configuredBaseOrder;
            RefreshRenderers();
            ApplyNow();
        }

        public void RefreshRenderers()
        {
            renderers.Clear();
            Renderer[] found = GetComponentsInChildren<Renderer>(true);
            if (found.Length == 0)
            {
                return;
            }

            int minimumOrder = int.MaxValue;
            foreach (Renderer renderer in found)
            {
                if (renderer != null)
                {
                    minimumOrder = Mathf.Min(minimumOrder, renderer.sortingOrder);
                }
            }

            if (minimumOrder == int.MaxValue)
            {
                minimumOrder = 0;
            }

            foreach (Renderer renderer in found)
            {
                if (renderer != null)
                {
                    renderers.Add(new RendererEntry(
                        renderer,
                        renderer.sortingOrder - minimumOrder));
                }
            }
        }

        public void ApplyNow()
        {
            lastY = transform.position.y;
            int anchorOrder = Mathf.Clamp(
                baseOrder -
                Mathf.RoundToInt(lastY * Mathf.Max(1f, ordersPerWorldUnit)),
                -30000,
                30000);
            foreach (RendererEntry entry in renderers)
            {
                if (entry.renderer != null)
                {
                    entry.renderer.sortingOrder =
                        Mathf.Clamp(anchorOrder + entry.offset, -30000, 30000);
                }
            }
        }

        private void LateUpdate()
        {
            if (updateWhileMoving &&
                Mathf.Abs(transform.position.y - lastY) >= movementThreshold)
            {
                ApplyNow();
            }
        }

        private readonly struct RendererEntry
        {
            public readonly Renderer renderer;
            public readonly int offset;

            public RendererEntry(Renderer renderer, int offset)
            {
                this.renderer = renderer;
                this.offset = offset;
            }
        }
    }

    public static class WorldShadowUtility
    {
        private static Sprite shadowSprite;

        public static SpriteRenderer EnsureShadow(
            Transform root,
            Vector2 size,
            float opacity = 0.2f,
            float verticalOffset = 0.05f)
        {
            if (root == null)
            {
                return null;
            }

            Transform shadow = root.Find("WorldShadow");
            if (shadow == null)
            {
                GameObject shadowObject = new GameObject("WorldShadow");
                shadowObject.transform.SetParent(root, false);
                shadow = shadowObject.transform;
            }

            SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = shadow.gameObject.AddComponent<SpriteRenderer>();
            }

            if (shadowSprite == null)
            {
                shadowSprite = RuntimeSpriteFactory.Create(
                    RuntimeSpriteShape.Circle,
                    "World Shadow");
            }

            renderer.sprite = shadowSprite;
            renderer.color = new Color(0f, 0f, 0f, Mathf.Clamp01(opacity));
            renderer.sortingOrder = -1;
            shadow.localPosition = new Vector3(0f, verticalOffset, 0f);
            Vector2 spriteSize = shadowSprite.bounds.size;
            shadow.localScale = new Vector3(
                Mathf.Max(0.05f, size.x) / Mathf.Max(0.001f, spriteSize.x),
                Mathf.Max(0.025f, size.y) / Mathf.Max(0.001f, spriteSize.y),
                1f);
            return renderer;
        }
    }

    [DisallowMultipleComponent]
    public class WorldDecoration : MonoBehaviour
    {
        private static readonly HashSet<WorldDecoration> Active =
            new HashSet<WorldDecoration>();

        private SpriteRenderer visual;
        private BoxCollider2D footprint;
        private WorldYSorter sorter;

        public string DecorationId { get; private set; }
        public Vector2 WorldSize { get; private set; }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        public void Configure(
            BiomeDecorationEntry entry,
            float scaleVariation,
            bool flipX)
        {
            if (entry == null || entry.sprite == null)
            {
                gameObject.SetActive(false);
                return;
            }

            EnsureComponents();
            DecorationId = entry.id ?? string.Empty;
            float variation = Mathf.Clamp(scaleVariation, 0.5f, 1.5f);
            WorldSize = new Vector2(
                Mathf.Max(0.05f, entry.targetWorldSize.x * variation),
                Mathf.Max(0.05f, entry.targetWorldSize.y * variation));

            visual.sprite = entry.sprite;
            visual.color = Color.white;
            visual.sortingOrder = 0;
            FitSprite(visual, WorldSize, flipX);

            footprint.size = new Vector2(
                Mathf.Max(0.05f, entry.colliderSize.x * variation),
                Mathf.Max(0.05f, entry.colliderSize.y * variation));
            footprint.offset = entry.colliderOffset * variation;
            footprint.isTrigger = !entry.blocksMovement;
            footprint.enabled = true;

            WorldShadowUtility.EnsureShadow(
                transform,
                new Vector2(WorldSize.x * 0.72f, WorldSize.y * 0.12f),
                0.2f,
                Mathf.Max(0.02f, footprint.offset.y * 0.35f));
            sorter.RefreshRenderers();
            sorter.ApplyNow();
            gameObject.name = "Decoration_" + DecorationId;
        }

        public static bool IsAreaOccupied(Vector2 center, Vector2 size)
        {
            Bounds candidate = new Bounds(center, new Vector3(
                Mathf.Max(0.05f, size.x),
                Mathf.Max(0.05f, size.y),
                0.1f));
            foreach (WorldDecoration decoration in Active)
            {
                if (decoration == null ||
                    !decoration.isActiveAndEnabled ||
                    decoration.footprint == null ||
                    !decoration.footprint.enabled)
                {
                    continue;
                }

                if (candidate.Intersects(decoration.footprint.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool OverlapsVisualBounds(Bounds candidate)
        {
            foreach (WorldDecoration decoration in Active)
            {
                if (decoration == null ||
                    !decoration.isActiveAndEnabled ||
                    decoration.visual == null ||
                    !decoration.visual.enabled)
                {
                    continue;
                }

                if (candidate.Intersects(decoration.visual.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        public static float CalculateUniformScale(
            Sprite sprite,
            Vector2 targetWorldSize)
        {
            if (sprite == null)
            {
                return 1f;
            }

            Vector2 source = sprite.bounds.size;
            if (source.x <= 0.0001f || source.y <= 0.0001f)
            {
                return 1f;
            }

            return Mathf.Clamp(
                Mathf.Min(
                    Mathf.Max(0.05f, targetWorldSize.x) / source.x,
                    Mathf.Max(0.05f, targetWorldSize.y) / source.y),
                0.02f,
                20f);
        }

        private void EnsureComponents()
        {
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform == null)
            {
                GameObject visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(transform, false);
                visualTransform = visualObject.transform;
            }

            visual = visualTransform.GetComponent<SpriteRenderer>();
            if (visual == null)
            {
                visual = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            footprint = GetComponent<BoxCollider2D>();
            if (footprint == null)
            {
                footprint = gameObject.AddComponent<BoxCollider2D>();
            }

            sorter = GetComponent<WorldYSorter>();
            if (sorter == null)
            {
                sorter = gameObject.AddComponent<WorldYSorter>();
            }

            sorter.Configure(false);
        }

        private static void FitSprite(
            SpriteRenderer renderer,
            Vector2 targetWorldSize,
            bool flipX)
        {
            float scale = CalculateUniformScale(renderer.sprite, targetWorldSize);
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = new Vector3(
                flipX ? -scale : scale,
                scale,
                1f);
        }
    }

    [DisallowMultipleComponent]
    public class BiomeDecorationRepeater : MonoBehaviour
    {
        private const int GridRadius = 1;
        private const int PlacementAttempts = 24;

        private readonly List<DecorationSegment> segments =
            new List<DecorationSegment>();
        private readonly List<Bounds> occupiedBounds = new List<Bounds>();

        private BiomeType biome;
        private BiomeVisualConfig config;
        private Vector2 segmentSize;
        private Vector2 origin;
        private Vector2 protectedStart;
        private Transform target;
        private Vector2Int currentCell =
            new Vector2Int(int.MinValue, int.MinValue);
        private int maximumDecorationsPerSegment;

        public int SegmentCount { get { return segments.Count; } }

        public int ActiveDecorationCount
        {
            get
            {
                int count = 0;
                foreach (DecorationSegment segment in segments)
                {
                    count += segment.ActiveCount;
                }

                return count;
            }
        }

        public void Initialize(
            BiomeType biomeType,
            BiomeVisualConfig visualConfig,
            Vector2 configuredSegmentSize,
            Transform followTarget,
            Vector2 worldOrigin,
            Vector2 playerStart)
        {
            biome = biomeType;
            config = visualConfig;
            segmentSize = new Vector2(
                Mathf.Max(4f, configuredSegmentSize.x),
                Mathf.Max(4f, configuredSegmentSize.y));
            target = followTarget;
            origin = worldOrigin;
            protectedStart = worldOrigin + playerStart;
            maximumDecorationsPerSegment = GetMaximumDecorationCount(config);
            EnsurePool();
            currentCell = new Vector2Int(int.MinValue, int.MinValue);
            RefreshSegments(true);
        }

        private void LateUpdate()
        {
            RefreshSegments(false);
        }

        private void EnsurePool()
        {
            int required = (GridRadius * 2 + 1) * (GridRadius * 2 + 1);
            while (segments.Count < required)
            {
                GameObject segmentObject =
                    new GameObject("DecorationSegment_" + segments.Count);
                segmentObject.transform.SetParent(transform, false);
                segments.Add(new DecorationSegment(
                    segmentObject.transform,
                    maximumDecorationsPerSegment));
            }

            foreach (DecorationSegment segment in segments)
            {
                segment.EnsureCapacity(maximumDecorationsPerSegment);
            }
        }

        private void RefreshSegments(bool force)
        {
            if (target == null ||
                config == null ||
                config.decorations == null ||
                config.decorations.Count == 0)
            {
                DeactivateAll();
                return;
            }

            Vector2 relative = (Vector2)target.position - origin;
            Vector2Int cell = new Vector2Int(
                Mathf.FloorToInt(
                    (relative.x + segmentSize.x * 0.5f) / segmentSize.x),
                Mathf.FloorToInt(
                    (relative.y + segmentSize.y * 0.5f) / segmentSize.y));
            if (!force && cell == currentCell)
            {
                return;
            }

            currentCell = cell;
            DeactivateAll();
            int index = 0;
            for (int y = -GridRadius; y <= GridRadius; y++)
            {
                for (int x = -GridRadius; x <= GridRadius; x++)
                {
                    Vector2Int segmentCell =
                        new Vector2Int(cell.x + x, cell.y + y);
                    ConfigureSegment(segments[index++], segmentCell);
                }
            }
        }

        private void ConfigureSegment(
            DecorationSegment segment,
            Vector2Int cell)
        {
            segment.root.position = new Vector3(
                origin.x + cell.x * segmentSize.x,
                origin.y + cell.y * segmentSize.y,
                0f);
            segment.root.gameObject.SetActive(true);
            occupiedBounds.Clear();
            System.Random random = new System.Random(StableSeed(cell));
            int poolIndex = 0;

            foreach (BiomeDecorationEntry entry in config.decorations)
            {
                if (entry == null || entry.sprite == null)
                {
                    continue;
                }

                int count = Mathf.Clamp(entry.minCount, 0, entry.maxCount);
                for (int extra = count; extra < entry.maxCount; extra++)
                {
                    if (random.NextDouble() <
                        Mathf.Clamp01(entry.spawnWeight))
                    {
                        count++;
                    }
                }

                for (int decorationIndex = 0;
                     decorationIndex < count &&
                     poolIndex < segment.decorations.Count;
                     decorationIndex++)
                {
                    float variation = Mathf.Lerp(
                        Mathf.Min(
                            entry.scaleVariation.x,
                            entry.scaleVariation.y),
                        Mathf.Max(
                            entry.scaleVariation.x,
                            entry.scaleVariation.y),
                        (float)random.NextDouble());
                    Vector2 worldSize =
                        entry.targetWorldSize * Mathf.Clamp(variation, 0.5f, 1.5f);
                    Vector2 localPosition;
                    if (!TryFindPosition(
                            segment,
                            entry,
                            worldSize,
                            random,
                            out localPosition))
                    {
                        continue;
                    }

                    WorldDecoration decoration =
                        segment.decorations[poolIndex++];
                    decoration.transform.localPosition =
                        new Vector3(localPosition.x, localPosition.y, 0f);
                    decoration.gameObject.SetActive(true);
                    decoration.Configure(
                        entry,
                        variation,
                        entry.allowFlipX && random.NextDouble() > 0.5);
                    occupiedBounds.Add(new Bounds(
                        decoration.transform.position +
                        Vector3.up * worldSize.y * 0.5f,
                        new Vector3(
                            worldSize.x + entry.minimumDistance,
                            worldSize.y + entry.minimumDistance,
                            0.1f)));
                }
            }

            segment.SetActiveCount(poolIndex);
        }

        private bool TryFindPosition(
            DecorationSegment segment,
            BiomeDecorationEntry entry,
            Vector2 worldSize,
            System.Random random,
            out Vector2 localPosition)
        {
            float halfWidth = Mathf.Max(0.25f, worldSize.x * 0.5f);
            float xLimit = Mathf.Max(0.5f, segmentSize.x * 0.5f - halfWidth);
            float yMinimum = -segmentSize.y * 0.5f + 0.15f;
            float yMaximum = segmentSize.y * 0.5f -
                             Mathf.Max(0.25f, worldSize.y) -
                             0.15f;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                localPosition = new Vector2(
                    Mathf.Lerp(-xLimit, xLimit, (float)random.NextDouble()),
                    Mathf.Lerp(
                        yMinimum,
                        Mathf.Max(yMinimum, yMaximum),
                        (float)random.NextDouble()));
                Vector2 worldPosition =
                    (Vector2)segment.root.position + localPosition;
                float startDistance = Mathf.Max(2.5f, entry.minimumDistance);
                if ((worldPosition - protectedStart).sqrMagnitude <
                    startDistance * startDistance)
                {
                    continue;
                }

                Bounds candidate = new Bounds(
                    worldPosition + Vector2.up * worldSize.y * 0.5f,
                    new Vector3(
                        worldSize.x + entry.minimumDistance,
                        worldSize.y + entry.minimumDistance,
                        0.1f));
                bool overlap = false;
                foreach (Bounds occupied in occupiedBounds)
                {
                    if (candidate.Intersects(occupied))
                    {
                        overlap = true;
                        break;
                    }
                }

                if (overlap ||
                    WorldDecoration.OverlapsVisualBounds(candidate) ||
                    IsOccupiedByGameplay(worldPosition, worldSize))
                {
                    continue;
                }

                return true;
            }

            localPosition = Vector2.zero;
            return false;
        }

        private static bool IsOccupiedByGameplay(
            Vector2 position,
            Vector2 size)
        {
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                position,
                new Vector2(
                    Mathf.Max(0.25f, size.x * 0.85f),
                    Mathf.Max(0.25f, size.y * 0.45f)),
                0f);
            foreach (Collider2D overlap in overlaps)
            {
                if (overlap == null)
                {
                    continue;
                }

                if (overlap.GetComponentInParent<WorldPickup>() != null ||
                    overlap.GetComponentInParent<PlayerController2D>() != null ||
                    overlap.GetComponentInParent<WorldDecoration>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void DeactivateAll()
        {
            foreach (DecorationSegment segment in segments)
            {
                segment.Deactivate();
            }
        }

        private int StableSeed(Vector2Int cell)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)biome;
                hash = hash * 31 + cell.x;
                hash = hash * 31 + cell.y;
                return hash;
            }
        }

        private static int GetMaximumDecorationCount(
            BiomeVisualConfig visualConfig)
        {
            if (visualConfig == null || visualConfig.decorations == null)
            {
                return 0;
            }

            int total = 0;
            foreach (BiomeDecorationEntry entry in visualConfig.decorations)
            {
                if (entry != null && entry.sprite != null)
                {
                    total += Mathf.Max(0, entry.maxCount);
                }
            }

            return total;
        }

        private sealed class DecorationSegment
        {
            public readonly Transform root;
            public readonly List<WorldDecoration> decorations =
                new List<WorldDecoration>();

            public int ActiveCount { get; private set; }

            public DecorationSegment(Transform root, int capacity)
            {
                this.root = root;
                EnsureCapacity(capacity);
            }

            public void EnsureCapacity(int capacity)
            {
                while (decorations.Count < capacity)
                {
                    GameObject decorationObject = new GameObject(
                        "DecorationPool_" + decorations.Count);
                    decorationObject.transform.SetParent(root, false);
                    WorldDecoration decoration =
                        decorationObject.AddComponent<WorldDecoration>();
                    decorationObject.SetActive(false);
                    decorations.Add(decoration);
                }
            }

            public void SetActiveCount(int count)
            {
                ActiveCount = Mathf.Clamp(count, 0, decorations.Count);
                for (int index = ActiveCount; index < decorations.Count; index++)
                {
                    decorations[index].gameObject.SetActive(false);
                }
            }

            public void Deactivate()
            {
                ActiveCount = 0;
                foreach (WorldDecoration decoration in decorations)
                {
                    if (decoration != null)
                    {
                        decoration.gameObject.SetActive(false);
                    }
                }

                root.gameObject.SetActive(false);
            }
        }
    }
}
