using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class WorldExplorationManager : MonoBehaviour
    {
        public const int ExplorerCompassEnergyCost = 3;
        public const float TraceDropChance = 0.35f;
        public const float ResourceDropChance = 0.25f;
        public const float AccessoryDropChance = 0.10f;
        public const float CreatureDropChance = 0.30f;
        public static bool ReleaseDropChancesAreValid
        {
            get
            {
                return Mathf.Abs(
                    TraceDropChance + ResourceDropChance +
                    AccessoryDropChance + CreatureDropChance - 1f) < 0.001f;
            }
        }

        [SerializeField] private List<BiomeMapData> mapCatalog = new List<BiomeMapData>();
        [SerializeField] private Transform worldRoot;
        [SerializeField] private bool premiumCompassEnabled;

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
        private InfiniteBiomeMap infiniteMap;
        private WorldNavigationGuide navigationGuide;
        private TrackChainSystem trackChains;
        private CreatureNestSystem creatureNests;
        private BiomeEventSystem biomeEvents;
        private float respawnTimer;
        private float maintenanceTimer;
        private bool switchingMap;

        public BiomeData LoadedBiome { get { return loadedBiome; } }
        public BiomeMapData CurrentMapData { get { return currentMapData; } }
        public int ActivePickupCount { get { return activePickups.Count; } }
        public int PersistentNestCount
        {
            get
            {
                int count = 0;
                foreach (WorldPickup pickup in activePickups)
                {
                    if (pickup != null &&
                        pickup.PickupType == WorldPickupType.CreatureNest &&
                        pickup.IsPersistent)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public InfiniteBiomeMap InfiniteMap { get { return infiniteMap; } }
        public WorldNavigationGuide NavigationGuide { get { return navigationGuide; } }
        public bool PremiumCompassEnabled { get { return premiumCompassEnabled; } }

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

            navigationGuide = GetComponentInChildren<WorldNavigationGuide>(true);
            if (navigationGuide == null)
            {
                GameObject guideObject = new GameObject("ExplorerCompassGuide");
                guideObject.transform.SetParent(transform, false);
                navigationGuide = guideObject.AddComponent<WorldNavigationGuide>();
            }

            navigationGuide.Initialize(player != null ? player.transform : null);

            trackChains = GetComponent<TrackChainSystem>();
            if (trackChains == null)
            {
                trackChains = gameObject.AddComponent<TrackChainSystem>();
            }

            trackChains.Initialize(this);
            creatureNests = FindObjectOfType<CreatureNestSystem>();
            biomeEvents = FindObjectOfType<BiomeEventSystem>();
            if (biomeEvents != null)
            {
                biomeEvents.EventStarted -= SpawnSpecialEvent;
                biomeEvents.EventStarted += SpawnSpecialEvent;
            }

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

            if (biomeEvents != null)
            {
                biomeEvents.EventStarted -= SpawnSpecialEvent;
            }
        }

        private void Update()
        {
            if (currentMap == null || currentMapData == null || switchingMap)
            {
                return;
            }

            activePickups.RemoveWhere(pickup => pickup == null);
            maintenanceTimer -= Time.deltaTime;
            if (maintenanceTimer <= 0f)
            {
                RefreshSpawnPointsNearPlayer();
                RecycleDistantPickups();
                MaintainPremiumCompass();
                maintenanceTimer = 1f;
            }

            int targetCount = Mathf.Max(0, currentMapData.activeFindings);
            if (GetRegularPickupCount() >= targetCount)
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

            if (navigationGuide != null)
            {
                navigationGuide.ClearTarget(pickup);
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
                return;
            }

            ApplyEnvironmentVisuals();
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
            DisableMapBoundaries(currentMap);
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
            infiniteMap = null;
            if (navigationGuide != null)
            {
                navigationGuide.ClearTarget();
            }

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

            CreateFallbackDecorations(map.transform, biome.type, data.backgroundColor);
            CreateFallbackSpawnPoints(map.transform, biome.type);
            return map;
        }

        private void AddDataBackground(GameObject map, BiomeMapData data)
        {
            if (map == null || data == null)
            {
                return;
            }

            Sprite sprite = SpriteDatabase.Active.GetBiomeBackground(loadedBiome, data);
            Color color = SpriteDatabase.Active.HasBiomeArtwork(loadedBiome, data)
                ? Color.white
                : data.backgroundColor;
            Transform existing = map.transform.Find("Background");
            if (existing != null)
            {
                SpriteRenderer existingRenderer = existing.GetComponent<SpriteRenderer>();
                if (existingRenderer != null)
                {
                    sprite = existingRenderer.sprite != null ? existingRenderer.sprite : sprite;
                    color = existingRenderer.color;
                    existingRenderer.enabled = false;
                }
            }

            infiniteMap = map.GetComponent<InfiniteBiomeMap>();
            if (infiniteMap == null)
            {
                infiniteMap = map.AddComponent<InfiniteBiomeMap>();
            }

            infiniteMap.Initialize(
                sprite,
                color,
                data.worldSize,
                player != null ? player.transform : null);
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
                cameraFollow.ClearBounds();
            }

            ApplyEnvironmentVisuals();
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
            RefreshSpawnPointsNearPlayer();
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

            SpawnDiscoveredNestMarkers();
            respawnTimer = Mathf.Max(0.25f, currentMapData.respawnInterval);
        }

        private bool SpawnOnePickup()
        {
            if (exploration == null || spawnPoints.Count == 0)
            {
                return false;
            }

            RefreshSpawnPointsNearPlayer();
            List<SpawnPoint> available = spawnPoints.FindAll(point => point != null && !point.IsOccupied);
            if (available.Count == 0)
            {
                return false;
            }

            SpawnPoint spawnPoint = available[Random.Range(0, available.Count)];
            WorldPickupType type = RollPickupType();
            CreatureData creature = null;
            ItemData item = null;
            AccessoryData accessory = null;
            TrackType track = exploration.SelectTrack();

            if (type == WorldPickupType.Creature)
            {
                IList<CreatureData> creatures = currentMapData.possibleCreatures.Count > 0
                    ? currentMapData.possibleCreatures
                    : loadedBiome.availableCreatures;
                creature = exploration.SelectWorldCreature(creatures);
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
            else if (type == WorldPickupType.Accessory)
            {
                accessory = exploration.SelectAccessory();
                if (accessory == null)
                {
                    type = WorldPickupType.Trace;
                }
            }

            if (type == WorldPickupType.Trace)
            {
                track = TrackType.Paws;
            }

            int chainStage = type == WorldPickupType.Trace ? 0 : -1;
            return CreatePickup(
                       spawnPoint.GetSpawnPosition(),
                       type,
                       creature,
                       item,
                       track,
                       accessory,
                       spawnPoint,
                       chainStage) != null;
        }

        public bool StartQuickSearch()
        {
            if (player == null || navigationGuide == null || game == null)
            {
                return false;
            }

            if (navigationGuide.HasTarget)
            {
                game.RaiseNotification("Компас уже указывает на находку.");
                return false;
            }

            WorldPickup nearest = FindNearestInterestingPickup();
            if (nearest == null)
            {
                SpawnOnePickup();
                nearest = FindNearestInterestingPickup();
            }

            if (nearest == null)
            {
                game.RaiseNotification("Компас пока не нашёл интересную цель.");
                return false;
            }

            if (!premiumCompassEnabled && !game.SpendEnergy(ExplorerCompassEnergyCost))
            {
                return false;
            }

            navigationGuide.SetTarget(nearest);
            game.RaiseNotification(premiumCompassEnabled
                ? "Премиум-компас выбрал новую цель."
                : "Компас активирован за 3 энергии. Доберитесь до цели.");
            return true;
        }

        public void SetPremiumCompassEnabled(bool enabled)
        {
            premiumCompassEnabled = enabled;
            if (premiumCompassEnabled)
            {
                MaintainPremiumCompass();
            }
            else if (navigationGuide != null)
            {
                navigationGuide.ClearTarget();
            }
        }

        public void AdvanceTrackChain(WorldPickup trace)
        {
            if (trackChains != null)
            {
                trackChains.Advance(trace);
            }
        }

        public void SpawnTrackStep(Vector3 position, TrackType trackType, int stage)
        {
            CreatePickup(
                position,
                WorldPickupType.Trace,
                null,
                null,
                trackType,
                null,
                null,
                stage);

            game.RaiseNotification(stage >= 4
                ? "Следы привели к логову."
                : "Цепочка следов продолжается.");
        }

        public void SpawnTrackedCreature(Vector3 position)
        {
            IList<CreatureData> source = loadedBiome.availableCreatures;
            List<CreatureData> available = new List<CreatureData>();
            List<CreatureData> rare = new List<CreatureData>();
            foreach (CreatureData creature in source)
            {
                if (creature != null &&
                    creature.appearanceChance > 0.001f &&
                    game.AreAppearanceConditionsMet(creature))
                {
                    available.Add(creature);
                    if (creature.rarity != CreatureRarity.Common)
                    {
                        rare.Add(creature);
                    }
                }
            }

            List<CreatureData> nestCandidates = available.FindAll(creature =>
                game.Content.creatureNests.Exists(nest =>
                    nest != null && nest.speciesId == creature.id &&
                    (creatureNests == null || creatureNests.GetNest(nest.id) == null)));
            CreatureData selected = exploration.SelectCreature(
                nestCandidates.Count > 0
                    ? nestCandidates
                    : rare.Count > 0
                        ? rare
                        : available);
            if (selected == null)
            {
                game.RaiseNotification("След оборвался: условия появления существа изменились.");
                return;
            }

            CreatureNestData nestData = game.Content.creatureNests.Find(nest =>
                nest != null && nest.speciesId == selected.id);
            if (nestData == null)
            {
                CreatePickup(
                    position,
                    WorldPickupType.Creature,
                    selected,
                    null,
                    exploration.SelectTrack(),
                    null,
                    null,
                    -1);
                game.RaiseNotification("Следы привели к " + selected.creatureName + ".");
                return;
            }

            WorldPickup nestPickup = CreatePickup(
                position,
                WorldPickupType.CreatureNest,
                selected,
                null,
                exploration.SelectTrack(),
                null,
                null,
                -1);
            if (nestPickup != null)
            {
                nestPickup.ConfigureNestMarker(nestData.id, false);
            }

            Vector2 nearbyOffset = UnityEngine.Random.insideUnitCircle.normalized * 2.2f;
            CreatePickup(
                position + (Vector3)nearbyOffset,
                WorldPickupType.Creature,
                selected,
                null,
                exploration.SelectTrack(),
                null,
                null,
                -1);

            game.RaiseNotification("Следы привели к логовищу " + selected.creatureName + ".");
        }

        public void ResolveCreatureNest(WorldPickup pickup)
        {
            if (pickup == null || pickup.Creature == null || creatureNests == null)
            {
                return;
            }

            CreatureNestProgress progress;
            bool discovered = creatureNests.Discover(pickup.Creature.id, out progress);
            if (progress == null)
            {
                exploration.ResolveWorldDiscovery(
                    ExplorationResultType.Creature,
                    pickup.Creature);
                return;
            }

            CreatureNestData data = creatureNests.GetNestData(progress.nestId);
            pickup.ConfigureNestMarker(progress.nestId, true);
            if (navigationGuide != null)
            {
                navigationGuide.ClearTarget(pickup);
            }

            if (discovered)
            {
                game.AddTrack(TrackType.Lair);
            }

            exploration.PublishExternalResult(new ExplorationResult
            {
                type = ExplorationResultType.CreatureNest,
                title = discovered
                    ? "Новое логовище: " + pickup.Creature.creatureName
                    : data != null ? data.displayName : "Знакомое логовище",
                description = (discovered
                                  ? "Логовище открыто и теперь всегда отмечено в этом биоме."
                                  : "Вы вернулись к знакомому логовищу.") +
                              "\nОсмотрите его, когда награда будет готова.",
                icon = SpriteDatabase.Active.GetNest(data),
                accentColor = new Color(0.82f, 0.58f, 0.3f),
                creature = pickup.Creature
            }, discovered);

            UIManager ui = FindObjectOfType<UIManager>();
            if (ui != null)
            {
                ui.OpenNest(progress);
            }
        }

        public void ResolveSpecialMapEvent(WorldPickup pickup)
        {
            if (pickup == null || pickup.BiomeEvent == null)
            {
                return;
            }

            ItemData reward = exploration.SelectItem(false);
            if (reward != null)
            {
                game.AddItem(reward.id, 2);
            }

            CreatureData rare = SelectRareCreature();
            if (rare != null)
            {
                game.AddCreatureCopies(rare.id, 1);
            }

            exploration.PublishExternalResult(new ExplorationResult
            {
                type = ExplorationResultType.SpecialEvent,
                title = pickup.BiomeEvent.specialObjectName,
                description = "Редкая находка события «" + pickup.BiomeEvent.displayName + "»." +
                              (reward != null ? "\nПолучено: 2 × " + reward.itemName : "") +
                              (rare != null ? "\nКопия вида: " + rare.creatureName : ""),
                icon = SpriteDatabase.Active.GetSpecialEvent(pickup.BiomeEvent),
                accentColor = new Color(0.95f, 0.42f, 0.82f)
            });
        }

        public void SpawnFavoriteHint(bool preferNest)
        {
            if (player == null || currentMap == null)
            {
                return;
            }

            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 position = player.transform.position + (Vector3)(direction * 5f);
            if (preferNest && creatureNests != null)
            {
                CreatureNestData nest = game.Content.creatureNests.Find(data =>
                    data != null && loadedBiome != null && data.biome == loadedBiome.type &&
                    creatureNests.GetNest(data.id) == null);
                CreatureData creature = nest != null ? game.GetCreature(nest.speciesId) : null;
                if (creature != null)
                {
                    CreatePickup(position, WorldPickupType.CreatureNest, creature, null,
                        TrackType.Lair, null, null, -1);
                    game.RaiseNotification("Любимчик указывает на возможное логовище.");
                    return;
                }
            }

            CreatePickup(position, WorldPickupType.Trace, null, null,
                TrackType.Paws, null, null, 0);
            game.RaiseNotification("Любимчик заметил след неподалёку.");
        }

        private void SpawnSpecialEvent(BiomeEventData eventData)
        {
            if (eventData == null || player == null || currentMap == null ||
                loadedBiome == null || eventData.biome != loadedBiome.type)
            {
                return;
            }

            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 position = player.transform.position + (Vector3)(direction * 6.5f);
            CreatePickup(
                position,
                WorldPickupType.SpecialEvent,
                null,
                null,
                TrackType.ShinyStone,
                null,
                null,
                -1,
                eventData,
                eventData.durationSeconds);
        }

        private CreatureData SelectRareCreature()
        {
            if (loadedBiome == null)
            {
                return null;
            }

            List<CreatureData> rare = loadedBiome.availableCreatures.FindAll(creature =>
                creature != null &&
                creature.appearanceChance > 0.001f &&
                creature.rarity != CreatureRarity.Common &&
                game.AreAppearanceConditionsMet(creature));
            return rare.Count > 0 ? rare[UnityEngine.Random.Range(0, rare.Count)] : null;
        }

        private void MaintainPremiumCompass()
        {
            if (!premiumCompassEnabled || navigationGuide == null ||
                navigationGuide.HasTarget || player == null)
            {
                return;
            }

            WorldPickup target = FindNearestInterestingPickup();
            if (target != null)
            {
                navigationGuide.SetTarget(target);
            }
        }

        private WorldPickup CreatePickup(
            Vector3 position,
            WorldPickupType type,
            CreatureData creature,
            ItemData item,
            TrackType track,
            AccessoryData accessory,
            SpawnPoint spawnPoint,
            int chainStage,
            BiomeEventData eventData = null,
            float lifetimeSeconds = 0f)
        {
            if (currentMap == null)
            {
                return null;
            }

            GameObject pickupObject = new GameObject("Finding_" + type);
            pickupObject.transform.SetParent(currentMap.transform, true);
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = Vector3.one * 1.05f;
            CircleCollider2D pickupCollider = pickupObject.AddComponent<CircleCollider2D>();
            pickupCollider.radius = 0.48f;
            WorldPickup pickup = pickupObject.AddComponent<WorldPickup>();
            pickup.Initialize(
                type,
                creature,
                item,
                track,
                exploration,
                this,
                spawnPoint,
                accessory,
                chainStage,
                eventData,
                lifetimeSeconds);
            activePickups.Add(pickup);
            return pickup;
        }

        private WorldPickup FindNearestInterestingPickup()
        {
            WorldPickup nearest = null;
            float nearestDistance = float.MaxValue;
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            foreach (WorldPickup pickup in activePickups)
            {
                if (pickup == null || !pickup.CanInteract ||
                    pickup.PickupType == WorldPickupType.Nothing)
                {
                    continue;
                }

                if (pickup.IsPersistent &&
                    pickup.PickupType == WorldPickupType.CreatureNest &&
                    creatureNests != null)
                {
                    CreatureNestProgress progress =
                        creatureNests.GetNest(pickup.NestId);
                    if (progress != null && !creatureNests.IsRewardReady(progress))
                    {
                        continue;
                    }
                }

                float distance = (pickup.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = pickup;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void RefreshSpawnPointsNearPlayer()
        {
            if (player == null || spawnPoints.Count == 0)
            {
                return;
            }

            Vector3 center = player.transform.position;
            for (int index = 0; index < spawnPoints.Count; index++)
            {
                SpawnPoint point = spawnPoints[index];
                if (point == null || point.IsOccupied)
                {
                    continue;
                }

                float angle = index * Mathf.PI * 2f / spawnPoints.Count;
                float radius = 7f + index % 4 * 1.8f;
                point.transform.position = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
            }
        }

        private void RecycleDistantPickups()
        {
            if (player == null || currentMapData == null)
            {
                return;
            }

            float maxDistance = Mathf.Max(currentMapData.worldSize.x, currentMapData.worldSize.y) * 1.75f;
            float maxDistanceSquared = maxDistance * maxDistance;
            List<WorldPickup> distant = new List<WorldPickup>();
            foreach (WorldPickup pickup in activePickups)
            {
                if (pickup == null ||
                    pickup.IsPersistent ||
                    (navigationGuide != null && navigationGuide.Target == pickup))
                {
                    continue;
                }

                if ((pickup.transform.position - player.transform.position).sqrMagnitude >
                    maxDistanceSquared)
                {
                    distant.Add(pickup);
                }
            }

            foreach (WorldPickup pickup in distant)
            {
                activePickups.Remove(pickup);
                Destroy(pickup.gameObject);
            }
        }

        private void DisableMapBoundaries(GameObject map)
        {
            if (map == null)
            {
                return;
            }

            foreach (Collider2D collider in map.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider != null && collider.name.StartsWith("Boundary"))
                {
                    collider.enabled = false;
                }
            }
        }

        private void ApplyEnvironmentVisuals()
        {
            if (worldCamera == null || currentMapData == null || game == null)
            {
                return;
            }

            Color timeTint;
            switch (game.CurrentTimeOfDay)
            {
                case TimeOfDay.Morning:
                    timeTint = new Color(1f, 0.88f, 0.72f);
                    break;
                case TimeOfDay.Evening:
                    timeTint = new Color(0.88f, 0.62f, 0.5f);
                    break;
                case TimeOfDay.Night:
                    timeTint = new Color(0.35f, 0.42f, 0.68f);
                    break;
                default:
                    timeTint = Color.white;
                    break;
            }

            Color weatherTint = game.CurrentWeather == WeatherType.Fog
                ? new Color(0.72f, 0.76f, 0.78f)
                : game.CurrentWeather == WeatherType.Rain
                    ? new Color(0.62f, 0.72f, 0.82f)
                    : game.CurrentWeather == WeatherType.MeteorShower
                        ? new Color(0.65f, 0.5f, 0.86f)
                        : Color.white;
            Color baseColor = currentMapData.backgroundColor * currentMapData.lightingColor;
            worldCamera.backgroundColor = baseColor * timeTint * weatherTint;
            worldCamera.backgroundColor = new Color(
                worldCamera.backgroundColor.r,
                worldCamera.backgroundColor.g,
                worldCamera.backgroundColor.b,
                1f);
            RenderSettings.ambientLight = currentMapData.lightingColor * timeTint * weatherTint;
        }

        private static WorldPickupType RollPickupType()
        {
            float roll = Random.value;
            if (roll < TraceDropChance)
            {
                return WorldPickupType.Trace;
            }

            if (roll < TraceDropChance + ResourceDropChance - 0.02f)
            {
                return WorldPickupType.Item;
            }

            if (roll < TraceDropChance + ResourceDropChance)
            {
                return WorldPickupType.Egg;
            }

            if (roll < TraceDropChance + ResourceDropChance + AccessoryDropChance)
            {
                return WorldPickupType.Accessory;
            }

            return WorldPickupType.Creature;
        }

        private int GetRegularPickupCount()
        {
            int count = 0;
            foreach (WorldPickup pickup in activePickups)
            {
                if (pickup != null && !pickup.IsPersistent)
                {
                    count++;
                }
            }

            return count;
        }

        private void SpawnDiscoveredNestMarkers()
        {
            if (creatureNests == null || loadedBiome == null || player == null)
            {
                return;
            }

            int index = 0;
            foreach (CreatureNestProgress progress in creatureNests.GetAllNests())
            {
                CreatureNestData data = progress != null
                    ? creatureNests.GetNestData(progress.nestId)
                    : null;
                if (data == null || data.biome != loadedBiome.type ||
                    FindNestMarker(data.id) != null)
                {
                    continue;
                }

                CreatureData creature = game.GetCreature(data.speciesId);
                if (creature == null)
                {
                    continue;
                }

                uint hash = StableHash(data.id);
                float angle = (hash % 360u) * Mathf.Deg2Rad;
                float radius = 7.5f + hash % 5u;
                Vector3 position = player.transform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
                position += new Vector3(index * 0.65f, index * 0.35f, 0f);
                index++;

                WorldPickup marker = CreatePickup(
                    position,
                    WorldPickupType.CreatureNest,
                    creature,
                    null,
                    TrackType.Lair,
                    null,
                    null,
                    -1);
                if (marker != null)
                {
                    marker.ConfigureNestMarker(data.id, true);
                }
            }
        }

        private WorldPickup FindNestMarker(string nestId)
        {
            foreach (WorldPickup pickup in activePickups)
            {
                if (pickup != null &&
                    pickup.PickupType == WorldPickupType.CreatureNest &&
                    pickup.NestId == nestId)
                {
                    return pickup;
                }
            }

            return null;
        }

        private static uint StableHash(string value)
        {
            uint hash = 2166136261u;
            string source = value ?? string.Empty;
            for (int index = 0; index < source.Length; index++)
            {
                hash ^= source[index];
                hash *= 16777619u;
            }

            return hash;
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

        private static void CreateFallbackDecorations(
            Transform parent,
            BiomeType biome,
            Color biomeColor)
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
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(decoration.transform, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = SpriteDatabase.Active.GetBiomeDecoration(
                    biome,
                    index % 2 != 0);
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
