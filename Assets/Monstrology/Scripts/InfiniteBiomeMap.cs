using UnityEngine;

namespace Monstrology
{
    public class InfiniteBiomeMap : MonoBehaviour
    {
        private TileRepeater repeater;

        public int PooledTileCount
        {
            get { return repeater != null ? repeater.PooledTileCount : 0; }
        }

        public void Initialize(
            Sprite background,
            Color color,
            Vector2 tileSize,
            Transform player)
        {
            repeater = GetComponent<TileRepeater>();
            if (repeater == null)
            {
                repeater = gameObject.AddComponent<TileRepeater>();
            }

            repeater.Initialize(background, color, tileSize, player, transform.position);
        }
    }
}
