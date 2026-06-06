using UnityEngine;
using UnityEngine.EventSystems;

namespace Monstrology
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField, Min(10f)] private float radius = 54f;

        private RectTransform root;
        private PlayerController2D player;
        private Camera eventCamera;

        private void Awake()
        {
            root = transform as RectTransform;
        }

        public void Initialize(PlayerController2D controller, RectTransform joystickHandle, float joystickRadius)
        {
            player = controller;
            handle = joystickHandle;
            radius = Mathf.Max(10f, joystickRadius);
            root = transform as RectTransform;
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            eventCamera = eventData.pressEventCamera;
            UpdateInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetJoystick();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        private void UpdateInput(PointerEventData eventData)
        {
            if (root == null || player == null)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    root, eventData.position, eventCamera, out localPoint))
            {
                return;
            }

            Vector2 input = Vector2.ClampMagnitude(localPoint / radius, 1f);
            if (handle != null)
            {
                handle.anchoredPosition = input * radius;
            }

            player.SetMobileInput(input);
        }

        private void ResetJoystick()
        {
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (player != null)
            {
                player.ClearMobileInput();
            }
        }
    }
}
