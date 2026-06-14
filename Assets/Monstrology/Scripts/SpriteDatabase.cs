using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class SpriteIdEntry
    {
        public string id;
        public Sprite sprite;
    }

    [Serializable]
    public class CreatureSpriteEntry
    {
        public string id;
        public Sprite portraitSprite;
        public Sprite worldSprite;
        public Sprite evolutionSprite;
        public Vector2 headAnchorOffset;
        public Vector2 bodyAnchorOffset;
        public Vector2 legAnchorOffset;
    }

    [Serializable]
    public class BiomeDecorationEntry
    {
        public string id;
        public Sprite sprite;
        public Vector2 targetWorldSize = Vector2.one;
        [Min(0f)] public float spawnWeight = 1f;
        [Min(0)] public int minCount = 1;
        [Min(0)] public int maxCount = 2;
        [Min(0f)] public float minimumDistance = 1.5f;
        public Vector2 colliderSize = new Vector2(0.5f, 0.2f);
        public Vector2 colliderOffset = new Vector2(0f, 0.1f);
        public Vector2 scaleVariation = new Vector2(0.9f, 1.1f);
        public bool allowFlipX = true;
        public bool blocksMovement;
    }

    [Serializable]
    public class BiomeVisualConfig
    {
        public Sprite groundDetail;
        public bool groundDetailEnabled = true;
        [Range(0f, 1f)] public float groundDetailOpacity = 0.2f;
        public Vector2 backgroundWorldSize = new Vector2(30f, 18f);
        public Vector2 groundTileWorldSize = new Vector2(8f, 8f);
        public List<BiomeDecorationEntry> decorations =
            new List<BiomeDecorationEntry>();
    }

    [Serializable]
    public class BiomeSpriteEntry
    {
        public BiomeType biome;
        public Sprite background;
        public Sprite primaryDecoration;
        public Sprite secondaryDecoration;
        public BiomeVisualConfig visualConfig = new BiomeVisualConfig();
    }

    [Serializable]
    public class TrackSpriteEntry
    {
        public TrackType track;
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "SpriteDatabase", menuName = "Monstrology/Sprite Database")]
    public class SpriteDatabase : ScriptableObject
    {
        private static SpriteDatabase active;

        [Header("Content by stable id")]
        [SerializeField] private List<CreatureSpriteEntry> creatureVisuals =
            new List<CreatureSpriteEntry>();
        [Tooltip("Legacy single-sprite creature overrides.")]
        [SerializeField] private List<SpriteIdEntry> creatures = new List<SpriteIdEntry>();
        [SerializeField] private List<SpriteIdEntry> items = new List<SpriteIdEntry>();
        [SerializeField] private List<SpriteIdEntry> accessories = new List<SpriteIdEntry>();
        [SerializeField] private List<SpriteIdEntry> nests = new List<SpriteIdEntry>();
        [SerializeField] private List<SpriteIdEntry> specialEvents = new List<SpriteIdEntry>();

        [Header("Biomes and tracks")]
        [SerializeField] private List<BiomeSpriteEntry> biomes = new List<BiomeSpriteEntry>();
        [SerializeField] private List<TrackSpriteEntry> tracks = new List<TrackSpriteEntry>();

        [Header("World")]
        [SerializeField] private Sprite player;
        [SerializeField] private Sprite creatureFallback;
        [SerializeField] private Sprite unknownCreature;
        [SerializeField] private Sprite itemFallback;
        [SerializeField] private Sprite accessoryFallback;
        [SerializeField] private Sprite nestFallback;
        [SerializeField] private Sprite eggFallback;
        [SerializeField] private Sprite specialEventFallback;
        [SerializeField] private Sprite emptyFinding;
        [SerializeField] private Sprite compassArrow;
        [SerializeField] private Sprite signatureSetEffect;
        [SerializeField] private Sprite biomeFallback;
        [SerializeField] private Sprite primaryDecorationFallback;
        [SerializeField] private Sprite secondaryDecorationFallback;

        [Header("UI")]
        [SerializeField] private Sprite roundedPanel;
        [SerializeField] private Sprite joystickBase;
        [SerializeField] private Sprite joystickHandle;

        [NonSerialized] private Dictionary<string, Sprite> runtimeFallbacks;

        public static SpriteDatabase Active
        {
            get
            {
                if (active == null)
                {
                    active = Resources.Load<SpriteDatabase>("SpriteDatabase");
                    if (active == null)
                    {
                        active = CreateInstance<SpriteDatabase>();
                        active.name = "Runtime Sprite Database";
                        active.hideFlags = HideFlags.DontSave;
                    }
                }

                return active;
            }
        }

        public static void Install(SpriteDatabase database)
        {
            active = database != null ? database : Resources.Load<SpriteDatabase>("SpriteDatabase");
            if (active == null)
            {
                active = CreateInstance<SpriteDatabase>();
                active.name = "Runtime Sprite Database";
                active.hideFlags = HideFlags.DontSave;
            }
        }

        public Sprite GetCreature(CreatureData data)
        {
            return GetCreaturePortrait(data);
        }

        public Sprite GetCreaturePortrait(CreatureData data)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            return (entry != null ? entry.portraitSprite : null) ??
                   (data != null ? data.portraitSprite : null) ??
                   Find(creatures, data != null ? data.id : null) ??
                   (data != null ? data.icon : null) ??
                   GetOrCreate(creatureFallback, RuntimeSpriteShape.Circle, "Creature Fallback");
        }

        public Sprite GetCreatureWorld(CreatureData data)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            return (entry != null ? entry.worldSprite : null) ??
                   (data != null ? data.worldSprite : null) ??
                   GetCreaturePortrait(data);
        }

        public Sprite GetCreatureEvolution(CreatureData data)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            return (entry != null ? entry.evolutionSprite : null) ??
                   (data != null ? data.evolutionSprite : null) ??
                   GetCreatureWorld(data);
        }

        public Vector2 GetCreatureAnchorOffset(
            CreatureData data,
            AccessorySlot slot)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            if (entry == null)
            {
                return Vector2.zero;
            }

            switch (slot)
            {
                case AccessorySlot.Head:
                    return entry.headAnchorOffset;
                case AccessorySlot.Body:
                    return entry.bodyAnchorOffset;
                default:
                    return entry.legAnchorOffset;
            }
        }

        public bool HasCreatureArtwork(CreatureData data)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            return (entry != null &&
                    (entry.portraitSprite != null || entry.worldSprite != null)) ||
                   (data != null &&
                    (data.portraitSprite != null ||
                     data.worldSprite != null ||
                     data.icon != null)) ||
                   Find(creatures, data != null ? data.id : null) != null;
        }

        public bool HasCreaturePortrait(CreatureData data)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            return (entry != null && entry.portraitSprite != null) ||
                   (data != null &&
                    (data.portraitSprite != null || data.icon != null)) ||
                   Find(creatures, data != null ? data.id : null) != null;
        }

        public bool HasCreatureWorld(CreatureData data)
        {
            CreatureSpriteEntry entry = FindCreature(data != null ? data.id : null);
            return (entry != null && entry.worldSprite != null) ||
                   (data != null && data.worldSprite != null) ||
                   HasCreaturePortrait(data);
        }

        public Sprite GetUnknownCreature()
        {
            return GetOrCreate(unknownCreature, RuntimeSpriteShape.Ring, "Unknown Creature");
        }

        public Sprite GetItem(ItemData data)
        {
            Sprite assigned = Find(items, data != null ? data.id : null) ??
                              (data != null ? data.icon : null);
            if (assigned != null)
            {
                return assigned;
            }

            return data != null && data.kind == ItemKind.Egg
                ? GetOrCreate(eggFallback, RuntimeSpriteShape.Egg, "Egg Fallback")
                : GetOrCreate(itemFallback, RuntimeSpriteShape.Diamond, "Item Fallback");
        }

        public bool HasItemArtwork(ItemData data)
        {
            return Find(items, data != null ? data.id : null) != null ||
                   (data != null && data.icon != null);
        }

        public Sprite GetAccessory(AccessoryData data)
        {
            return Find(accessories, data != null ? data.id : null) ??
                   (data != null ? data.icon : null) ??
                   GetOrCreate(
                       accessoryFallback,
                       RuntimeSpriteShape.Square,
                       "Accessory Fallback");
        }

        public bool HasAccessoryArtwork(AccessoryData data)
        {
            return Find(accessories, data != null ? data.id : null) != null ||
                   (data != null && data.icon != null);
        }

        public Sprite GetNest(CreatureNestData data)
        {
            return Find(nests, data != null ? data.id : null) ??
                   (data != null ? data.icon : null) ??
                   GetOrCreate(nestFallback, RuntimeSpriteShape.Ring, "Nest Fallback");
        }

        public bool HasNestArtwork(CreatureNestData data)
        {
            return Find(nests, data != null ? data.id : null) != null ||
                   (data != null && data.icon != null);
        }

        public Sprite GetSpecialEvent(BiomeEventData data)
        {
            return Find(specialEvents, data != null ? data.id : null) ??
                   (data != null ? data.icon : null) ??
                   GetOrCreate(
                       specialEventFallback,
                       RuntimeSpriteShape.Diamond,
                       "Special Event Fallback");
        }

        public bool HasSpecialEventArtwork(BiomeEventData data)
        {
            return Find(specialEvents, data != null ? data.id : null) != null ||
                   (data != null && data.icon != null);
        }

        public Sprite GetTrack(TrackType track)
        {
            TrackSpriteEntry entry = tracks.Find(item => item != null && item.track == track);
            if (entry != null && entry.sprite != null)
            {
                return entry.sprite;
            }

            switch (track)
            {
                case TrackType.EggShell:
                    return GetOrCreate(eggFallback, RuntimeSpriteShape.Egg, "Egg Fallback");
                case TrackType.Lair:
                    return GetOrCreate(nestFallback, RuntimeSpriteShape.Ring, "Nest Fallback");
                case TrackType.Fur:
                    return GetOrCreate(
                        itemFallback,
                        RuntimeSpriteShape.Circle,
                        "Fur Track Fallback");
                default:
                    return GetOrCreate(
                        specialEventFallback,
                        RuntimeSpriteShape.Diamond,
                        "Generic Track Fallback");
            }
        }

        public Sprite GetBiomeBackground(BiomeData biome, BiomeMapData mapData)
        {
            BiomeSpriteEntry entry = FindBiome(biome != null ? biome.type : BiomeType.Forest);
            return entry != null && entry.background != null
                ? entry.background
                : mapData != null && mapData.background != null
                    ? mapData.background
                    : biome != null && biome.background != null
                        ? biome.background
                        : GetOrCreate(
                            biomeFallback,
                            RuntimeSpriteShape.Square,
                            "Biome Fallback");
        }

        public bool HasBiomeArtwork(BiomeData biome, BiomeMapData mapData)
        {
            BiomeSpriteEntry entry = FindBiome(biome != null ? biome.type : BiomeType.Forest);
            return (entry != null && entry.background != null) ||
                   (mapData != null && mapData.background != null) ||
                   (biome != null && biome.background != null);
        }

        public Sprite GetBiomeDecoration(BiomeType biome, bool secondary)
        {
            BiomeSpriteEntry entry = FindBiome(biome);
            Sprite assigned = entry != null
                ? secondary ? entry.secondaryDecoration : entry.primaryDecoration
                : null;
            if (assigned == null &&
                entry != null &&
                entry.visualConfig != null &&
                entry.visualConfig.decorations != null)
            {
                int index = secondary ? 1 : 0;
                if (entry.visualConfig.decorations.Count > index)
                {
                    assigned = entry.visualConfig.decorations[index].sprite;
                }
            }

            if (assigned != null)
            {
                return assigned;
            }

            return secondary
                ? GetOrCreate(
                    secondaryDecorationFallback,
                    RuntimeSpriteShape.Square,
                    "Secondary Biome Decoration")
                : GetOrCreate(
                    primaryDecorationFallback,
                    RuntimeSpriteShape.Circle,
                    "Primary Biome Decoration");
        }

        public BiomeVisualConfig GetBiomeVisualConfig(BiomeType biome)
        {
            BiomeSpriteEntry entry = FindBiome(biome);
            return entry != null ? entry.visualConfig : null;
        }

        public bool HasBiomeDecorationSet(BiomeType biome)
        {
            BiomeVisualConfig config = GetBiomeVisualConfig(biome);
            return config != null &&
                   config.decorations != null &&
                   config.decorations.Exists(decoration =>
                       decoration != null && decoration.sprite != null);
        }

        public Sprite GetPlayer()
        {
            return GetOrCreate(player, RuntimeSpriteShape.Player, "Player");
        }

        public Sprite GetCompassArrow()
        {
            return GetOrCreate(compassArrow, RuntimeSpriteShape.Arrow, "Compass Arrow");
        }

        public Sprite GetSignatureSetEffect()
        {
            return GetOrCreate(
                signatureSetEffect,
                RuntimeSpriteShape.Ring,
                "Signature Set Effect");
        }

        public Sprite GetEmptyFinding()
        {
            return GetOrCreate(emptyFinding, RuntimeSpriteShape.Ring, "Empty Finding");
        }

        public Sprite GetRoundedPanel()
        {
            if (roundedPanel != null)
            {
                return roundedPanel;
            }

            if (runtimeFallbacks == null)
            {
                runtimeFallbacks = new Dictionary<string, Sprite>();
            }

            Sprite runtime;
            if (!runtimeFallbacks.TryGetValue("Rounded UI Panel", out runtime))
            {
                runtime = RuntimeSpriteFactory.CreateRoundedPanel();
                runtimeFallbacks.Add("Rounded UI Panel", runtime);
            }

            return runtime;
        }

        public Sprite GetJoystickBase()
        {
            return GetOrCreate(joystickBase, RuntimeSpriteShape.Circle, "Joystick Base");
        }

        public Sprite GetJoystickHandle()
        {
            return GetOrCreate(joystickHandle, RuntimeSpriteShape.Circle, "Joystick Handle");
        }

        private BiomeSpriteEntry FindBiome(BiomeType biome)
        {
            return biomes.Find(entry => entry != null && entry.biome == biome);
        }

        private CreatureSpriteEntry FindCreature(string id)
        {
            return string.IsNullOrEmpty(id)
                ? null
                : creatureVisuals.Find(entry => entry != null && entry.id == id);
        }

        private static Sprite Find(ICollection<SpriteIdEntry> entries, string id)
        {
            if (entries == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (SpriteIdEntry entry in entries)
            {
                if (entry != null && entry.id == id && entry.sprite != null)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private Sprite GetOrCreate(
            Sprite assigned,
            RuntimeSpriteShape shape,
            string displayName)
        {
            if (assigned != null)
            {
                return assigned;
            }

            if (runtimeFallbacks == null)
            {
                runtimeFallbacks = new Dictionary<string, Sprite>();
            }

            Sprite runtime;
            if (!runtimeFallbacks.TryGetValue(displayName, out runtime))
            {
                runtime = RuntimeSpriteFactory.Create(shape, displayName);
                runtimeFallbacks.Add(displayName, runtime);
            }

            return runtime;
        }
    }

    internal enum RuntimeSpriteShape
    {
        Square,
        Circle,
        Diamond,
        Egg,
        Ring,
        Player,
        Arrow
    }

    internal static class RuntimeSpriteFactory
    {
        public static Sprite Create(RuntimeSpriteShape shape, string displayName)
        {
            const int size = 32;
            Texture2D texture = NewTexture(size, FilterMode.Point);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    pixels[y * size + x] = IsFilled(shape, nx, ny)
                        ? Color.white
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return CreateSprite(texture, displayName, Vector4.zero);
        }

        public static Sprite CreateRoundedPanel()
        {
            const int size = 32;
            const int radius = 9;
            Texture2D texture = NewTexture(size, FilterMode.Bilinear);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cornerX = x < radius ? radius : x >= size - radius ? size - radius - 1 : x;
                    int cornerY = y < radius ? radius : y >= size - radius ? size - radius - 1 : y;
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        new Vector2(cornerX, cornerY));
                    pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return CreateSprite(
                texture,
                "Rounded UI Panel",
                new Vector4(radius, radius, radius, radius));
        }

        private static Texture2D NewTexture(int size, FilterMode filterMode)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Runtime Sprite Fallback";
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = filterMode;
            return texture;
        }

        private static Sprite CreateSprite(
            Texture2D texture,
            string displayName,
            Vector4 border)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = displayName;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static bool IsFilled(RuntimeSpriteShape shape, float x, float y)
        {
            switch (shape)
            {
                case RuntimeSpriteShape.Circle:
                    return x * x + y * y <= 0.78f;
                case RuntimeSpriteShape.Diamond:
                    return Mathf.Abs(x) + Mathf.Abs(y) <= 0.86f;
                case RuntimeSpriteShape.Egg:
                    return x * x / 0.48f + (y + 0.12f) * (y + 0.12f) / 0.78f <= 1f;
                case RuntimeSpriteShape.Ring:
                    float radius = x * x + y * y;
                    return radius <= 0.8f && radius >= 0.38f;
                case RuntimeSpriteShape.Player:
                    bool body = Mathf.Abs(x) <= 0.62f && Mathf.Abs(y) <= 0.62f;
                    bool pointer = y > 0.45f && Mathf.Abs(x) <= 0.9f - y;
                    return body || pointer;
                case RuntimeSpriteShape.Arrow:
                    bool shaft = Mathf.Abs(x) <= 0.18f && y <= 0.45f;
                    bool head = y > 0.05f && Mathf.Abs(x) <= 0.75f - y * 0.55f;
                    return shaft || head;
                default:
                    return Mathf.Abs(x) <= 0.72f && Mathf.Abs(y) <= 0.72f;
            }
        }
    }
}
