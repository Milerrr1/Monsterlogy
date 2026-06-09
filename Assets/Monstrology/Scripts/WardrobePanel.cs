using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class WardrobePanel : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private AccessoryInventoryManager inventory;
        private Transform content;
        private string selectedPetId;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            AccessoryInventoryManager inventoryManager,
            Transform contentRoot)
        {
            game = gameManager;
            collection = collectionManager;
            inventory = inventoryManager;
            content = contentRoot;
        }

        public void Rebuild()
        {
            UIFactory.ClearChildren(content);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(30, 30, 20, 30);
            layout.spacing = 10f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            IReadOnlyList<CreatureInstance> pets = collection.GetAllPets();
            if (pets.Count == 0)
            {
                UIFactory.Label(content, "Сначала добавьте питомца в коллекцию.", 20, 80f);
                return;
            }

            CreatureInstance selected = collection.GetPetById(selectedPetId);
            if (selected == null)
            {
                selected = pets[0];
                selectedPetId = selected.uniqueId;
            }

            Dropdown petDropdown = UIFactory.Dropdown("Pet", content);
            UIFactory.SetLayoutHeight(petDropdown.gameObject, 48f);
            List<string> petNames = new List<string>();
            int selectedIndex = 0;
            for (int index = 0; index < pets.Count; index++)
            {
                CreatureData species = game.GetCreature(pets[index].speciesId);
                petNames.Add(pets[index].GetDisplayName(species));
                if (pets[index].uniqueId == selectedPetId)
                {
                    selectedIndex = index;
                }
            }

            petDropdown.AddOptions(petNames);
            petDropdown.value = selectedIndex;
            petDropdown.onValueChanged.AddListener(index =>
            {
                selectedPetId = pets[Mathf.Clamp(index, 0, pets.Count - 1)].uniqueId;
                Rebuild();
            });

            Text slots = UIFactory.Label(
                content,
                PetDetailsPanel.BuildAccessoryText(selected, inventory),
                16,
                54f);
            slots.color = new Color(0.82f, 0.88f, 0.96f);

            IReadOnlyList<string> owned = inventory.GetOwnedAccessoryIds();
            if (owned.Count == 0)
            {
                UIFactory.Label(
                    content,
                    "Гардероб пока пуст. Ищите одежду во время исследования мира.",
                    18,
                    80f);
                return;
            }

            foreach (string accessoryId in owned)
            {
                AccessoryData accessory = game.GetAccessory(accessoryId);
                if (accessory != null)
                {
                    BuildWardrobeRow(selected, accessory);
                }
            }
        }

        private void BuildWardrobeRow(CreatureInstance pet, AccessoryData accessory)
        {
            bool equipped = pet.equippedAccessories != null &&
                            pet.equippedAccessories.Exists(entry =>
                                entry != null &&
                                entry.slot == accessory.slot &&
                                entry.equippedAccessoryId == accessory.id);
            bool allowed = accessory.IsAllowedFor(pet.speciesId);
            Image row = UIFactory.Image(accessory.id, content, new Color(0.09f, 0.12f, 0.18f));
            UIFactory.ApplyRounded(row);
            UIFactory.SetLayoutHeight(row.gameObject, 76f);

            Image icon = UIFactory.Image("Icon", row.transform, Color.white);
            UIFactory.SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(52f, 52f));
            icon.sprite = accessory.icon != null ? accessory.icon : WorldPlaceholderSprites.Square;
            icon.color = accessory.icon != null
                ? Color.white
                : PetLocalization.RarityColor(accessory.rarity);

            Text info = UIFactory.Text(
                "Info",
                row.transform,
                accessory.displayName + "\n" + accessory.slot + "  |  " +
                PetLocalization.Rarity(accessory.rarity),
                16,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);
            UIFactory.SetOffsets(info.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(82f, 8f), new Vector2(-190f, -8f));
            info.color = PetLocalization.RarityColor(accessory.rarity);

            Button action = UIFactory.Button(
                "Action",
                row.transform,
                equipped ? "СНЯТЬ" : allowed ? "НАДЕТЬ" : "НЕ ПОДХОДИТ",
                equipped ? new Color(0.62f, 0.34f, 0.32f) : new Color(0.25f, 0.56f, 0.42f));
            UIFactory.SetRect(action.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(156f, 44f));
            action.interactable = equipped || allowed;
            action.onClick.AddListener(() =>
            {
                if (equipped)
                {
                    inventory.UnequipAccessory(pet.uniqueId, accessory.slot);
                }
                else
                {
                    inventory.EquipAccessory(pet.uniqueId, accessory.id);
                }

                Rebuild();
            });
        }
    }
}
