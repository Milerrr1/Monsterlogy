using UnityEngine;

namespace Monstrology
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;

        private Camera cameraComponent;
        private Vector3 velocity;
        private Bounds mapBounds;
        private bool hasBounds;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographic = true;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            desired = ClampToBounds(desired);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        public void SetTarget(Transform value)
        {
            target = value;
            SnapToTarget();
        }

        public void SetBounds(Bounds value)
        {
            mapBounds = value;
            hasBounds = value.size.x > 0f && value.size.y > 0f;
            SnapToTarget();
        }

        public void ClearBounds()
        {
            hasBounds = false;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = ClampToBounds(
                new Vector3(target.position.x, target.position.y, transform.position.z));
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!hasBounds || cameraComponent == null)
            {
                return position;
            }

            float verticalExtent = cameraComponent.orthographicSize;
            float horizontalExtent = verticalExtent * cameraComponent.aspect;
            float minX = mapBounds.min.x + horizontalExtent;
            float maxX = mapBounds.max.x - horizontalExtent;
            float minY = mapBounds.min.y + verticalExtent;
            float maxY = mapBounds.max.y - verticalExtent;

            position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : mapBounds.center.x;
            position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : mapBounds.center.y;
            return position;
        }
    }
}
