# Монстрология: настройка прототипа

Инструкция по новому 2D-миру, prefab-картам, компонентам игрока и связи с `BiomeData`:
[`WORLD_EXPLORATION_SETUP.md`](WORLD_EXPLORATION_SETUP.md).

## Быстрый запуск

1. Откройте `Assets/Scenes/SampleScene.unity`.
2. Нажмите Play.
3. `MonstrologyBootstrap` автоматически создаст игровые системы, демо-данные, `EventSystem` и весь Canvas.

Стартовая сцена уже включена в Build Settings. Никаких объектов для первого запуска добавлять не нужно.

## Runtime-иерархия

Во время Play Mode создаётся:

```text
Monstrology
├── GameManager
├── ExplorationSystem
├── MutationSystem
├── QuestSystem
├── YandexGamesBridge
├── UIManager
└── MonstrologyCanvas
    ├── BiomeBackground
    ├── TopBar
    ├── ResultCard
    ├── Explore
    ├── Tracks
    ├── BottomBar
    ├── Энциклопедия
    ├── Карта биомов
    ├── Лаборатория мутаций
    └── Квесты
```

Canvas использует `Screen Space Overlay`, `CanvasScaler / Scale With Screen Size`,
reference resolution `960 x 540`, match `0.5`.

## Кнопки и методы

Если Canvas собирается вручную, кнопки можно связать с публичными методами `UIManager`:

| Кнопка | Метод |
|---|---|
| Исследовать | `UIManager.OnExploreButton()` |
| Биомы | `UIManager.OpenBiomes()` |
| Энциклопедия | `UIManager.OpenEncyclopedia()` |
| Мутации | `UIManager.OpenMutations()` |
| Квесты | `UIManager.OpenQuests()` |
| Мутировать | `UIManager.PerformMutation()` |
| Закрыть окно | `UIManager.CloseAllPanels()` |
| Энергия за рекламу | `UIManager.ShowRewardedEnergy()` |

Динамические кнопки выбора биома, получения квеста и покупки подсказки привязываются кодом,
потому что каждая из них хранит ссылку на конкретные данные.

## Авторские ScriptableObject-данные

Демо-набор создаётся в памяти классом `DemoContentFactory`. Для настоящего контента:

1. Создайте пустой объект `Monstrology` и добавьте `MonstrologyBootstrap`.
2. Снимите `Use Runtime Demo Content`.
3. Создайте ассеты через меню:
   - `Create > Monstrology > Creature`
   - `Create > Monstrology > Biome`
   - `Create > Monstrology > Item`
   - `Create > Monstrology > Quest`
4. Заполните списки `Authored Content` у bootstrap.
5. В каждом `BiomeData` заполните `Available Creatures`.
6. В `Mutation Recipes` укажите ID базового существа, предмета и результата.

ID должны быть уникальными, стабильными и состоять из латиницы, цифр и подчёркиваний.
Сохранение использует ID, поэтому переименование отображаемого имени безопасно, а изменение ID
после релиза разорвёт старые записи прогресса.

### CreatureData

- `Id`: стабильный ID.
- `Creature Name`, `Description`, `Icon`.
- `Rarity`: Common, Rare, Epic, Legendary или Secret.
- `Element`, `Biome`.
- `Appearance Chance`: относительный вес внутри биома.
- `Appearance Conditions`: список условий; все условия списка должны выполниться.
- `Found`: только предпросмотр в Inspector. Реальный прогресс хранит `GameManager`.

Доступные условия:

- `CurrentBiome`: конкретный текущий биом.
- `HasItem`: наличие предмета по ID.
- `FoundCreaturesOfElement`: число открытых видов выбранной стихии.
- `ExplorationCount`: общее число исследований.

### BiomeData

Заполните тип, имя, цену, описание, фон, резервный цвет и список существ. Лес должен иметь
цену `0`, потому что он открыт в новом сохранении.

### ItemData

Заполните ID, имя, описание, тип, иконку и предпочтительный биом. Предпочтительный биом
даёт предмету повышенную вероятность при исследовании этой локации.

### QuestData

Выберите цель, требуемое количество, награду монетами и энергией. Для `UnlockBiome`
также задайте нужный биом.

## Сохранение

`SaveSystem` хранит JSON в `PlayerPrefs` под ключом `Monstrology.Progress.v1`.
Сохраняются монеты, энергия, биом, исследования, мутации, существа, предметы, следы,
квесты и купленные подсказки.

Для сброса во время разработки вызовите `GameManager.ResetProgress()` или удалите PlayerPrefs.

## WebGL и Яндекс Игры

1. Переключите платформу: `File > Build Settings > WebGL > Switch Platform`.
2. В Player Settings используйте WebGL Template `Default`, compression `Gzip` или `Brotli`
   в зависимости от настроек хостинга.
3. Оставьте сцену `Assets/Scenes/SampleScene.unity` первой в Build Settings.
4. Соберите проект и загрузите содержимое папки билда в черновик Яндекс Игр.

`YandexGamesBridge` сейчас является заглушкой. Для подключения SDK замените тела:

- `ShowRewardedAd(Action<bool>)`
- `SaveProgress()`
- `LoadProgress()`
- `ShowLeaderboard()`

Остальной игровой код уже обращается к этому мосту и не должен зависеть от конкретного SDK.

## Основные файлы

- `GameManager.cs`: состояние, экономика, условия и прогресс.
- `ExplorationSystem.cs`: таблица результатов и выбор существ.
- `SaveSystem.cs`: сериализация PlayerPrefs.
- `EncyclopediaUI.cs`, `BiomeUI.cs`, `UIManager.cs`: интерфейс.
- `MutationSystem.cs`, `QuestSystem.cs`: мутации и квесты.
- `MonstrologyBootstrap.cs`: самозапуск и демо-контент.
- `YandexGamesBridge.cs`: точка интеграции Яндекс Игр.
