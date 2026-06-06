using UnityEngine;

namespace Monstrology
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
        [SerializeField] private Transform visual;
        [SerializeField] private bool rotateVisualToDirection = true;

        private Rigidbody2D body;
        private Vector2 keyboardInput;
        private Vector2 mobileInput;
        private Vector2 lastDirection = Vector2.down;
        private bool movementEnabled = true;

        public bool MovementEnabled { get { return movementEnabled; } }
        public Vector2 LastDirection { get { return lastDirection; } }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (visual == null)
            {
                visual = transform;
            }
        }

        private void Update()
        {
            keyboardInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            Vector2 direction = GetCombinedInput();
            if (direction.sqrMagnitude > 0.001f)
            {
                lastDirection = direction.normalized;
                UpdateFacing(lastDirection);
            }
        }

        private void FixedUpdate()
        {
            if (!movementEnabled)
            {
                body.velocity = Vector2.zero;
                return;
            }

            Vector2 direction = GetCombinedInput();
            body.velocity = Vector2.ClampMagnitude(direction, 1f) * moveSpeed;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }
        }

        public void EnableMovement(bool value)
        {
            movementEnabled = value;
            if (!value && body != null)
            {
                keyboardInput = Vector2.zero;
                mobileInput = Vector2.zero;
                body.velocity = Vector2.zero;
            }
        }

        public void SetMobileInput(Vector2 input)
        {
            mobileInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void SetVisual(Transform value)
        {
            visual = value != null ? value : transform;
        }

        public void ClearMobileInput()
        {
            mobileInput = Vector2.zero;
        }

        private Vector2 GetCombinedInput()
        {
            if (!movementEnabled)
            {
                return Vector2.zero;
            }

            Vector2 input = keyboardInput.sqrMagnitude >= mobileInput.sqrMagnitude
                ? keyboardInput
                : mobileInput;
            return Vector2.ClampMagnitude(input, 1f);
        }

        private void UpdateFacing(Vector2 direction)
        {
            if (visual == null)
            {
                return;
            }

            if (rotateVisualToDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                visual.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else if (Mathf.Abs(direction.x) > 0.05f)
            {
                Vector3 scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
                visual.localScale = scale;
            }
        }
    }
}
