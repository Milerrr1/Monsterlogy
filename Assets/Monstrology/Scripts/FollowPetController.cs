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
        private CreatureVisualRig visualRig;
        private TextMesh nameText;
        private GameObject fullSetEffect;
        private WorldYSorter ySorter;
        private Vector3 velocity;
        private string displayedPetId;

        public bool HasWorldRarityLabel
        {
            get
            {
                return petObject != null &&
                       petObject.transform.Find("Rarity") != null;
            }
        }

        public string DisplayedName
        {
            get { return nameText != null ? nameText.text : string.Empty; }
        }

        public CreatureVisualRig VisualRig
        {
            get { return visualRig; }
        }

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
            visualRig = petObject.AddComponent<CreatureVisualRig>();
            visualRig.EnsureStructure();
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
            MonstrologyFontProvider.Apply(nameText);

            fullSetEffect = new GameObject("SignatureSetEffect");
            fullSetEffect.transform.SetParent(petObject.transform, false);
            fullSetEffect.transform.localScale = Vector3.one * 1.8f;
            SpriteRenderer effectRenderer = fullSetEffect.AddComponent<SpriteRenderer>();
            effectRenderer.sprite = SpriteDatabase.Active.GetSignatureSetEffect();
            effectRenderer.color = new Color(0.55f, 0.86f, 1f, 0.55f);
            effectRenderer.sortingOrder = 18;
            fullSetEffect.SetActive(false);
            WorldShadowUtility.EnsureShadow(
                petObject.transform,
                new Vector2(0.58f, 0.12f),
                0.2f);
            ySorter = petObject.AddComponent<WorldYSorter>();
            ySorter.Configure(true);
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
            CreatureData species = GameManager.Instance != null
                ? GameManager.Instance.GetCreature(favorite.speciesId)
                : null;
            visualRig.ApplyCreature(
                species,
                PetLocalization.RarityColor(favorite.rarity));
            nameText.text = favorite.GetDisplayName(species);
            RebuildAccessoryVisuals(favorite);
            if (fullSetEffect != null)
            {
                fullSetEffect.SetActive(
                    signatureSets != null && signatureSets.HasFullSetVisual(favorite));
            }

            if (ySorter != null)
            {
                ySorter.RefreshRenderers();
                ySorter.ApplyNow();
            }

            if (changedPet && player != null)
            {
                petObject.transform.position = player.transform.position -
                                               (Vector3)(player.LastDirection.normalized * followDistance);
            }
        }

        private void RebuildAccessoryVisuals(CreatureInstance favorite)
        {
            List<AccessoryData> equipped = accessories != null
                ? accessories.GetEquippedAccessories(favorite.uniqueId)
                : new List<AccessoryData>();
            visualRig.ApplyAccessories(equipped);
            if (ySorter != null)
            {
                ySorter.RefreshRenderers();
            }
        }
    }
}
