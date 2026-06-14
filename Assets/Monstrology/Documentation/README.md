# Монстрология: настройка прототипа

Инструкция по новому 2D-миру, prefab-картам, компонентам игрока и связи с `BiomeData`:
[`WORLD_EXPLORATION_SETUP.md`](WORLD_EXPLORATION_SETUP.md).

Инструкция по личным питомцам, сохранению экземпляров и окну коллекции:
[`PETS_SYSTEM_SETUP.md`](PETS_SYSTEM_SETUP.md).

Интеграция первого графического пакета Леса:
[`FOREST_ART_PACK_GUIDE.md`](FOREST_ART_PACK_GUIDE.md).

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
├── PetUpgradeSystem
├── BreedingSystem
├── WorldEnvironmentSystem
├── EnergyRegenerationSystem
├── StarterBoostSystem
├── SignatureSetSystem
├── CreatureNestSystem
├── BiomeEventSystem
├── FavoriteHelperSystem
├── AchievementSystem
├── AudioManager
├── DailyRewardSystem
├── GameplayHintSystem
├── EncyclopediaAvailabilityCheck
├── ReleaseReadinessCheck
├── QuestSystem
├── YandexGamesBridge
├── UIManager
├── InfiniteBiomeMap
├── TrackChainSystem
└── MonstrologyCanvas
    ├── BiomeBackground
    ├── TopBar
    ├── ResultCard
    ├── Explore
    ├── Tracks
    ├── BottomBar
    ├── Энциклопедия
    ├── Карта биомов
    ├── Питомцы / Гардероб
    ├── Эволюция видов
    ├── Логовище
    ├── Квесты
    ├── Пауза / Настройки / Достижения
    ├── Вступление профессора
    └── Ежедневная награда
