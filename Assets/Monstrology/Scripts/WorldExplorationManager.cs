using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class WorldExplorationManager : MonoBehaviour
    {
        [SerializeField] private List<BiomeMapData> mapCatalog = new List<BiomeMapData>();
        [SerializeField] private Transform worldRoot;

        private readonly List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        private readonly HashSet<WorldPickup> activePickups = new HashSet<WorldPickup>();

        private GameManager game;
        private ExplorationSystem exploration;
        private PlayerController2D player;
        private CameraFollow2D cameraFollow;
        private Camera worldCamera;
        private BiomeData loadedBiome;
        private BiomeMapData currentMapData;
        private GameObject currentMap;
        private float respawnTimer;
        private bool switchingMap;

        public BiomeData LoadedBiome { get { return loadedBiome; } }
        public BiomeMapData CurrentMapData { get { return currentMapData; } }
        public int ActivePickupCount { get { return activePickups.Count; } }

        public void Initialize(
            GameManager gameManager,
            ExplorationSystem explorationSystem,
            PlayerController2D playerController,
            CameraFollow2D followCamera)
        {
            game = gameManager;
            exploration = explorationSystem;
            player = playerController;
            cameraFollow = followCamera;
            worldCamera = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;

            if (worldRoot == null)
            {
                GameObject root = new GameObject("World");
                root.transform.SetParent(transform, false);
                worldRoot = root.transform;
            }

            if (game != null)
            {
                game.StateChanged -= HandleGameStateChanged;
                game.StateChanged += HandleGameStateChanged;
                LoadBiome(game.CurrentBiome);
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.StateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            if (currentMap == null || currentMapData == null || switchingMap)
            {
                return;
            }

            activePickups.RemoveWhere(pickup => pickup == null);
            int targetCount = Mathf.Max(0, currentMapData.activeFindings);
            if (activePickups.Count >= targetCount)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                SpawnOnePickup();
                respawnTimer = Mathf.Max(0.25f, currentMapData.respawnInterval);
            }
        }

        public void ReloadCurrentBiome()
        {
            LoadBiome(game != null ? game.CurrentBiome : null, true);
        }

        public void NotifyPickupCollected(WorldPickup pickup)
        {
            if (pickup != null)
            {
                activePickups.Remove(pickup);
            }

            if (currentMapData != null)
            {
                respawnTimer = Mathf.Max(0.25f, currentMapData.respawnInterval);
            }
        }

        private void HandleGameStateChanged()
        {
            if (game != null && game.CurrentBiome != loadedBiome)
            {
                LoadBiome(game.CurrentBiome);
            }
        }

        private void LoadBiome(BiomeData biome, bool force = false)
        {
            if (biome == null || (!force && biome == loadedBiome && currentMap != null))
            {
                return;
            }

            switchingMap = true;
            ClearCurrentMap();
            loadedBiome = biome;
            currentMapData = ResolveMapData(biome);

            if (currentMapData.mapPrefab != null)
            {
                currentMap = Instantiate(currentMapData.mapPrefab, worldRoot);
                currentMap.name = currentMapData.mapPrefab.name + " (Active)";
            }
            else
            {
                currentMap = BuildFallbackMap(biome, currentMapData);
            }

            AddDataBackground(currentMap, currentMapData);
            ResolveSpawnPoints();
            ConfigureCamera();
            MovePlayerToStart();
            SpawnInitialPickups();
            switchingMap = false;
        }

        private BiomeMapData ResolveMapData(BiomeData biome)
        {
            if (biome.mapData != null)
            {
                return biome.mapData;
            }

            BiomeMapData catalogEntry = mapCatalog.Find(data => data != null && data.Matches(biome));
            if (catalogEntry != null)
            {
                return catalogEntry;
            }

            BiomeMapData fallback = ScriptableObject.CreateInstance<BiomeMapData>();
            fallback.name = biome.type + " Runtime Map";
            fallback.hideFlags = HideFlags.DontSave;
            fallback.biomeId = biome.type.ToString();
            fallback.biomeType = biome.type;
            fallback.background = biome.background;
            fallback.backgroundColor = biome.fallbackColor;
            fallback.lightingColor = Color.white;
            fallback.worldSize = new Vector2(30f, 18f);
            fallback.playerStart = Vector2.zero;
            fallback.activeFindings = 7;
            fallback.respawnInterval = 7f;
            fallback.possibleCreatures.AddRange(biome.availableCreatures);
            return fallback;
        }

        private void ClearCurrentMap()
        {
            activePickups.Clear();
            spawnPoints.Clear();

            if (currentMap != null)
            {
                currentMap.SetActive(false);
                Destroy(currentMap);
                currentMap = null;
            }
        }

        private GameObject BuildFallbackMap(BiomeData biome, BiomeMapData data)
        {
            GameObject map = new GameObject(biome.type + "MapPrefab_Runtime");
            map.transform.SetParent(worldRoot, false);

            CreateBoundary(map.transform, data.worldSize);
            CreateFallbackDecorations(map.transform, data.backgroundColor);
            CreateFallbackSpawnPoints(map.transform, biome.type);
            return map;
        }

        private void AddDataBackground(GameObject map, BiomeMapData data)
        {
            if (map == null || data == null)
            {
                return;
            }

            Transform existing = map.transform.Find("Background");
            if (existing != null)
            {
                return;
            }

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(map.transform, false);
            backgroundObject.transform.localPosition = new Vector3(0f, 0f, 1f);
            SpriteRenderer renderer = backgroundObject.AddComponent<SpriteRenderer>();
            renderer.sprite = data.background != null ? data.background : WorldPlaceholderSprites.Square;
            renderer.color = data.background != null ? Color.white : data.backgroundColor;
            renderer.sortingOrder = -100;
            renderer.drawMode = SpriteDrawMode.Simple;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            backgroundObject.transform.localScale = new Vector3(
                data.worldSize.x / Mathf.Max(0.01f, spriteSize.x),
                data.worldSize.y / Mathf.Max(0.01f, spriteSize.y),
                1f);
        }

        private void ResolveSpawnPoints()
        {
            spawnPoints.Clear();
            spawnPoints.AddRange(currentMap.GetComponentsInChildren<SpawnPoint>(true));

            if (spawnPoints.Count == 0 && currentMapData.spawnPoints != null)
            {
                foreach (SpawnPoint source in currentMapData.spawnPoints)
                {
                    if (source == null)
                    {
                        continue;
                    }

                    GameObject pointObject = new GameObject(source.name);
                    pointObject.transform.SetParent(currentMap.transform, false);
                    pointObject.transform.localPosition = source.transform.localPosition;
                    SpawnPoint point = pointObject.AddComponent<SpawnPoint>();
                    point.SetZoneType(source.ZoneType);
                    spawnPoints.Add(point);
                }
            }

            if (spawnPoints.Count == 0)
            {
                CreateFallbackSpawnPoints(currentMap.transform, loadedBiome.type);
                spawnPoints.AddRange(currentMap.GetComponentsInChildren<SpawnPoint>(true));
            }
        }

        private void ConfigureCamera()
        {
            Vector2 size = currentMapData.worldSize;
            Bounds bounds = new Bounds(
                currentMap.transform.position,
                new Vector3(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y), 1f));

            if (worldCamera != null)
            {
                worldCamera.orthographic = true;
                worldCamera.backgroundColor = currentMapData.backgroundColor * currentMapData.lightingColor;
                worldCamera.backgroundColor = new Color(
                    worldCamera.backgroundColor.r,
                    worldCamera.backgroundColor.g,
                    worldCamera.backgroundColor.b,
                    1f);
            }

            RenderSettings.ambientLight = currentMapData.lightingColor;
            if (cameraFollow != null)
            {
                cameraFollow.SetBounds(bounds);
            }
        }

        private void MovePlayerToStart()
        {
            if (player == null)
            {
                return;
            }

            Vector2 start = currentMapData.playerStart;
            player.transform.position = currentMap.transform.TransformPoint(new Vector3(start.x, start.y, 0f));
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            if (playerBody != null)
            {
                playerBody.velocity = Vector2.zero;
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(player.transform);
                cameraFollow.SnapToTarget();
            }
        }

        private void SpawnInitialPickups()
        {
            activePickups.Clear();
            int count = Mathf.Min(
                Mathf.Max(0, currentMapData.activeFindings),
                spawnPoints.Count);

            for (int index = 0; index < count; index++)
            {
                if (!SpawnOnePickup())
                {
                    break;
                }
            }

            respawnTimer = Mathf.Max(0.25f, currentMapData.respawnInterval);
        }

        private bool SpawnOnePickup()
        {
            if (exploration == null || spawnPoints.Count == 0)
            {
                return false;
            }

            List<SpawnPoint> available = spawnPoints.FindAll(point => point != null && !point.IsOccupied);
            if (available.Count == 0)
            {
                return false;
            }

            SpawnPoint spawnPoint = available[Random.Range(0, available.Count)];
            WorldPickupType type = RollPickupType();
            CreatureData creature = null;
            ItemData item = null;
            TrackType track = exploration.SelectTrack();

            if (type == WorldPickupType.Creature)
            {
                IList<CreatureData> creatures = currentMapData.possibleCreatures.Count > 0
                    ? currentMapData.possibleCreatures
                    : loadedBiome.availableCreatures;
                creature = exploration.SelectCreature(creatures);
                if (creature == null)
                {
                    type = WorldPickupType.Trace;
                }
            }
            else if (type == WorldPickupType.Item)
            {
                item = exploration.SelectItem(false);
                if (item == null)
                {
                    type = WorldPickupType.Trace;
                }
            }
            else if (type == WorldPickupType.Egg)
            {
                item = exploration.SelectItem(true);
                if (item == null)
                {
                    type = WorldPickupType.Trace;
                }
            }

            GameObject pickupObject = new GameObject("Finding_" + type);
            pickupObject.transform.SetParent(currentMap.transform, true);
            pickupObject.transform.position = spawnPoint.GetSpawnPosition();
            pickupObject.transform.localScale = Vector3.one * 1.05f;
            CircleCollider2D pickupCollider = pickupObject.AddComponent<CircleCollider2D>();
            pickupCollider.radius = 0.48f;
            WorldPickup pickup = pickupObject.AddComponent<WorldPickup>();
            pickup.Initialize(type, creature, item, track, exploration, this, spawnPoint);
            activePickups.Add(pickup);
            return true;
        }

        private static WorldPickupType RollPickupType()
        {
            float roll = Random.value;
            if (roll < 0.46f)
            {
                return WorldPickupType.Creature;
            }

            if (roll < 0.70f)
            {
                return WorldPickupType.Trace;
            }

            if (roll < 0.84f)
            {
                return WorldPickupType.Item;
            }

            if (roll < 0.92f)
            {
                return WorldPickupType.Egg;
            }

            return WorldPickupType.Nothing;
        }

        private static void CreateBoundary(Transform parent, Vector2 size)
        {
            const float thickness = 1f;
            CreateWall(parent, "BoundaryTop", new Vector2(0f, size.y * 0.5f + thickness * 0.5f),
                new Vector2(size.x + thickness * 2f, thickness));
            CreateWall(parent, "BoundaryBottom", new Vector2(0f, -size.y * 0.5f - thickness * 0.5f),
                new Vector2(size.x + thickness * 2f, thickness));
            CreateWall(parent, "BoundaryLeft", new Vector2(-size.x * 0.5f - thickness * 0.5f, 0f),
                new Vector2(thickness, size.y));
            CreateWall(parent, "BoundaryRight", new Vector2(size.x * 0.5f + thickness * 0.5f, 0f),
                new Vector2(thickness, size.y));
        }

        private static void CreateWall(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static void CreateFallbackDecorations(Transform parent, Color biomeColor)
        {
            Vector2[] positions =
            {
                new Vector2(-5.5f, 2.6f),
                new Vector2(5.2f, 2.1f),
                new Vector2(-4.3f, -3.1f),
                new Vector2(4.8f, -3.3f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject decoration = new GameObject("Obstacle_" + (index + 1));
                decoration.transform.SetParent(parent, false);
                decoration.transform.localPosition = positions[index];
                decoration.transform.localScale = index % 2 == 0
                    ? new Vector3(1.8f, 1.2f, 1f)
                    : new Vector3(1.2f, 1.8f, 1f);
                SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
                renderer.sprite = index % 2 == 0
                    ? WorldPlaceholderSprites.Circle
                    : WorldPlaceholderSprites.Square;
                renderer.color = Color.Lerp(biomeColor, Color.black, 0.28f);
                renderer.sortingOrder = -5;
                decoration.AddComponent<BoxCollider2D>();
            }
        }

        private static void CreateFallbackSpawnPoints(Transform parent, BiomeType biomeType)
        {
            Vector2[] positions =
            {
                new Vector2(-10f, 5f),
                new Vector2(-6f, 0f),
                new Vector2(-10f, -5f),
                new Vector2(-2.5f, 5.5f),
                new Vector2(2.5f, 5.5f),
                new Vector2(6f, 0f),
                new Vector2(10f, 5f),
                new Vector2(10f, -5f),
                new Vector2(2.5f, -5.5f),
                new Vector2(-2.5f, -5.5f)
            };

            SpawnZoneType zone = ZoneForBiome(biomeType);
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject pointObject = new GameObject("SpawnPoint_" + (index + 1));
                pointObject.transform.SetParent(parent, false);
                pointObject.transform.localPosition = positions[index];
                SpawnPoint point = pointObject.AddComponent<SpawnPoint>();
                point.SetZoneType(zone);
            }
        }

        private static SpawnZoneType ZoneForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return SpawnZoneType.Desert;
                case BiomeType.Tundra:
                    return SpawnZoneType.Snow;
                case BiomeType.Volcano:
                    return SpawnZoneType.Lava;
                case BiomeType.Ocean:
                    return SpawnZoneType.Water;
                case BiomeType.Space:
                    return SpawnZoneType.Space;
                default:
                    return SpawnZoneType.Forest;
            }
        }
    }
}
