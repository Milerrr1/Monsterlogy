using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class InteractionSystem : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField, Min(0.25f)] private float interactionRadius = 1.6f;

        private Text promptText;
        private Button interactButton;
        private WorldPickup nearestPickup;
        private bool interactionEnabled = true;
        private bool mobileMode;

        public WorldPickup NearestPickup { get { return nearestPickup; } }
        public bool InteractionEnabled { get { return interactionEnabled; } }

        private void Awake()
        {
            if (player == null)
            {
                player = transform;
            }
        }

        private void Update()
        {
            FindNearestPickup();

            if (interactionEnabled && nearestPickup != null && Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }

        public void Initialize(Transform playerTransform)
        {
            player = playerTransform != null ? playerTransform : transform;
        }

        public void BindUI(Text prompt, Button button)
        {
            promptText = prompt;
            interactButton = button;

            if (interactButton != null)
            {
                interactButton.onClick.RemoveListener(Interact);
                interactButton.onClick.AddListener(Interact);
            }

            RefreshPrompt();
        }

        public void EnableInteraction(bool value)
        {
            interactionEnabled = value;
            if (!value)
            {
                nearestPickup = null;
            }

            RefreshPrompt();
        }

        public void SetMobileMode(bool value)
        {
            mobileMode = value;
            RefreshPrompt();
        }

        public void Interact()
        {
            if (!interactionEnabled || nearestPickup == null)
            {
                return;
            }

            WorldPickup pickup = nearestPickup;
            nearestPickup = null;
            pickup.Interact();
            RefreshPrompt();
        }

        private void FindNearestPickup()
        {
            WorldPickup best = null;
            float bestDistance = interactionRadius * interactionRadius;
            Vector3 origin = player != null ? player.position : transform.position;

            foreach (WorldPickup pickup in WorldPickup.ActivePickups)
            {
                if (pickup == null || !pickup.CanInteract)
                {
                    continue;
                }

                float distance = (pickup.transform.position - origin).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = pickup;
                }
            }

            if (best != nearestPickup)
            {
                nearestPickup = best;
                RefreshPrompt();
            }
        }

        private void RefreshPrompt()
        {
            bool visible = interactionEnabled && nearestPickup != null;
            if (promptText != null)
            {
                promptText.gameObject.SetActive(visible && !mobileMode);
                promptText.text = visible
                    ? "Нажмите E — " + nearestPickup.DisplayName
                    : string.Empty;
            }

            if (interactButton != null)
            {
                interactButton.gameObject.SetActive(
                    interactionEnabled && mobileMode);
                interactButton.interactable = visible;
            }
        }
    }
}
