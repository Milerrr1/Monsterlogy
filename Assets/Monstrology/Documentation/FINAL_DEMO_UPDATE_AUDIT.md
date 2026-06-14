# Final Demo Update Audit

Audit date: 2026-06-13  
Project: `D:\Monstrologyv2`  
Unity: `2022.3.62f2`

## Baseline

- A clean batch-mode import and script compilation completed successfully on an isolated project copy.
- C# compiler errors: none.
- C# compiler warnings: none.
- Active gameplay scene: `Assets/Scenes/SampleScene.unity`.
- The project builds its demo content and most UI at runtime from `MonstrologyBootstrap`.
- Current local save key: `Monstrology.Progress.v1`.
- Current save structure version before this update: `9`.

## Input And UI

- Desktop movement: `PlayerController2D` reads Unity axes `Horizontal` and `Vertical`.
- Desktop interaction: `InteractionSystem` reads `KeyCode.E`.
- Pause: `UIManager` reads `KeyCode.Escape`.
- Mobile movement: `VirtualJoystick` writes to `PlayerController2D.SetMobileInput`.
- Mobile interaction: runtime UI button bound through `InteractionSystem.BindUI`.
- Mobile controls were created unconditionally before this update.
- The runtime canvas uses `CanvasScaler.ScaleWithScreenSize`, reference resolution `960x540`.
- No safe-area controller or runtime device-mode service existed.
- Main panels are created once, but their full-screen overlays cover the bottom navigation.
- The pause menu still created an `Exit` button and retained an `Application.Quit` code path.
- The development diagnostics overlay was always expanded.

## Pet Creation And Migration

Pet records can be created from:

- `CreatureCollectionManager.AddPet`.
- `CreatureCollectionManager.AddPetInstance`.
- `CreatureCollectionManager.HandleCreatureRegistered`.
- `CreatureCollectionManager.RestoreMissingDiscoveredPets`.
- `BreedingSystem.EvolveSpecies` fallback creation.
- Editor/debug helpers.

Existing protections:

- `GetPetBySpecies` prevents a second pet for the same species.
- Duplicate saved pets are merged.
- First discovery raises `CreatureRegistered`, which creates a pet.
- Missing discovered pets are restored on collection load.

Remaining risks before this update:

- The discovered-pet repair was idempotent but not version-marked in the save.
- Custom names were trimmed and length-limited, but control characters were not removed.

## Drop Tables And First Hour

Drop selection is currently split between:

- `WorldExplorationManager.RollPickupType`.
- `ExplorationSystem.SelectWorldCreature`.
- `ExplorationSystem.SelectCreature`.
- `ExplorationSystem.SelectItem`.
- `ExplorationSystem.SelectAccessory`.
- Track-chain, nest, event and starter-boost systems.

The primary world weights before this update were hard-coded:

- traces: `0.35`;
- resources and eggs: `0.25`;
- wardrobe: `0.10`;
- creatures: `0.30`.

There was no first-session phase model, no shared configurable balance asset and no repeat/pity history.

## Evolution

Evolution definitions are created in:

- `MonstrologyBootstrap.CreateEvolutions`;
- `EvolutionFallbackBuilder`.

Thresholds before this update were `100`, `250`, `500`.

`BreedingSystem` checked copies of the current form and consumed them. This made later forms dependent on copies of forms that do not normally spawn in the wild. The final update must use cumulative root-species progress and must not spend collected copies.

## Saves And Daily Reward

- Local saves use the existing `SaveSystem` and `PlayerPrefs`.
- The save has no revision/timestamp conflict metadata.
- Daily rewards use `lastDailyRewardUtcDate` and `dailyRewardStreak`.
- With an empty date, `DailyRewardSystem.CanClaimToday` returned true, so a new account could claim on its first launch.

Migration risks:

- New fields must remain optional under Unity `JsonUtility`.
- Existing pet records, discovered species, inventory, wardrobe, nests and progression must be retained.
- Previously spent evolution copies cannot be reconstructed exactly; migration can only guarantee a cumulative floor appropriate to the saved evolution stage.

## PluginYG2

Installed modules:

- `RewardedAdv`;
- `InterstitialAdv`.

Installed platform:

- Yandex Games;
- scripting defines include `YandexGamesPlatform_yg`, `RewardedAdv_yg` and `InterstitialAdv_yg`.

Not installed:

- `Storage`;
- `Authorization`.

The plugin server catalog lists both modules as downloadable, but their runtime code and scripting defines are absent. The Yandex platform asset has `saveCloud` enabled, but that flag alone does not provide a cloud API.

Consequences:

- Rewarded/interstitial ads, Game Ready and Gameplay markup can continue through `YandexGamesBridge`.
- Real account cloud synchronization cannot be implemented or truthfully validated until the official PluginYG2 `Storage` and `Authorization` modules are installed.
- This update will add save revision metadata, deterministic conflict resolution, local backup/import APIs and a cloud coordinator that reports the missing-module state without breaking Editor or local saves.
- The final validator must report cloud synchronization as unavailable instead of claiming it is ready.

## Regression Guard

The following already-fixed behavior must remain intact:

- Russian/Cyrillic WebGL fonts.
- `vacuum_rhino` remains the rare Desert creature and keeps its pet/name/favorite behavior.
- Repeated species encounters increase copies without creating duplicate pets.
- Pet followers do not display rarity labels.
- `fire_stone` does not drop in the Forest and linked resource names stay hidden until discovery.
- Nest cooldown behavior and forest boots equipment remain unchanged.
- Rewarded energy is granted only from the confirmed reward callback.
- Game Ready is sent once after gameplay becomes available.
- Gameplay Start/Stop remains state-driven.
- The existing Yandex WebGL template and build pipeline remain in use.
