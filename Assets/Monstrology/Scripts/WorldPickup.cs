using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public enum WorldPickupType
    {
        Creature,
        Trace,
        Item,
        Egg,
        Nothing,
        Accessory
    }

    [RequireComponent(typeof(Collider2D))]
    public class WorldPickup : MonoBehaviour
    {
        private static readonly HashSet<WorldPickup> Active = new HashSet<WorldPickup>();

        [SerializeField] private WorldPickupType pickupType;
        [SerializeField] private CreatureData creature;
        [SerializeField] private ItemData item;
        [SerializeField] private TrackType trackType;
        [SerializeField] private AccessoryData accessory;
        [SerializeField] private SpriteRenderer visual;

        private ExplorationSystem exploration;
        private WorldExplorationManager owner;
        private SpawnPoint spawnPoint;
        private bool collected;
        private Vector3 baseScale;
        private int trackChainStage = -1;

        public static IEnumerable<WorldPickup> ActivePickups { get { return Active; } }
        public WorldPickupType PickupType { get { return pickupType; } }
        public CreatureData Creature { get { return creature; } }
        public ItemData Item { get { return item; } }
        public AccessoryData Accessory { get { return accessory; } }
        public bool CanInteract { get { return isActiveAndEnabled && !collected; } }
        public TrackType TrackType { get { return trackType; } }
        public int TrackChainStage { get { return trackChainStage; } }

        public string DisplayName
        {
            get
            {
                switch (pickupType)
                {
                    case WorldPickupType.Creature:
                        return creature != null ? creature.creatureName : "неизвестное существо";
                    case WorldPickupType.Trace:
                        return Localization.Track(trackType);
                    case WorldPickupType.Item:
                        return item != null ? item.itemName : "подобрать предмет";
                    case WorldPickupType.Egg:
                        return item != null ? item.itemName : "осмотреть яйцо";
                    case WorldPickupType.Accessory:
                        return accessory != null ? accessory.displayName : "подобрать одежду";
                    default:
                        return "проверить место";
                }
            }
        }

        private void Awake()
        {
            Collider2D pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;

            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2.4f + transform.position.x) * 0.045f;
            transform.localScale = baseScale * pulse;
        }

        private void OnDestroy()
        {
            Active.Remove(this);
            if (spawnPoint != null)
            {
                spawnPoint.Release(this);
            }
        }

        public void Initialize(
            WorldPickupType type,
            CreatureData creatureData,
            ItemData itemData,
            TrackType traceType,
            ExplorationSystem explorationSystem,
            WorldExplorationManager manager,
            SpawnPoint point,
            AccessoryData accessoryData = null,
            int chainStage = -1)
        {
            pickupType = type;
            creature = creatureData;
            item = itemData;
            trackType = traceType;
            accessory = accessoryData;
            exploration = explorationSystem;
            owner = manager;
            spawnPoint = point;
            trackChainStage = chainStage;

            if (spawnPoint != null)
            {
                spawnPoint.Occupy(this);
            }

            RefreshVisual();
        }

        public void Interact()
        {
            if (collected || exploration == null)
            {
                return;
            }

            collected = true;
            if (pickupType == WorldPickupType.Trace && owner != null)
            {
                owner.AdvanceTrackChain(this);
            }

            ExplorationResultType resultType = ConvertType(pickupType);
            exploration.ResolveWorldDiscovery(resultType, creature, item, trackType, accessory);

            if (owner != null)
            {
                owner.NotifyPickupCollected(this);
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void RefreshVisual()
        {
            if (visual == null)
            {
                visual = gameObject.AddComponent<SpriteRenderer>();
            }

            visual.sortingOrder = 10;
            visual.sprite = GetSprite();
            visual.color = GetColor();
        }

        private Sprite GetSprite()
        {
            if (pickupType == WorldPickupType.Creature && creature != null && creature.icon != null)
            {
                return creature.icon;
            }

            if ((pickupType == WorldPickupType.Item || pickupType == WorldPickupType.Egg) &&
                item != null && item.icon != null)
            {
                return item.icon;
            }

            if (pickupType == WorldPickupType.Accessory && accessory != null && accessory.icon != null)
            {
                return accessory.icon;
            }

            switch (pickupType)
            {
                case WorldPickupType.Creature:
                    return WorldPlaceholderSprites.Circle;
                case WorldPickupType.Trace:
                    return WorldPlaceholderSprites.Diamond;
                case WorldPickupType.Egg:
                    return WorldPlaceholderSprites.Egg;
                case WorldPickupType.Accessory:
                    return WorldPlaceholderSprites.Square;
                case WorldPickupType.Nothing:
                    return WorldPlaceholderSprites.Ring;
                default:
                    return WorldPlaceholderSprites.Square;
            }
        }

        private Color GetColor()
        {
            switch (pickupType)
            {
                case WorldPickupType.Creature:
                    return creature != null && creature.icon != null
                        ? Color.white
                        : new Color(0.96f, 0.55f, 0.48f);
                case WorldPickupType.Trace:
                    return new Color(1f, 0.82f, 0.34f);
                case WorldPickupType.Item:
                    return item != null && item.icon != null
                        ? Color.white
                        : new Color(0.38f, 0.88f, 0.68f);
                case WorldPickupType.Egg:
                    return item != null && item.icon != null
                        ? Color.white
                        : new Color(0.82f, 0.66f, 1f);
                case WorldPickupType.Accessory:
                    return accessory != null && accessory.icon != null
                        ? Color.white
                        : accessory != null
                            ? PetLocalization.RarityColor(accessory.rarity)
                            : new Color(0.96f, 0.72f, 0.3f);
                default:
                    return new Color(0.72f, 0.76f, 0.84f);
            }
        }

        private static ExplorationResultType ConvertType(WorldPickupType type)
        {
            switch (type)
            {
                case WorldPickupType.Creature:
                    return ExplorationResultType.Creature;
                case WorldPickupType.Trace:
                    return ExplorationResultType.Track;
                case WorldPickupType.Item:
                    return ExplorationResultType.Item;
                case WorldPickupType.Egg:
                    return ExplorationResultType.Egg;
                case WorldPickupType.Accessory:
                    return ExplorationResultType.Accessory;
                default:
                    return ExplorationResultType.Nothing;
            }
        }
    }

    internal static class WorldPlaceholderSprites
    {
        private static Sprite square;
        private static Sprite circle;
        private static Sprite diamond;
        private static Sprite egg;
        private static Sprite ring;
        private static Sprite player;

        public static Sprite Square { get { return square ?? (square = Create(Shape.Square)); } }
        public static Sprite Circle { get { return circle ?? (circle = Create(Shape.Circle)); } }
        public static Sprite Diamond { get { return diamond ?? (diamond = Create(Shape.Diamond)); } }
        public static Sprite Egg { get { return egg ?? (egg = Create(Shape.Egg)); } }
        public static Sprite Ring { get { return ring ?? (ring = Create(Shape.Ring)); } }
        public static Sprite Player { get { return player ?? (player = Create(Shape.Player)); } }

        private enum Shape
        {
            Square,
            Circle,
            Diamond,
            Egg,
            Ring,
            Player
        }

        private static Sprite Create(Shape shape)
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Runtime " + shape;
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = clear;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    bool filled = IsFilled(shape, nx, ny);
                    if (filled)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            sprite.name = "Runtime " + shape;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static bool IsFilled(Shape shape, float x, float y)
        {
            switch (shape)
            {
                case Shape.Circle:
                    return x * x + y * y <= 0.78f;
                case Shape.Diamond:
                    return Mathf.Abs(x) + Mathf.Abs(y) <= 0.86f;
                case Shape.Egg:
                    return x * x / 0.48f + (y + 0.12f) * (y + 0.12f) / 0.78f <= 1f;
                case Shape.Ring:
                    float radius = x * x + y * y;
                    return radius <= 0.8f && radius >= 0.38f;
                case Shape.Player:
                    bool body = Mathf.Abs(x) <= 0.62f && Mathf.Abs(y) <= 0.62f;
                    bool pointer = y > 0.45f && Mathf.Abs(x) <= 0.9f - y;
                    return body || pointer;
                default:
                    return Mathf.Abs(x) <= 0.72f && Mathf.Abs(y) <= 0.72f;
            }
        }
    }
}
