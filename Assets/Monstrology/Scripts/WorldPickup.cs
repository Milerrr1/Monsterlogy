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
        Accessory,
        CreatureNest,
        SpecialEvent
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
        [SerializeField] private BiomeEventData biomeEvent;
        [SerializeField] private SpriteRenderer visual;

        private ExplorationSystem exploration;
        private WorldExplorationManager owner;
        private SpawnPoint spawnPoint;
        private bool collected;
        private Vector3 baseScale;
        private int trackChainStage = -1;
        private float expiresAt;
        private bool persistent;
        private string nestId;
        private TextMesh worldLabel;

        public static IEnumerable<WorldPickup> ActivePickups { get { return Active; } }
        public WorldPickupType PickupType { get { return pickupType; } }
        public CreatureData Creature { get { return creature; } }
        public ItemData Item { get { return item; } }
        public AccessoryData Accessory { get { return accessory; } }
        public bool CanInteract { get { return isActiveAndEnabled && !collected; } }
        public TrackType TrackType { get { return trackType; } }
        public int TrackChainStage { get { return trackChainStage; } }
        public BiomeEventData BiomeEvent { get { return biomeEvent; } }
        public bool IsPersistent { get { return persistent; } }
        public string NestId { get { return nestId; } }
        public SpriteRenderer VisualRenderer { get { return visual; } }

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
                    case WorldPickupType.CreatureNest:
                        return creature != null
                            ? "осмотреть логово " + creature.creatureName
                            : "осмотреть логово";
                    case WorldPickupType.SpecialEvent:
                        return biomeEvent != null
                            ? "исследовать " + biomeEvent.specialObjectName
                            : "исследовать редкий объект";
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
            if (expiresAt > 0f && Time.time >= expiresAt)
            {
                if (owner != null)
                {
                    owner.NotifyPickupCollected(this);
                }

                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

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
            int chainStage = -1,
            BiomeEventData eventData = null,
            float lifetimeSeconds = 0f)
        {
            pickupType = type;
            creature = creatureData;
            item = itemData;
            trackType = traceType;
            accessory = accessoryData;
            biomeEvent = eventData;
            exploration = explorationSystem;
            owner = manager;
            spawnPoint = point;
            trackChainStage = chainStage;
            expiresAt = lifetimeSeconds > 0f ? Time.time + lifetimeSeconds : 0f;

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
            if (pickupType == WorldPickupType.CreatureNest && owner != null)
            {
                owner.ResolveCreatureNest(this);
                collected = false;
                return;
            }
            else if (pickupType == WorldPickupType.SpecialEvent && owner != null)
            {
                owner.ResolveSpecialMapEvent(this);
            }
            else
            {
                if (pickupType == WorldPickupType.Trace && owner != null)
                {
                    owner.AdvanceTrackChain(this);
                }

                ExplorationResultType resultType = ConvertType(pickupType);
                exploration.ResolveWorldDiscovery(resultType, creature, item, trackType, accessory);
            }

            if (owner != null)
            {
                owner.NotifyPickupCollected(this);
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public void ConfigureNestMarker(string configuredNestId, bool makePersistent)
        {
            nestId = configuredNestId ?? string.Empty;
            persistent = makePersistent;
            if (persistent)
            {
                expiresAt = 0f;
                if (spawnPoint != null)
                {
                    spawnPoint.Release(this);
                    spawnPoint = null;
                }
            }

            gameObject.name = string.IsNullOrEmpty(nestId)
                ? "CreatureNest"
                : "Nest_" + nestId;
            BuildNestDecoration();
        }

        private void RefreshVisual()
        {
            if (visual == null || visual.transform == transform)
            {
                SpriteRenderer previous = visual;
                GameObject visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(transform, false);
                visual = visualObject.AddComponent<SpriteRenderer>();
                if (previous != null)
                {
                    visual.sprite = previous.sprite;
                    visual.color = previous.color;
                    visual.sharedMaterial = previous.sharedMaterial;
                    Destroy(previous);
                }
            }

            if (pickupType == WorldPickupType.Creature)
            {
                CreatureVisualRig rig = GetComponent<CreatureVisualRig>();
                if (rig == null)
                {
                    rig = gameObject.AddComponent<CreatureVisualRig>();
                }

                rig.ApplyCreature(creature, GetColor(), 10);
                visual = rig.CreatureRenderer;
                return;
            }

            visual.sortingOrder = 10;
            visual.sprite = GetSprite();
            visual.color = GetColor();
        }

        private void BuildNestDecoration()
        {
            if (pickupType != WorldPickupType.CreatureNest)
            {
                return;
            }

            Transform creatureMarker = transform.Find("LinkedCreature");
            if (creatureMarker == null)
            {
                GameObject markerObject = new GameObject("LinkedCreature");
                markerObject.transform.SetParent(transform, false);
                markerObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                markerObject.transform.localScale = Vector3.one * 0.56f;
                CreatureVisualRig markerRig =
                    markerObject.AddComponent<CreatureVisualRig>();
                markerRig.ApplyCreature(
                    creature,
                    new Color(1f, 0.68f, 0.3f),
                    11);
            }

            if (worldLabel == null)
            {
                GameObject labelObject = new GameObject("NestName");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.08f;
                worldLabel = labelObject.AddComponent<TextMesh>();
                worldLabel.anchor = TextAnchor.MiddleCenter;
                worldLabel.alignment = TextAlignment.Center;
                worldLabel.fontSize = 32;
                worldLabel.characterSize = 1f;
                worldLabel.color = new Color(1f, 0.88f, 0.58f);
                MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 12;
                }
            }

            worldLabel.text = creature != null
                ? "Логово " + creature.creatureName
                : "Логово";
        }

        private Sprite GetSprite()
        {
            switch (pickupType)
            {
                case WorldPickupType.Creature:
                    return SpriteDatabase.Active.GetCreatureWorld(creature);
                case WorldPickupType.Trace:
                    return SpriteDatabase.Active.GetTrack(trackType);
                case WorldPickupType.Item:
                case WorldPickupType.Egg:
                    return SpriteDatabase.Active.GetItem(item);
                case WorldPickupType.Accessory:
                    return SpriteDatabase.Active.GetAccessory(accessory);
                case WorldPickupType.CreatureNest:
                    return SpriteDatabase.Active.GetNest(GetNestData());
                case WorldPickupType.SpecialEvent:
                    return SpriteDatabase.Active.GetSpecialEvent(biomeEvent);
                case WorldPickupType.Nothing:
                    return SpriteDatabase.Active.GetEmptyFinding();
                default:
                    return SpriteDatabase.Active.GetItem(item);
            }
        }

        private Color GetColor()
        {
            switch (pickupType)
            {
                case WorldPickupType.Creature:
                    return SpriteDatabase.Active.HasCreatureArtwork(creature)
                        ? Color.white
                        : new Color(0.96f, 0.55f, 0.48f);
                case WorldPickupType.Trace:
                    return new Color(1f, 0.82f, 0.34f);
                case WorldPickupType.Item:
                    return SpriteDatabase.Active.HasItemArtwork(item)
                        ? Color.white
                        : new Color(0.38f, 0.88f, 0.68f);
                case WorldPickupType.Egg:
                    return SpriteDatabase.Active.HasItemArtwork(item)
                        ? Color.white
                        : new Color(0.82f, 0.66f, 1f);
                case WorldPickupType.Accessory:
                    return SpriteDatabase.Active.HasAccessoryArtwork(accessory)
                        ? Color.white
                        : accessory != null
                            ? PetLocalization.RarityColor(accessory.rarity)
                            : new Color(0.96f, 0.72f, 0.3f);
                case WorldPickupType.CreatureNest:
                    return SpriteDatabase.Active.HasNestArtwork(GetNestData())
                        ? Color.white
                        : new Color(0.75f, 0.52f, 0.28f);
                case WorldPickupType.SpecialEvent:
                    return SpriteDatabase.Active.HasSpecialEventArtwork(biomeEvent)
                        ? Color.white
                        : new Color(0.95f, 0.42f, 0.82f);
                default:
                    return new Color(0.72f, 0.76f, 0.84f);
            }
        }

        private CreatureNestData GetNestData()
        {
            return GameManager.Instance != null && GameManager.Instance.Content != null
                ? GameManager.Instance.Content.creatureNests.Find(data =>
                    data != null &&
                    ((!string.IsNullOrEmpty(nestId) && data.id == nestId) ||
                     (creature != null && data.speciesId == creature.id)))
                : null;
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

}
