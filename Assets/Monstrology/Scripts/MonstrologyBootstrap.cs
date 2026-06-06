using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    [DefaultExecutionOrder(-1000)]
    public class MonstrologyBootstrap : MonoBehaviour
    {
        [SerializeField] private bool useRuntimeDemoContent = true;
        [SerializeField] private GameContent authoredContent = new GameContent();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForEmptyScene()
        {
            if (FindObjectOfType<GameManager>() != null)
            {
                return;
            }

            GameObject root = new GameObject("Monstrology");
            root.AddComponent<MonstrologyBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            YandexGamesBridge bridge = GetComponent<YandexGamesBridge>();
            if (bridge == null)
            {
                bridge = gameObject.AddComponent<YandexGamesBridge>();
            }

            GameManager game = GetComponent<GameManager>();
            if (game == null)
            {
                game = gameObject.AddComponent<GameManager>();
            }

            GameContent content = useRuntimeDemoContent ? DemoContentFactory.Create() : authoredContent;
            game.Initialize(content);

            ExplorationSystem exploration = GetComponent<ExplorationSystem>();
            if (exploration == null)
            {
                exploration = gameObject.AddComponent<ExplorationSystem>();
            }

            exploration.Initialize(game);

            MutationSystem mutations = GetComponent<MutationSystem>();
            if (mutations == null)
            {
                mutations = gameObject.AddComponent<MutationSystem>();
            }

            mutations.Initialize(game);

            QuestSystem quests = GetComponent<QuestSystem>();
            if (quests == null)
            {
                quests = gameObject.AddComponent<QuestSystem>();
            }

            quests.Initialize(game);

            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            if (player == null)
            {
                player = CreateRuntimePlayer();
            }

            InteractionSystem interaction = player.GetComponent<InteractionSystem>();
            if (interaction == null)
            {
                interaction = player.gameObject.AddComponent<InteractionSystem>();
            }

            interaction.Initialize(player.transform);

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                worldCamera = cameraObject.GetComponent<Camera>();
            }

            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 5f;
            CameraFollow2D cameraFollow = worldCamera.GetComponent<CameraFollow2D>();
            if (cameraFollow == null)
            {
                cameraFollow = worldCamera.gameObject.AddComponent<CameraFollow2D>();
            }

            cameraFollow.SetTarget(player.transform);

            WorldExplorationManager world = GetComponent<WorldExplorationManager>();
            if (world == null)
            {
                world = gameObject.AddComponent<WorldExplorationManager>();
            }

            world.Initialize(game, exploration, player, cameraFollow);

            UIManager ui = GetComponent<UIManager>();
            if (ui == null)
            {
                ui = gameObject.AddComponent<UIManager>();
            }

            ui.Initialize(game, exploration, mutations, quests, player, interaction);
            bridge.LoadProgress();
        }

        private PlayerController2D CreateRuntimePlayer()
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = Vector3.zero;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(playerObject.transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldPlaceholderSprites.Player;
            renderer.color = new Color(0.28f, 0.76f, 0.98f);
            renderer.sortingOrder = 20;

            PlayerController2D player = playerObject.AddComponent<PlayerController2D>();
            player.SetVisual(visualObject.transform);
            return player;
        }
    }

    internal static class DemoContentFactory
    {
        public static GameContent Create()
        {
            GameContent content = new GameContent();
            Dictionary<BiomeType, BiomeData> biomes = CreateBiomes(content);
            Dictionary<string, CreatureData> creatures = CreateCreatures(content);
            CreateItems(content);
            CreateQuests(content);
            CreateMutations(content);

            foreach (CreatureData creature in content.creatures)
            {
                if (creature.id == "fire_wolf" || creature.id == "cosmo_cat")
                {
                    continue;
                }

                biomes[creature.biome].availableCreatures.Add(creature);
            }

            foreach (BiomeData biome in biomes.Values)
            {
                biome.mapData.possibleCreatures.AddRange(biome.availableCreatures);
            }

            return content;
        }

        private static Dictionary<BiomeType, BiomeData> CreateBiomes(GameContent content)
        {
            Dictionary<BiomeType, BiomeData> result = new Dictionary<BiomeType, BiomeData>();
            AddBiome(content, result, BiomeType.Forest, "Лес", 0,
                "Мягкий мох, высокие грибы и шорохи в листве.", new Color(0.12f, 0.34f, 0.24f));
            AddBiome(content, result, BiomeType.Desert, "Пустыня", 60,
                "Горячие дюны скрывают механических и фруктовых существ.", new Color(0.54f, 0.32f, 0.14f));
            AddBiome(content, result, BiomeType.Tundra, "Тундра", 140,
                "Снег хранит следы лучше любого исследовательского журнала.", new Color(0.24f, 0.44f, 0.58f));
            AddBiome(content, result, BiomeType.Volcano, "Вулкан", 250,
                "Здесь камни светятся, а воздух дрожит от жара.", new Color(0.48f, 0.13f, 0.1f));
            AddBiome(content, result, BiomeType.Ocean, "Океан", 360,
                "Подводные лампы, ракушки и загадочные песни глубин.", new Color(0.05f, 0.25f, 0.44f));
            AddBiome(content, result, BiomeType.Space, "Космос", 500,
                "Обломки комет и островки звёздной пыли.", new Color(0.10f, 0.07f, 0.25f));
            return result;
        }

        private static Dictionary<string, CreatureData> CreateCreatures(GameContent content)
        {
            Dictionary<string, CreatureData> result = new Dictionary<string, CreatureData>();
            AddCreature(content, result, "bread_cat", "Хлебокот",
                "Тёплый кот с ароматной корочкой. Любит устраиваться рядом с рюкзаком.",
                CreatureRarity.Common, CreatureElement.Nature, BiomeType.Forest, 1.2f, new Color(0.86f, 0.57f, 0.28f));
            AddCreature(content, result, "tv_horn", "Телевизорог",
                "Его экран показывает погоду, но только вчерашнюю.",
                CreatureRarity.Rare, CreatureElement.Electric, BiomeType.Forest, 0.72f, new Color(0.37f, 0.72f, 0.83f));
            AddCreature(content, result, "mushroom_fox", "Гриболис",
                "Тихий лесной хитрец. На его шляпках светятся споры.",
                CreatureRarity.Common, CreatureElement.Nature, BiomeType.Forest, 1.05f, new Color(0.65f, 0.31f, 0.38f));
            AddCreature(content, result, "kitten", "Котёнок",
                "Любопытный путешественник, готовый к удивительным превращениям.",
                CreatureRarity.Common, CreatureElement.Nature, BiomeType.Forest, 0.9f, new Color(0.84f, 0.68f, 0.48f));
            AddCreature(content, result, "wolf_cub", "Волчонок",
                "Храбрый малыш. Кажется, внутри него дремлет горячая стихия.",
                CreatureRarity.Rare, CreatureElement.Nature, BiomeType.Forest, 0.55f, new Color(0.48f, 0.58f, 0.54f));

            AddCreature(content, result, "vacuum_rhino", "Пылесосорог",
                "Собирает песок хоботом и оставляет за собой идеально ровную дорожку.",
                CreatureRarity.Rare, CreatureElement.Sand, BiomeType.Desert, 0.78f, new Color(0.72f, 0.58f, 0.34f));
            CreatureData watermelon = AddCreature(content, result, "watermelon_saur", "Арбузавр",
                "Полосатый великан, который выращивает прохладные ломтики на спине.",
                CreatureRarity.Epic, CreatureElement.Nature, BiomeType.Desert, 0.52f, new Color(0.25f, 0.68f, 0.33f));
            watermelon.appearanceConditions.Add(ExplorationCondition(5));

            AddCreature(content, result, "ice_saur", "Ледозавр",
                "Его прозрачный гребень звенит, когда начинается снег.",
                CreatureRarity.Rare, CreatureElement.Ice, BiomeType.Tundra, 0.85f, new Color(0.48f, 0.82f, 0.96f));

            AddCreature(content, result, "magma_orb", "Магмошар",
                "Круглый обитатель лавовых троп. Остывает, когда смущается.",
                CreatureRarity.Common, CreatureElement.Fire, BiomeType.Volcano, 1.0f, new Color(0.95f, 0.32f, 0.12f));
            AddCreature(content, result, "fire_wolf", "Огненный Волчонок",
                "Новая форма Волчонка. Его хвост освещает путь и не обжигает друзей.",
                CreatureRarity.Epic, CreatureElement.Fire, BiomeType.Volcano, 0.05f, new Color(1f, 0.43f, 0.12f));

            AddCreature(content, result, "lamp_crab", "Лампакраб",
                "Подсвечивает морское дно и собирает вокруг себя стаи маленьких рыб.",
                CreatureRarity.Rare, CreatureElement.Water, BiomeType.Ocean, 0.82f, new Color(0.24f, 0.86f, 0.82f));

            AddCreature(content, result, "astro_fox", "Астролис",
                "Прыгает между маленькими астероидами и прячет звёзды в хвосте.",
                CreatureRarity.Legendary, CreatureElement.Space, BiomeType.Space, 0.65f, new Color(0.58f, 0.42f, 0.94f));
            AddCreature(content, result, "cosmo_cat", "Космокот",
                "Мурлычет на частоте далёких спутников и оставляет невесомые следы.",
                CreatureRarity.Epic, CreatureElement.Space, BiomeType.Space, 0.05f, new Color(0.42f, 0.48f, 0.98f));

            CreatureData dragon = AddCreature(content, result, "mushroom_dragon", "Грибной Дракон",
                "Секретный хранитель леса. Его крылья распускаются после долгих исследований.",
                CreatureRarity.Secret, CreatureElement.Mystery, BiomeType.Forest, 0.8f, new Color(0.73f, 0.28f, 0.82f));
            dragon.appearanceConditions.Add(ElementCondition(CreatureElement.Nature, 3));
            dragon.appearanceConditions.Add(ItemCondition("glowing_mushroom"));
            dragon.appearanceConditions.Add(ExplorationCondition(15));
            return result;
        }

        private static void CreateItems(GameContent content)
        {
            AddItem(content, "fire_stone", "Огненный камень",
                "Тёплый катализатор для мутаций.", ItemKind.MutationCatalyst, BiomeType.Volcano,
                new Color(1f, 0.35f, 0.12f));
            AddItem(content, "space_dust", "Космическая пыль",
                "Искрится даже в закрытой банке.", ItemKind.MutationCatalyst, BiomeType.Space,
                new Color(0.55f, 0.45f, 1f));
            AddItem(content, "glowing_mushroom", "Светящийся гриб",
                "Редкий лесной образец. Может привлечь тайного хранителя.", ItemKind.Material, BiomeType.Forest,
                new Color(0.78f, 0.34f, 0.85f));
            AddItem(content, "pearl_shell", "Жемчужная ракушка",
                "Тихо повторяет звуки океана.", ItemKind.Material, BiomeType.Ocean,
                new Color(0.44f, 0.85f, 0.92f));
            AddItem(content, "mystery_egg", "Загадочное яйцо",
                "Пока не вылупляется, но отлично дополняет коллекцию.", ItemKind.Egg, BiomeType.Tundra,
                new Color(0.86f, 0.72f, 0.95f));
        }

        private static void CreateQuests(GameContent content)
        {
            AddQuest(content, "first_explore", "Первые шаги", "Сделайте первое исследование.",
                QuestGoalType.CompleteExplorations, 1, BiomeType.Forest, 20, 3);
            AddQuest(content, "first_creature", "Первая встреча", "Найдите первое существо.",
                QuestGoalType.FindAnyCreature, 1, BiomeType.Forest, 30, 2);
            AddQuest(content, "unlock_desert", "Горизонт расширяется", "Откройте Пустыню.",
                QuestGoalType.UnlockBiome, 1, BiomeType.Desert, 55, 4);
            AddQuest(content, "ten_creatures", "Настоящая коллекция", "Найдите 10 существ, повторы считаются.",
                QuestGoalType.FindCreatureCopies, 10, BiomeType.Forest, 90, 6);
            AddQuest(content, "first_mutation", "Научный прорыв", "Проведите первую успешную мутацию.",
                QuestGoalType.CompleteMutations, 1, BiomeType.Forest, 100, 5);
            AddQuest(content, "find_secret", "За гранью справочника", "Найдите секретное существо.",
                QuestGoalType.FindSecretCreature, 1, BiomeType.Forest, 180, 10);
        }

        private static void CreateMutations(GameContent content)
        {
            content.mutationRecipes.Add(new MutationRecipe
            {
                baseCreatureId = "wolf_cub",
                itemId = "fire_stone",
                resultCreatureId = "fire_wolf"
            });
            content.mutationRecipes.Add(new MutationRecipe
            {
                baseCreatureId = "kitten",
                itemId = "space_dust",
                resultCreatureId = "cosmo_cat"
            });
        }

        private static void AddBiome(GameContent content, IDictionary<BiomeType, BiomeData> target,
            BiomeType type, string title, int price, string description, Color color)
        {
            BiomeData biome = ScriptableObject.CreateInstance<BiomeData>();
            biome.hideFlags = HideFlags.DontSave;
            biome.type = type;
            biome.biomeName = title;
            biome.unlockPrice = price;
            biome.description = description;
            biome.fallbackColor = color;
            biome.mapData = ScriptableObject.CreateInstance<BiomeMapData>();
            biome.mapData.hideFlags = HideFlags.DontSave;
            biome.mapData.biomeId = type.ToString();
            biome.mapData.biomeType = type;
            biome.mapData.backgroundColor = color;
            biome.mapData.lightingColor = Color.Lerp(Color.white, color, 0.12f);
            biome.mapData.worldSize = new Vector2(30f, 18f);
            biome.mapData.playerStart = Vector2.zero;
            biome.mapData.activeFindings = 7;
            biome.mapData.respawnInterval = 7f;
            content.biomes.Add(biome);
            target.Add(type, biome);
        }

        private static CreatureData AddCreature(GameContent content, IDictionary<string, CreatureData> target,
            string id, string title, string description, CreatureRarity rarity, CreatureElement element,
            BiomeType biome, float chance, Color color)
        {
            CreatureData creature = ScriptableObject.CreateInstance<CreatureData>();
            creature.hideFlags = HideFlags.DontSave;
            creature.id = id;
            creature.creatureName = title;
            creature.description = description;
            creature.rarity = rarity;
            creature.element = element;
            creature.biome = biome;
            creature.appearanceChance = chance;
            creature.icon = RuntimeIconFactory.CreateCreatureIcon(color, StableVariant(id));
            content.creatures.Add(creature);
            target.Add(id, creature);
            return creature;
        }

        private static void AddItem(GameContent content, string id, string title, string description,
            ItemKind kind, BiomeType biome, Color color)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.hideFlags = HideFlags.DontSave;
            item.id = id;
            item.itemName = title;
            item.description = description;
            item.kind = kind;
            item.preferredBiome = biome;
            item.icon = RuntimeIconFactory.CreateItemIcon(color);
            content.items.Add(item);
        }

        private static void AddQuest(GameContent content, string id, string title, string description,
            QuestGoalType type, int amount, BiomeType biome, int coins, int energy)
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.hideFlags = HideFlags.DontSave;
            quest.id = id;
            quest.questName = title;
            quest.description = description;
            quest.goalType = type;
            quest.requiredAmount = amount;
            quest.requiredBiome = biome;
            quest.rewardCoins = coins;
            quest.rewardEnergy = energy;
            content.quests.Add(quest);
        }

        private static AppearanceCondition ExplorationCondition(int count)
        {
            return new AppearanceCondition
            {
                type = AppearanceConditionType.ExplorationCount,
                requiredCount = count
            };
        }

        private static AppearanceCondition ItemCondition(string itemId)
        {
            return new AppearanceCondition
            {
                type = AppearanceConditionType.HasItem,
                requiredItemId = itemId
            };
        }

        private static AppearanceCondition ElementCondition(CreatureElement element, int count)
        {
            return new AppearanceCondition
            {
                type = AppearanceConditionType.FoundCreaturesOfElement,
                requiredElement = element,
                requiredCount = count
            };
        }

        private static int StableVariant(string value)
        {
            int result = 0;
            for (int index = 0; index < value.Length; index++)
            {
                result = (result * 31 + value[index]) & 0x7fffffff;
            }

            return result % 3;
        }
    }

    internal static class RuntimeIconFactory
    {
        public static Sprite CreateCreatureIcon(Color bodyColor, int variant)
        {
            const int size = 96;
            Texture2D texture = NewTexture(size);
            Color outline = new Color(
                Mathf.Max(0f, bodyColor.r - 0.25f),
                Mathf.Max(0f, bodyColor.g - 0.25f),
                Mathf.Max(0f, bodyColor.b - 0.25f),
                1f);

            DrawCircle(texture, 48, 53, 31, outline);
            DrawCircle(texture, 48, 53, 27, bodyColor);
            if (variant == 0)
            {
                DrawTriangle(texture, 19, 20, 42, outline);
                DrawTriangle(texture, 77, 20, 42, outline);
            }
            else if (variant == 1)
            {
                DrawCircle(texture, 24, 29, 12, outline);
                DrawCircle(texture, 72, 29, 12, outline);
            }
            else
            {
                DrawCircle(texture, 48, 20, 13, outline);
            }

            DrawCircle(texture, 37, 52, 5, Color.white);
            DrawCircle(texture, 59, 52, 5, Color.white);
            DrawCircle(texture, 38, 52, 2, new Color(0.04f, 0.05f, 0.07f));
            DrawCircle(texture, 60, 52, 2, new Color(0.04f, 0.05f, 0.07f));
            DrawCircle(texture, 48, 65, 4, outline);
            texture.Apply();
            return CreateSprite(texture);
        }

        public static Sprite CreateItemIcon(Color color)
        {
            const int size = 96;
            Texture2D texture = NewTexture(size);
            Color outline = new Color(
                Mathf.Max(0f, color.r - 0.28f),
                Mathf.Max(0f, color.g - 0.28f),
                Mathf.Max(0f, color.b - 0.28f),
                1f);
            DrawDiamond(texture, 48, 48, 34, outline);
            DrawDiamond(texture, 48, 48, 29, color);
            DrawCircle(texture, 39, 38, 7, new Color(1f, 1f, 1f, 0.58f));
            texture.Apply();
            return CreateSprite(texture);
        }

        private static Texture2D NewTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[size * size];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = clear;
            }

            texture.SetPixels(pixels);
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSquared)
                    {
                        SetPixel(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawDiamond(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int width = radius - Mathf.Abs(y);
                for (int x = -width; x <= width; x++)
                {
                    SetPixel(texture, centerX + x, centerY + y, color);
                }
            }
        }

        private static void DrawTriangle(Texture2D texture, int centerX, int minY, int maxY, Color color)
        {
            int height = maxY - minY;
            for (int y = minY; y <= maxY; y++)
            {
                int halfWidth = (maxY - y) * 13 / height;
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    SetPixel(texture, centerX + x, y, color);
                }
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