```

Canvas использует `Screen Space Overlay`, `CanvasScaler / Scale With Screen Size`,
reference resolution `960 x 540`, match `0.5`.

## Кнопки и методы

Если Canvas собирается вручную, кнопки можно связать с публичными методами `UIManager`:

| Кнопка | Метод |
|---|---|
| Компас исследователя | `UIManager.OnExploreButton()` |
| Биомы | `UIManager.OpenBiomes()` |
| Энциклопедия | `UIManager.OpenEncyclopedia()` |
| Питомцы | `UIManager.OpenPets()` |
| Гардероб | `UIManager.OpenWardrobe()` |
| Эволюция | `UIManager.OpenBreeding()` |
| Квесты | `UIManager.OpenQuests()` |
| Пауза | `UIManager.TogglePause()` |
| Закрыть окно | `UIManager.CloseAllPanels()` |
| Энергия за рекламу | `UIManager.ShowRewardedEnergy()` |

Динамические кнопки выбора биома, получения квеста и покупки подсказки привязываются кодом,
потому что каждая из них хранит ссылку на конкретные данные.

Достижения открываются из меню паузы. `Escape` закрывает текущее окно, возвращает в меню
паузы или продолжает игру. Громкость музыки и звуков сохраняется отдельно в PlayerPrefs
под ключами `MusicVolume` и `SfxVolume`.

## Авторские ScriptableObject-данные

Демо-набор создаётся в памяти классом `DemoContentFactory`. Для настоящего контента:

1. Создайте пустой объект `Monstrology` и добавьте `MonstrologyBootstrap`.
2. Снимите `Use Runtime Demo Content`.
3. Создайте ассеты через меню:
   - `Create > Monstrology > Creature`
   - `Create > Monstrology > Biome`
   - `Create > Monstrology > Item`
   - `Create > Monstrology > Quest`
   - `Create > Monstrology > Signature Set`
   - `Create > Monstrology > Creature Nest`
   - `Create > Monstrology > Biome Event`
4. Заполните списки `Authored Content` у bootstrap.
5. В каждом `BiomeData` заполните `Available Creatures`.
6. В `Species Evolutions` укажите базовый вид, число копий и следующую форму.

`ContentAvailabilityRepair` исправляет недостижимые привязки и условия. Если ручная
цепочка отсутствует или короче трёх форм, `EvolutionFallbackBuilder` добавит стабильные
runtime-формы II/III, а `NestFallbackBuilder` создаст недостающее логовище базового вида.
Ручные данные всегда имеют приоритет.

ID должны быть уникальными, стабильными и состоять из латиницы, цифр и подчёркиваний.
Сохранение использует ID, поэтому переименование отображаемого имени безопасно, а изменение ID
после релиза разорвёт старые записи прогресса.

### CreatureData

- `Id`: стабильный ID.
- `Creature Name`, `Description`.
- `Portrait Sprite`, `World Sprite`, необязательный `Evolution Sprite`.
- `Icon`: legacy-fallback для существующих ассетов.
- `Rarity`: Common, Rare, Epic, Legendary или Secret.
- `Element`, `Biome`.
- `Appearance Chance`: относительный вес внутри биома.
- `Appearance Conditions`: список условий; все условия списка должны выполниться.
- `Allowed Times`: допустимые части суток; пустой список разрешает любое время.
- `Allowed Weather`: допустимая погода; пустой список разрешает любую погоду.
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

Заполните ID, имя, описание, тип, иконку, `Required Species Id` и предпочтительный биом. Предпочтительный биом
даёт предмету повышенную вероятность при исследовании этой локации.

### QuestData

Выберите цель, требуемое количество, награду монетами и энергией. Для `UnlockBiome`
также задайте нужный биом.

### AccessoryData

Обычная одежда имеет выключенный `Is Signature` и может выпадать в любом биоме.
Для сигнатурной одежды заполните `Is Signature`, `Signature Biome Id`,
опциональный `Signature Species Id` и `Set Id`. Legacy-поля
`Signature Set Id` и `Signature Biome` продолжают поддерживаться.

### CreatureNestData

Логово связывается с `Species Id` и биомом. После открытия оно сохраняется и при следующем
посещении биома восстанавливается как постоянный объект карты. `NestPanel` показывает
связанное существо, уровень, таймер и кнопку повторного осмотра.

## Сохранение

`SaveSystem` хранит JSON в `PlayerPrefs` под прежним ключом `Monstrology.Progress.v1`.
Формат версии 8 автоматически дополняет старые сохранения. Сохраняются монеты, энергия и её UTC-таймер, биом,
исследования, открытые виды, копии, питомцы, уровни, предметы, гардероб, следы, время суток,
погода, логовища и их уровни, прогресс комплектов, активные бонусы комплектов, найденные события,
стартовый буст, достижения, квесты, уровни купленных подсказок, прохождение вступления и серия
ежедневных наград.

Новый игрок начинает с `50` монет и `75` энергии; вступление доводит запас до `75`
монет и `100` энергии. Валюта дальше поступает из находок, квестов, достижений и
ежедневных наград.

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

### Замена графики

Центральная база находится в
`Assets/Monstrology/Art/Resources/SpriteDatabase.asset` и открывается через
`Tools > Monstrology > Open Sprite Database`.

`SpriteDatabase` разрешает спрайты существ, предметов, одежды, логовищ и событий по
стабильному `id`, а фоны и декорации — по `BiomeType`. Следы связываются по `TrackType`.
Назначенная в базе ссылка имеет приоритет над совместимыми полями `icon` и `background`
в существующих ScriptableObject. Если новая ссылка ещё не назначена, база выдаёт
runtime-fallback, поэтому незавершённый арт не ломает игру.

Мировые находки, любимчик, компас и декорации используют отдельные дочерние
`SpriteRenderer`; Canvas продолжает использовать штатные `Image`. Замена спрайта не
изменяет ID, прогресс, условия появления или формат сохранения.

Все существа используют `CreatureBaseTemplate`: холст `2048x2048`, прозрачный фон,
safe zone `10%`, фронтальный симметричный вид и пропорции головы/тела/ног `40/40/20`.
`CreatureVisualRig` автоматически создаёт дочерний `Visual` с `SpriteRenderer` и
якоря `HeadAnchor`, `BodyAnchor`, `LegAnchor`. Гардероб использует только эти якоря,
поэтому замена спрайта не требует ручных смещений для каждого вида.

- `GameManager.cs`: состояние, экономика, условия и прогресс.
- `ExplorationSystem.cs`: выдача награды после взаимодействия с находкой.
- `WorldExplorationManager.cs`, `InfiniteBiomeMap.cs`, `TileRepeater.cs`: мир, компас и пул фоновых тайлов.
- `TrackChainSystem.cs`: последовательности следов, ведущие к редкому существу.
- `WorldEnvironmentSystem.cs`: ускоренное время суток и автоматическая погода.
- `EnergyRegenerationSystem.cs`, `StarterBoostSystem.cs`: онлайн/офлайн энергия и первые 15 минут прогрессии.
- `SignatureSetSystem.cs`: прогресс, бонусы и завершение тематических комплектов одежды.
- `CreatureNestSystem.cs`: постоянные логовища, таймеры наград и уровни логовищ.
- `NestPanel.cs`, `WorldPickup.cs`: повторное взаимодействие с видимыми логовищами на карте.
- `ContentAvailabilityRepair.cs`, `EvolutionFallbackBuilder.cs`, `NestFallbackBuilder.cs`:
  автоматическое исправление доступности, недостающие формы II/III и логовища базовых видов.
- `SpriteDatabase.cs`: единая точка назначения и замены всей игровой 2D-графики.
- `CreatureBaseTemplate.cs`, `CreatureVisualValidator.cs`: единый формат существ,
  автоматические якоря гардероба и debug-проверка спрайтов/ссылок.
- `BiomeEventSystem.cs`, `FavoriteHelperSystem.cs`: временные события и пассивная помощь любимчика.
- `AchievementSystem.cs`: проверка и сохранение достижений.
- `AudioManager.cs`, `DailyRewardSystem.cs`, `GameplayHintSystem.cs`: настройки звука,
  ежедневный цикл наград и контекстные подсказки.
- `ContentValidator.cs`, `EncyclopediaAvailabilityCheck.cs`, `ReleaseReadinessCheck.cs`,
  `BetaReadinessCheck.cs`:
  не блокирующая проверка контента, достижимости существ и
  итоговая диагностика релизной и Beta-готовности.
- `SaveSystem.cs`: сериализация PlayerPrefs.
- `EncyclopediaUI.cs`, `BiomeUI.cs`, `UIManager.cs`: интерфейс.
- `PetUpgradeSystem.cs`, `BreedingSystem.cs`, `QuestSystem.cs`: уровни, эволюция и квесты.
- `MonstrologyBootstrap.cs`: самозапуск и демо-контент.
- `YandexGamesBridge.cs`: точка интеграции Яндекс Игр.

В Play Mode итоговую проверку можно запустить через
`Tools > Monstrology > Run Release Readiness Check`. Успешный результат пишет в Console
маркер `MONSTROLOGY_RELEASE_READINESS_PASS`.

Отдельная команда `Tools > Monstrology > Run Encyclopedia Availability Check` проверяет
реальные маршруты получения каждого существа. Успех отмечается маркером
`MONSTROLOGY_ENCYCLOPEDIA_AVAILABILITY_PASS`.

Полный Beta-аудит запускается в Play Mode через
`Tools > Monstrology > Run Beta Readiness Check`. Он проверяет монстров, биомы,
логовища, достижения, эволюции, гардероб, энергию, деньги, энциклопедию и любимчика.
Успешный отчёт заканчивается маркером `MONSTROLOGY_BETA_READINESS_PASS`.

Отдельная проверка визуального стандарта запускается через
`Tools > Monstrology > Validate Creature Template` или одноимённый `ContextMenu`
компонента `CreatureVisualValidator`. Успех отмечается маркером
`CREATURE_TEMPLATE_VALIDATION_PASS`.

## Контент Хлебокота и Пылесосорога

Первые финальные PNG Хлебокота, Пылесосорога и комплекта следопыта подключаются
повторяемой командой:

`Tools > Monstrology > Install Forest Content`

Она настраивает импорт, восстанавливает ожидаемые пути и делает upsert записей
`SpriteDatabase` без дубликатов. Проверка доступна через:

`Tools > Monstrology > Validate Forest Content`

Успешный отчёт заканчивается маркером `FOREST_CONTENT_VALIDATION_PASS`.
Пустынная интеграция Пылесосорога проверяется отдельно:

`Tools > Monstrology > Validate Desert Content`

Успешный отчёт заканчивается маркером `DESERT_CONTENT_VALIDATION_PASS`.

Хлебокот использует ресурс `bread_crumbs`. Пылесосорог относится к Пустыне,
имеет базовый вес появления `0.10` и использует Песчаный фильтр. Стабильный
ID ресурса `forest_battery` сохранён для совместимости со старыми сохранениями.
`forest_set` состоит из `forest_hat`, `forest_scarf`, `forest_boots` и даёт
бонусы `5%`, `10%`, `3%`.

## Финальное обновление демоверсии

Аудит, команды проверки, мобильное управление, safe area, миграции,
накопительные эволюции и баланс первого часа описаны в:

- `FINAL_DEMO_UPDATE_AUDIT.md`;
- `FINAL_DEMO_UPDATE_GUIDE.md`;
- `FIRST_HOUR_BALANCE_REPORT.md`.

Основные команды:

- `Tools > Monstrology > Apply Final Demo Update`;
- `Tools > Monstrology > Validate Final Demo Update`;
- `Tools > Monstrology > Balance > Simulate First Hour`.
