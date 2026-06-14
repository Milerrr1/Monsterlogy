# Forest Art Pack

## Asset Locations

- `Art/Forest/Background/ForestBackground.png`
- `Art/Forest/Ground/ForestGroundTile.png`
- `Art/Forest/Decorations/ForestStump.png`
- `Art/Forest/Decorations/ForestBigMushroom.png`
- `Art/Creatures/Forest/Gribolis.png`
- `Art/Creatures/Forest/Lampcrab.png`

The installer also accepts the original
`Art/Import/ForestGroundTile.png.png` filename and moves it to the canonical
path. Repeated runs do not create duplicate files or database entries.

## Import Settings

All textures are single sRGB sprites with bilinear filtering, mipmaps disabled
and normal WebGL compression.

- Background: PPU 100, Full Rect, Clamp, max size 4096.
- Ground: PPU 128, Full Rect, Clamp, max size 2048. The 1254 px source is kept
  above 1024 to avoid an unnecessary downscale.
- Decorations: PPU 100, Tight mesh, Bottom Center pivot, alpha transparency,
  max size 2048.
- Creatures: PPU 100, Full Rect, Center pivot matching
  `CreatureBaseTemplate`, alpha transparency, max size 2048.

Sources are not resized or cropped. World dimensions are calculated from
`sprite.bounds`.

## Forest Layers

`InfiniteBiomeMap` uses bounded 3x3 pools:

- `BackgroundBase`: 30x18 world units, sorting order -1000.
- `GroundDetail`: 8x8 units per tile, sorting order -990.
- `WorldDecorations`: nine pooled 30x18 decoration segments.

Ground opacity is stored in
`SpriteDatabase > Forest > Visual Config > Ground Detail Opacity`. The
installed value is `0.20`. Disable `Ground Detail Enabled` to compare the
forest without this layer.

No new tile is created every frame. Existing tiles and decoration segments are
repositioned when the player crosses a segment boundary.

## Decorations

`forest_stump`:

- target world box: 1.4x1.1;
- count: 3-6 per segment;
- scale variation: 0.90-1.10;
- collider: 0.88x0.26 at the lower base.

`forest_big_mushroom`:

- target world box: 1.3x1.7;
- count: 2-4 per segment;
- scale variation: 0.90-1.08;
- collider: 0.46x0.30 around the lower stem.

Both colliders are triggers, so this visual pass does not unexpectedly block
movement. Segment coordinates provide stable seeds. Placement has a finite
retry budget and checks other decoration bounds, a protected 2.5-unit player
start radius, the player, pickups and nests.

`WorldExplorationManager` also moves a requested pickup to a nearby free point
when a decoration occupies its original position.

Static decorations calculate their Y order after configuration. Followers and
moving world objects update only after a meaningful Y change. A cached
semi-transparent ellipse provides a lightweight WebGL shadow.

## Creatures

Existing IDs are retained:

- Gribolis: `mushroom_fox`, Forest, Common, appearance chance 1.05.
- Lampcrab: `lamp_crab`, Forest, Rare, appearance chance 0.82.

Effective weights used by the existing selector:

- Gribolis: `1.05`;
- Lampcrab: `0.82 * 0.48 = 0.3936`.

Lampcrab is about 2.67 times rarer than Gribolis. It uses the existing
`ExplorationSystem` and `FirstSessionDirector`; no independent random roll was
added.

Discovery, encyclopedia progress, one pet per species, repeated copies, custom
names, favorite state, follower display and saves continue through the
existing systems. No save ID was changed.

`CreatureVisualRig` creates `HeadAnchor`, `BodyAnchor` and `LegAnchor`
automatically. Per-creature offsets are stored in `CreatureSpriteEntry`. Both
new entries start at zero and need a visual wardrobe check in the Editor.

## Current Source Issue

`Gribolis.png` has transparent outer pixels but also contains a painted
brown/gray vignette around the character. It is not a clean world sprite.

The installer registers it as a portrait but leaves its world override empty,
so gameplay retains the existing safe placeholder instead of displaying a
rectangular background. Validation returns `FOREST ART PACK NOT READY` until
the PNG is replaced with a clean transparent version and the installer is run
again.

Lampcrab and both decoration PNGs contain usable true transparency.

## Atlases

- `ForestWorldAtlas`: stump and large mushroom.
- `ForestCreatureAtlas`: Gribolis and Lampcrab source textures.

The large background and ground tile are not packed into the decoration atlas.

## Commands

- `Tools > Monstrology > Forest > Install Forest Art Pack`
- `Tools > Monstrology > Forest > Validate Forest Art Pack`

The installer moves assets, applies import settings, updates the existing
`SpriteDatabase`, creates or updates atlases and prints a detailed report.

## Manual Checks

After replacing the Gribolis source:

1. Run Install and Validate again.
2. Inspect seams while moving in every direction for several segment widths.
3. Preview 1920x1080, 1366x768 and narrow landscape layouts.
4. Check forest hat, scarf and boots on both creatures. Adjust only the
   serialized per-creature anchor offsets when needed.
5. Check palette balance and compression halos in a WebGL development build.
6. Run `Validate Final Demo Update`, `Validate Russian Fonts` and
   `Validate Demo Build`.
