using UnityEngine;

namespace Monstrology
{
    public class InfiniteBiomeMap : MonoBehaviour
    {
        private TileRepeater backgroundRepeater;
        private TileRepeater groundRepeater;
        private BiomeDecorationRepeater decorationRepeater;

        public int PooledTileCount
        {
            get
            {
                return backgroundRepeater != null
                    ? backgroundRepeater.PooledTileCount
                    : 0;
            }
        }

        public int GroundPooledTileCount
        {
            get
            {
                return groundRepeater != null
                    ? groundRepeater.PooledTileCount
                    : 0;
            }
        }

        public int DecorationSegmentCount
        {
            get
            {
                return decorationRepeater != null
                    ? decorationRepeater.SegmentCount
                    : 0;
            }
        }

        public void Initialize(
            Sprite background,
            Color color,
            Vector2 tileSize,
            Transform player)
        {
            Initialize(
                BiomeType.Forest,
                background,
                color,
                tileSize,
                null,
                player,
                Vector2.zero);
        }

        public void Initialize(
            BiomeType biome,
            Sprite background,
            Color color,
            Vector2 tileSize,
            BiomeVisualConfig visualConfig,
            Transform player,
            Vector2 playerStart)
        {
            Vector2 backgroundSize =
                visualConfig != null &&
                visualConfig.backgroundWorldSize.x > 0f &&
                visualConfig.backgroundWorldSize.y > 0f
                    ? visualConfig.backgroundWorldSize
                    : tileSize;
            backgroundRepeater = EnsureRepeater(
                "BackgroundBase",
                backgroundRepeater);
            backgroundRepeater.Initialize(
                background,
                color,
                backgroundSize,
                player,
                transform.position,
                -1000,
                "BackgroundBaseTile");

            bool hasGround = visualConfig != null &&
                             visualConfig.groundDetailEnabled &&
                             visualConfig.groundDetail != null;
            Transform groundRoot = transform.Find("GroundDetail");
            if (hasGround)
            {
                groundRepeater = EnsureRepeater(
                    "GroundDetail",
                    groundRepeater);
                groundRepeater.gameObject.SetActive(true);
                groundRepeater.Initialize(
                    visualConfig.groundDetail,
                    new Color(
                        1f,
                        1f,
                        1f,
                        Mathf.Clamp01(visualConfig.groundDetailOpacity)),
                    visualConfig.groundTileWorldSize,
                    player,
                    transform.position,
                    -990,
                    "GroundDetailTile");
            }
            else if (groundRoot != null)
            {
                groundRoot.gameObject.SetActive(false);
            }

            bool hasDecorations = visualConfig != null &&
                                  visualConfig.decorations != null &&
                                  visualConfig.decorations.Exists(entry =>
                                      entry != null && entry.sprite != null);
            Transform decorationRoot = transform.Find("WorldDecorations");
            if (hasDecorations)
            {
                if (decorationRepeater == null)
                {
                    if (decorationRoot == null)
                    {
                        GameObject root = new GameObject("WorldDecorations");
                        root.transform.SetParent(transform, false);
                        decorationRoot = root.transform;
                    }

                    decorationRepeater =
                        decorationRoot.GetComponent<BiomeDecorationRepeater>();
                    if (decorationRepeater == null)
                    {
                        decorationRepeater =
                            decorationRoot.gameObject
                                .AddComponent<BiomeDecorationRepeater>();
                    }
                }

                decorationRepeater.gameObject.SetActive(true);
                decorationRepeater.Initialize(
                    biome,
                    visualConfig,
                    backgroundSize,
                    player,
                    transform.position,
                    playerStart);
            }
            else if (decorationRoot != null)
            {
                decorationRoot.gameObject.SetActive(false);
            }
        }

        private TileRepeater EnsureRepeater(
            string childName,
            TileRepeater current)
        {
            if (current != null)
            {
                return current;
            }

            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            TileRepeater repeater = child.GetComponent<TileRepeater>();
            return repeater != null
                ? repeater
                : child.gameObject.AddComponent<TileRepeater>();
        }
    }
}
