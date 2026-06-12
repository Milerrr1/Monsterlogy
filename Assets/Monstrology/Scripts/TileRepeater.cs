using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class TileRepeater : MonoBehaviour
    {
        private const int GridRadius = 1;

        private readonly List<Transform> pooledTiles = new List<Transform>();
        private Transform target;
        private Vector2 tileSize;
        private Vector2 origin;
        private Vector2Int currentCell = new Vector2Int(int.MinValue, int.MinValue);

        public int PooledTileCount { get { return pooledTiles.Count; } }

        public void Initialize(
            Sprite sprite,
            Color color,
            Vector2 size,
            Transform followTarget,
            Vector2 worldOrigin)
        {
            target = followTarget;
            tileSize = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            origin = worldOrigin;
            EnsurePool(
                sprite != null
                    ? sprite
                    : SpriteDatabase.Active.GetBiomeBackground(null, null),
                color);
            RefreshTiles(true);
        }

        private void LateUpdate()
        {
            RefreshTiles(false);
        }

        private void EnsurePool(Sprite sprite, Color color)
        {
            int required = (GridRadius * 2 + 1) * (GridRadius * 2 + 1);
            while (pooledTiles.Count < required)
            {
                GameObject tile = new GameObject("BackgroundTile_" + pooledTiles.Count);
                tile.transform.SetParent(transform, false);
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = -100;
                Vector2 spriteSize = sprite.bounds.size;
                tile.transform.localScale = new Vector3(
                    tileSize.x / Mathf.Max(0.01f, spriteSize.x),
                    tileSize.y / Mathf.Max(0.01f, spriteSize.y),
                    1f);
                pooledTiles.Add(tile.transform);
            }
        }

        private void RefreshTiles(bool force)
        {
            if (target == null || pooledTiles.Count == 0)
            {
                return;
            }

            Vector2 relative = (Vector2)target.position - origin;
            Vector2Int cell = new Vector2Int(
                Mathf.FloorToInt((relative.x + tileSize.x * 0.5f) / tileSize.x),
                Mathf.FloorToInt((relative.y + tileSize.y * 0.5f) / tileSize.y));
            if (!force && cell == currentCell)
            {
                return;
            }

            currentCell = cell;
            int index = 0;
            for (int y = -GridRadius; y <= GridRadius; y++)
            {
                for (int x = -GridRadius; x <= GridRadius; x++)
                {
                    pooledTiles[index++].position = new Vector3(
                        origin.x + (cell.x + x) * tileSize.x,
                        origin.y + (cell.y + y) * tileSize.y,
                        1f);
                }
            }
        }
    }
}
