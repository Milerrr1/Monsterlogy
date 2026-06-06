# 2D exploration: настройка сцены и карт

## Что уже работает автоматически

`MonstrologyBootstrap` сохраняет старый UI-прототип и при запуске дополнительно создаёт:

- `Player` с `Rigidbody2D`, `CircleCollider2D`, `PlayerController2D` и `InteractionSystem`;
- `WorldExplorationManager`;
- `CameraFollow2D` на `Main Camera`;
- активную карту текущего биома;
- placeholder-находки и точки спавна;
- экранный joystick и кнопку `Взаимодействовать`.

Если у `BiomeData` не назначен `BiomeMapData`, игра создаёт лёгкую одноцветную runtime-карту. Поэтому старые данные не требуют обязательной миграции.

## Новые скрипты

- `PlayerController2D.cs` — WASD, стрелки, Rigidbody2D, направление, мобильный input и `EnableMovement`.
- `VirtualJoystick.cs` — экранный joystick на стандартном Unity UI/EventSystem.
- `WorldExplorationManager.cs` — переключение карт, спавн и респавн находок.
- `WorldPickup.cs` — данные и получение конкретной находки.
- `InteractionSystem.cs` — ближайшая находка, клавиша E и UI-кнопка.
- `BiomeMapData.cs` — настройки карты биома.
- `SpawnPoint.cs` — точка и тип зоны спавна.
- `CameraFollow2D.cs` — плавное слежение и границы карты.

## Создание BiomeMapData

Для каждого биома создайте asset через:

`Create > Monstrology > Biome Map`

Рекомендуемые имена:

- `ForestMapData`
- `DesertMapData`
- `TundraMapData`
- `VolcanoMapData`
- `OceanMapData`
- `SpaceMapData`

Заполните:

- `Biome Id` — стабильный ID, например `Forest`;
- `Biome Type` — соответствующий `BiomeType`;
- `Map Prefab` — prefab карты;
- `Background` — необязательный sprite фона;
- `Background Color` и `Lighting Color`;
- `World Size` — реальные размеры игровой области в world units;
- `Player Start` — старт игрока относительно корня prefab;
- `Active Findings` — сколько находок одновременно держать на карте;
- `Respawn Interval` — задержка респавна после подбора;
- `Possible Creatures` — необязательный override списка существ.

Затем откройте соответствующий `BiomeData` и назначьте asset в поле `Map Data`.

Если `Possible Creatures` пуст, используются `BiomeData.Available Creatures`.

## Prefab карты

Создайте шесть prefab:

- `ForestMapPrefab`
- `DesertMapPrefab`
- `TundraMapPrefab`
- `VolcanoMapPrefab`
- `OceanMapPrefab`
- `SpaceMapPrefab`

Минимальная рекомендуемая иерархия:

```text
ForestMapPrefab
├── Background
├── Decorations
│   ├── Tree_01
│   └── Rock_01
├── Obstacles
│   ├── TreeCollider
│   └── RockCollider
└── SpawnPoints
    ├── SpawnPoint_01
    ├── SpawnPoint_02
    └── ...
```

Требования к prefab:

1. Корень prefab должен находиться в `(0, 0, 0)` и иметь scale `(1, 1, 1)`.
2. Фон можно сделать одним `SpriteRenderer` с одноцветным квадратом.
3. На препятствия добавьте `BoxCollider2D`, `CircleCollider2D`, `PolygonCollider2D` или `CompositeCollider2D`.
4. На пустые дочерние объекты точек добавьте `SpawnPoint`.
5. Выберите для каждой точки `Zone Type`: `Forest`, `Water`, `Cave`, `Lava`, `Snow`, `Desert` или `Space`.
6. Расставляйте точки вне препятствий и не ставьте их вплотную к старту игрока.
7. Укажите в `BiomeMapData.World Size` размеры области, ограниченной внешними Collider2D.

`WorldExplorationManager` автоматически найдёт все `SpawnPoint` внутри созданного экземпляра prefab. Поле `BiomeMapData.Spawn Points` нужно только как дополнительный authoring fallback.

## Ручная настройка сцены

Автоматический Bootstrap уже выполняет эту настройку. Для полностью ручной сцены создайте:

### Player

- `Rigidbody2D`: Body Type `Dynamic`, Gravity Scale `0`, Freeze Rotation Z;
- `CircleCollider2D` или `BoxCollider2D`;
- `PlayerController2D`;
- `InteractionSystem`;
- дочерний `SpriteRenderer` с простым квадратом/кругом.

### Main Camera

- Projection `Orthographic`;
- `CameraFollow2D`;
- target — Transform игрока.

### Systems

На объекте систем:

- существующие `GameManager`, `ExplorationSystem`, `MutationSystem`, `QuestSystem`, `UIManager`;
- новый `WorldExplorationManager`.

Инициализацию удобнее оставить `MonstrologyBootstrap`, потому что он связывает все зависимости и сохраняет runtime demo content.

## Поток награды

Подбор на карте не записывает прогресс напрямую:

`WorldPickup -> ExplorationSystem.ResolveWorldDiscovery -> GameManager -> SaveSystem`

Поэтому:

- существо открывается в энциклопедии, увеличивает quest progress и сохраняется;
- след увеличивает прежний счётчик и включает прежний бонус редкости;
- предмет/яйцо попадает в тот же инвентарь, который использует `MutationSystem`;
- пустая находка всё равно считается исследованием;
- старая кнопка `Быстрый поиск` использует прежний случайный `Explore()` и остаётся fallback.

## WebGL

Система использует обычные `SpriteRenderer`, `Rigidbody2D`, Collider2D и Unity UI. Addressables, 3D, постобработка и тяжёлые эффекты не используются. Экранный joystick работает через стандартный `EventSystem`, поэтому подходит для WebGL, мобильного браузера и Яндекс Игр.
