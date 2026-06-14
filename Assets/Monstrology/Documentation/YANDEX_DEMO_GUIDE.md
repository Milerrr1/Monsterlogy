# Yandex Games Demo

## Installed PluginYG2

- Plugin Your Games: `v2.0092`
- Yandex Games platform: `v1.0091`
- RewardedAdv: `v1.011`
- InterstitialAdv: `v1.02`
- Platform define: `YandexGamesPlatform_yg`
- Module defines: `RewardedAdv_yg`, `InterstitialAdv_yg`
- WebGL template: `PROJECT:YandexGames`

The game uses the existing `YandexGamesBridge`. Runtime game systems do not
call `YG2` directly.

## Rewarded Energy

The existing energy button calls:

```csharp
YandexGamesBridge.Instance.ShowRewardedAd(
    "energy_3",
    rewardCallback);
```

The button is disabled while the request is active. Energy is granted only
inside the confirmed reward callback. The default configurable value is
`rewardedEnergyAmount = 3`, and `GameManager.AddEnergy` clamps the result to
the existing maximum energy value.

Closing or failing the advertisement does not grant energy. After a completed
request the HUD is refreshed, the local progress is flushed, and the player
receives a notification.

## Unity Editor Fallback

The bridge does not request a real advertisement in the Unity Editor. It:

1. Logs `[YandexBridge] Rewarded ad simulated in Unity Editor.`
2. Simulates the advertisement pause.
3. Calls the reward callback exactly once.
4. Restores the previous time scale, audio pause and input state.

The diagnostic overlay is visible in the Unity Editor and Development Builds.
It is excluded from normal release builds.

## Game Ready And Gameplay Markup

PluginYG2 `Auto GRA` is disabled. The bridge sends `YG2.GameReadyAPI()` once,
after the world, player and HUD exist and `UIManager` reports that gameplay
input is available.

`GameplayStart` and `GameplayStop` are sent only when the bridge state changes.
Gameplay is stopped for advertisements, browser focus loss, pause menus and
blocking panels.

## Saving

Cloud Storage is not active because the official PluginYG2 `Storage` and
`Authorization` modules are not installed. Progress remains in the existing
`SaveSystem` and `PlayerPrefs` key `Monstrology.Progress.v1`.

The save includes a monotonic revision, UTC update timestamp, newer-save
comparison and a local backup before external import. `CloudSaveCoordinator`
reports the missing modules without emulating a platform API.

`SavePlatformProgress` flushes the current local save and calls
`PlayerPrefs.Save()`. Existing game actions already pass through
`GameManager.Save()`, including creatures, biome unlocks, pet levels,
evolutions, wardrobe changes, daily rewards and achievements. Progress is
also flushed when browser focus is lost and when the application exits.

## Prepare Demo Build

Run:

`Tools > Monstrology > Yandex > Prepare Demo Build`

The command:

- switches the active target to WebGL;
- places `Assets/Scenes/SampleScene.unity` first;
- removes PluginYG2 example scenes from Scenes In Build;
- selects the Yandex Games WebGL template;
- checks the selected PluginYG2 platform;
- enables a Development Build for the demo diagnostics;
- creates `Builds/YandexDemo/Monstrology`.

## Build Demo ZIP

Run:

`Tools > Monstrology > Yandex > Build Demo ZIP`

The command prepares and builds WebGL. PluginYG2's enabled archive processor
creates the ZIP, so the Monstrology tool does not create a duplicate archive.

The archive is written next to the build directory:

`Builds/YandexDemo/Monstrology_YandexGames_Build(N).zip`

The tool verifies that `index.html`, `Build/` and `TemplateData/` are at the
ZIP root and that archive paths contain no spaces or non-ASCII characters.

## Validate Demo Build

Run:

`Tools > Monstrology > Yandex > Validate Demo Build`

The validator prints `PASS`, `WARNING` and `ERROR` entries and finishes with:

- `YANDEX DEMO READY`
- `YANDEX DEMO NOT READY`

It checks PluginYG2, Yandex Games, advertisement modules and defines, bridge
methods, reward callback placement, gameplay markup guards, scene order,
WebGL template, ZIP structure and loaded Unity assemblies.

## Manual Draft Checks

After uploading the ZIP to a Yandex Games draft:

1. Confirm the game reaches the map before Game Ready is sent.
2. Confirm `energy_3` opens a real rewarded advertisement.
3. Close an advertisement early and verify that energy is unchanged.
4. Complete an advertisement and verify that exactly 3 energy is granted.
5. Open the pause menu before an advertisement and verify it remains paused.
6. Switch browser tabs and verify audio and movement stop.
7. Return to the tab and verify gameplay resumes only when no blocking UI is
   open.
8. Reload the draft and verify the existing PlayerPrefs progress.

## Changed Integration Files

- `Assets/Monstrology/Scripts/YandexGamesBridge.cs`
- `Assets/Monstrology/Scripts/UIManager.cs`
- `Assets/Monstrology/Scripts/SaveSystem.cs`
- `Assets/Monstrology/Scripts/InteractionSystem.cs`
- `Assets/Monstrology/Scripts/MonstrologyBootstrap.cs`
- `Assets/Monstrology/Scripts/YandexGamesDebugOverlay.cs`
- `Assets/Monstrology/Editor/YandexDemoBuildTools.cs`
- `Assets/Monstrology/Editor/MonstrologySmokeTest.cs`
- `Assets/PluginYourGames/Resources/SettingsYG2.asset`
- `Assets/WebGLTemplates/YandexGames/TemplateData/README.txt`
