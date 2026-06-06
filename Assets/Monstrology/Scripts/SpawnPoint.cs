using UnityEngine;

namespace Monstrology
{
    public enum SpawnZoneType
    {
        Forest,
        Water,
        Cave,
        Lava,
        Snow,
        Desert,
        Space
    }

    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnZoneType zoneType = SpawnZoneType.Forest;
        [SerializeField, Min(0f)] private float spawnRadius = 0.35f;

        private WorldPickup occupant;

        public SpawnZoneType ZoneType { get { return zoneType; } }
        public bool IsOccupied { get { return occupant != null; } }

        public Vector3 GetSpawnPosition()
        {
            Vector2 offset = spawnRadius > 0f ? Random.insideUnitCircle * spawnRadius : Vector2.zero;
            return transform.position + new Vector3(offset.x, offset.y, 0f);
        }

        public void SetZoneType(SpawnZoneType value)
        {
            zoneType = value;
        }

        public void Occupy(WorldPickup pickup)
        {
            occupant = pickup;
        }

        public void Release(WorldPickup pickup)
        {
            if (occupant == pickup)
            {
                occupant = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.35f, 0.95f, 0.65f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.12f, spawnRadius));
        }
#endif
    }
}
