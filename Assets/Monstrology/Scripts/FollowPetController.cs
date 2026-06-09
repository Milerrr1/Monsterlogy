using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class FollowPetController : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float followDistance = 1.35f;
        [SerializeField, Min(0.1f)] private float smoothTime = 0.18f;
        [SerializeField, Min(2f)] private float teleportDistance = 8f;

        private PlayerController2D player;
        private CreatureCollectionManager collection;
        private AccessoryInventoryManager accessories;
        private SignatureSetSystem signatureSets;
        private GameObject petObject;
        private SpriteRenderer petRenderer;
        private TextMesh nameText;
        private TextMesh rarityText;
        private Transform accessoryRoot;
        private GameObject fullSetEffect;
        private Vector3 velocity;
        private string displayedPetId;

        public void Initialize(
            PlayerController2D playerController,
            CreatureCollectionManager collectionManager,
            AccessoryInventoryManager accessoryManager)
        {
            player = playerController;
            collection = collectionManager;
            accessories = accessoryManager;
            signatureSets = FindObjectOfType<SignatureSetSystem>();

            if (collection != null)
            {
                collection.CollectionChanged -= RefreshFavorite;
                collection.CollectionChanged += RefreshFavorite;
            }

            if (accessories != null)
            {
                accessories.InventoryChanged -= RefreshFavorite;
                accessories.InventoryChanged += RefreshFavorite;
            }

            if (signatureSets != null)
            {
                signatureSets.SetStateChanged -= RefreshFavorite;
                signatureSets.SetStateChanged += RefreshFavorite;
            }

            EnsureVisual();
            RefreshFavorite();
        }

        private void OnDestroy()
        {
            if (collection != null)
            {
                collection.CollectionChanged -= RefreshFavorite;
            }

            if (accessories != null)
            {
                accessories.InventoryChanged -= RefreshFavorite;
            }

            if (signatureSets != null)
            {
                signatureSets.SetStateChanged -= RefreshFavorite;
            }
        }

        private void LateUpdate()
        {
            if (petObject == null || !petObject.activeSelf || player == null)
            {
                return;
            }

            Vector2 direction = player.LastDirection.sqrMagnitude > 0.01f
                ? player.LastDirection.normalized
                : Vector2.down;
            Vector3 target = player.transform.position - (Vector3)(direction * followDistance);
            target.z = player.transform.position.z;

            if ((petObject.transform.position - target).sqrMagnitude >
                teleportDistance * teleportDistance)
            {
                petObject.transform.position = target;
                velocity = Vector3.zero;
                return;
            }

            petObject.transform.position = Vector3.SmoothDamp(
                petObject.transform.position,
                target,
                ref velocity,
                smoothTime);
        }

        private void EnsureVisual()
        {
            if (petObject != null)
            {
                return;
            }

            petObject = new GameObject("FavoritePetFollower");
            petObject.transform.SetParent(transform, false);
            petRenderer = petObject.AddComponent<SpriteRenderer>();
            petRenderer.sprite = WorldPlaceholderSprites.Circle;
            petRenderer.sortingOrder = 19;
            petObject.transform.localScale = Vector3.one * 0.72f;

            GameObject label = new GameObject("Name");
            label.transform.SetParent(petObject.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            nameText = label.AddComponent<TextMesh>();
            nameText.anchor = TextAnchor.MiddleCenter;
            nameText.alignment = TextAlignment.Center;
            nameText.characterSize = 0.11f;
            nameText.fontSize = 36;
            nameText.color = Color.white;

            GameObject rarityLabel = new GameObject("Rarity");
            rarityLabel.transform.SetParent(petObject.transform, false);
            rarityLabel.transform.localPosition = new Vector3(0f, 0.57f, 0f);
            rarityText = rarityLabel.AddComponent<TextMesh>();
            rarityText.anchor = TextAnchor.MiddleCenter;
            rarityText.alignment = TextAlignment.Center;
            rarityText.characterSize = 0.075f;
            rarityText.fontSize = 30;

            GameObject accessoryObject = new GameObject("Accessories");
            accessoryObject.transform.SetParent(petObject.transform, false);
            accessoryRoot = accessoryObject.transform;

            fullSetEffect = new GameObject("SignatureSetEffect");
            fullSetEffect.transform.SetParent(petObject.transform, false);
            fullSetEffect.transform.localScale = Vector3.one * 1.8f;
            SpriteRenderer effectRenderer = fullSetEffect.AddComponent<SpriteRenderer>();
            effectRenderer.sprite = WorldPlaceholderSprites.Ring;
            effectRenderer.color = new Color(0.55f, 0.86f, 1f, 0.55f);
            effectRenderer.sortingOrder = 18;
            fullSetEffect.SetActive(false);
            petObject.SetActive(false);
        }

        private void RefreshFavorite()
        {
            EnsureVisual();
            CreatureInstance favorite = collection != null ? collection.GetFavorite() : null;
            if (favorite == null)
            {
                displayedPetId = string.Empty;
                petObject.SetActive(false);
                return;
            }

            bool changedPet = favorite.uniqueId != displayedPetId;
            displayedPetId = favorite.uniqueId;
            petObject.SetActive(true);
            petRenderer.color = PetLocalization.RarityColor(favorite.rarity);
            CreatureData species = GameManager.Instance != null
                ? GameManager.Instance.GetCreature(favorite.speciesId)
                : null;
            nameText.text = favorite.GetDisplayName(species);
            rarityText.text = PetLocalization.Rarity(favorite.rarity);
            rarityText.color = PetLocalization.RarityColor(favorite.rarity);
            RebuildAccessoryVisuals(favorite);
            if (fullSetEffect != null)
            {
                fullSetEffect.SetActive(
                    signatureSets != null && signatureSets.HasFullSetVisual(favorite));
            }

            if (changedPet && player != null)
            {
                petObject.transform.position = player.transform.position -
                                               (Vector3)(player.LastDirection.normalized * followDistance);
            }
        }

        private void RebuildAccessoryVisuals(CreatureInstance favorite)
        {
            for (int index = accessoryRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(accessoryRoot.GetChild(index).gameObject);
            }

            List<AccessoryData> equipped = accessories != null
                ? accessories.GetEquippedAccessories(favorite.uniqueId)
                : new List<AccessoryData>();
            for (int index = 0; index < equipped.Count; index++)
            {
                AccessoryData accessory = equipped[index];
                GameObject visual = new GameObject(accessory.slot + "_" + accessory.id);
                visual.transform.SetParent(accessoryRoot, false);
                visual.transform.localPosition = accessory.visualOffset;
                Vector2 scale = accessory.visualScale == Vector2.zero
                    ? Vector2.one
                    : accessory.visualScale;
                visual.transform.localScale = new Vector3(scale.x, scale.y, 1f) * 0.34f;
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = accessory.icon != null
                    ? accessory.icon
                    : WorldPlaceholderSprites.Square;
                renderer.color = accessory.icon != null
                    ? Color.white
                    : PetLocalization.RarityColor(accessory.rarity);
                renderer.sortingOrder = 20 + index;
            }
        }
    }
}
