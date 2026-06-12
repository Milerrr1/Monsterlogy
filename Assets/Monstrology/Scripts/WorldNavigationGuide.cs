using UnityEngine;

namespace Monstrology
{
    public class WorldNavigationGuide : MonoBehaviour
    {
        private Transform player;
        private WorldPickup target;
        private SpriteRenderer arrow;

        public WorldPickup Target { get { return target; } }
        public bool HasTarget { get { return target != null && target.CanInteract; } }

        public void Initialize(Transform playerTransform)
        {
            player = playerTransform;
            if (arrow == null)
            {
                GameObject visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(transform, false);
                arrow = visualObject.AddComponent<SpriteRenderer>();
                arrow.sprite = SpriteDatabase.Active.GetCompassArrow();
                arrow.color = new Color(1f, 0.88f, 0.3f, 0.95f);
                arrow.sortingOrder = 40;
                transform.localScale = new Vector3(0.45f, 0.8f, 1f);
            }

            gameObject.SetActive(false);
        }

        public void SetTarget(WorldPickup pickup)
        {
            target = pickup;
            gameObject.SetActive(target != null);
        }

        public void ClearTarget(WorldPickup pickup = null)
        {
            if (pickup != null && pickup != target)
            {
                return;
            }

            target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (player == null || target == null || !target.CanInteract)
            {
                ClearTarget();
                return;
            }

            Vector2 direction = target.transform.position - player.position;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            transform.position = player.position + (Vector3)(direction * 1.25f);
            transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }
    }
}
