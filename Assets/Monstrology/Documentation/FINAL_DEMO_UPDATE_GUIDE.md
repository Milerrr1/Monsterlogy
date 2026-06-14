# Final Demo Update Guide

## Apply And Validate

Run these commands in order:

1. `Tools > Monstrology > Apply Final Demo Update`
2. `Tools > Monstrology > Validate Final Demo Update`
3. `Tools > Monstrology > UI > Validate Russian Fonts`
4. `Tools > Monstrology > Yandex > Validate Demo Build`
5. `Tools > Monstrology > Yandex > Build Demo ZIP`

`Apply Final Demo Update` is repeatable. It creates the balance asset when it
is missing, keeps the existing save key and content IDs, selects the Yandex
WebGL template, keeps Auto GRA disabled, generates the first-hour simulation
report and runs the validator.

## Controls

Desktop mode:

- movement uses WASD/Unity horizontal and vertical axes;
- interaction uses `E`;
- Escape opens and closes pause;
- the joystick and mobile interaction button are inactive and do not block
  mouse clicks.

Mobile mode:

- the joystick is on the left;
- the interaction button is on the right, above the bottom navigation;
- the desktop `E` prompt is hidden;
- the fullscreen button is shown;
- safe area is applied to the runtime HUD and panels.

Mode detection uses `Application.isMobilePlatform`, handheld touch
capabilities and a WebGL user-agent bridge. It is not based on resolution.

## iPhone And WebGL

The Yandex template uses `viewport-fit=cover`, `visualViewport`,
`100dvh`, fixed canvas positioning and disabled page scrolling. Unity applies
`Screen.safeArea` plus CSS safe-area insets returned by the WebGL bridge.

Fullscreen is never requested automatically. It is requested only from the
mobile `FULL` button. Unsupported iOS Safari versions simply remain in the
available viewport.

## World Pickups

Wardrobe pickup sprites are normalized once from `sprite.bounds`. The largest
sprite side is scaled toward `0.9` world units with a defensive
`0.05..1.5` clamp. The root object and interaction text are not scaled with
the artwork. The `CircleCollider2D` is recalculated from the normalized size.

This affects only map pickups. Equipped offsets, equipped scales, wardrobe
previews and source PNG files are unchanged.

## Pets And Daily Reward

The first discovery event creates one pet record before the naming dialog is
shown. Repeated encounters only increase species copies. Custom names remove
control characters, collapse whitespace and use a 24-character limit.

Save migration version `1` restores a missing pet for every already discovered
species and merges old duplicate species records.

Daily reward now records the first UTC launch date. It cannot be claimed on
that date or after a restart on the same date. The first claim becomes
available on the next UTC date.

## Evolution

Evolution uses lifetime root-species discoveries:

- form I to II: `10`;
- form II to III: `25`;
- form III to final: `60`;
- optional future transition: `120`.

The values are cumulative and are not spent. Old evolved pets receive a
minimum lifetime-copy floor matching the form already reached.

## First Hour

`FirstHourBalanceConfig` is the single configurable source for category
weights, phase multipliers, cooldown values, pity timers and repeat limits.
`FirstSessionDirector` only adjusts the existing selectors.

It tracks active gameplay time, recent findings, new content, wardrobe
progress, rare streaks, evolution proximity and energy/gameplay availability.
Paused menus, advertisements, inactive browser focus and blocked gameplay do
not advance the timer.

Run `Tools > Monstrology > Balance > Simulate First Hour` to regenerate:

`Assets/Monstrology/Documentation/FIRST_HOUR_BALANCE_REPORT.md`

## Cloud Status

The installed PluginYG2 contains `RewardedAdv` and `InterstitialAdv` only.
The official `Storage` and `Authorization` modules are not installed.

The project therefore remains on the existing local
`Monstrology.Progress.v1` PlayerPrefs save. The save now includes revision and
UTC update metadata, deterministic newer-save comparison and a local backup
before external import. `CloudSaveCoordinator` reports the missing-module
state and does not invent or emulate a cloud API.

To complete real cross-device synchronization:

1. Install the official PluginYG2 `Authorization` module.
2. Install the official PluginYG2 `Storage` module.
3. Reopen Unity so `Authorization_yg` and `Storage_yg` are generated.
4. Bind the verified module API in `CloudSaveCoordinator`.
5. Test authorized and anonymous accounts on two physical devices.
6. Re-run `Validate Final Demo Update`.

Until those steps are complete, the correct validator result is
`FINAL DEMO UPDATE NOT READY`.

## Manual Device Checks

- Desktop WebGL: WASD, E, mouse UI, direct tab switching, Escape pause.
- iPhone Safari: landscape and portrait viewport, safe area, address-bar
  resize, fullscreen rejection fallback and no HTML scroll.
- Android browser: joystick plus simultaneous interaction touch.
- Yandex draft: rewarded callback, ad pause/audio restore, Game Ready once and
  Gameplay Start/Stop transitions.
- Two-device cloud test after the missing official modules are installed.
