using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class PetUpgradePanel : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private PetUpgradeSystem upgrades;
        private Transform content;
        private readonly List<CreatureInstance> pets = new List<CreatureInstance>();
        private string selectedPetId;
        private Text details;
        private Text message;
        private Button upgradeButton;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            PetUpgradeSystem upgradeSystem,
            Transform contentRoot)
        {
            game = gameManager;
            collection = collectionManager;
            upgrades = upgradeSystem;
            content = contentRoot;
        }

        public void Rebuild()
        {
            UIFactory.ClearChildren(content);
            pets.Clear();
            pets.AddRange(collection.GetAllPets());

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(100, 100, 28, 36);
            layout.spacing = 14f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            if (pets.Count == 0)
            {
                UIFactory.Label(content, "Сначала добавьте питомца в коллекцию.", 20, 80f);
                return;
            }

            int selectedIndex = Mathf.Max(0, pets.FindIndex(pet => pet.uniqueId == selectedPetId));
            selectedPetId = pets[selectedIndex].uniqueId;

            Dropdown dropdown = UIFactory.Dropdown("Pet", content);
            UIFactory.SetLayoutHeight(dropdown.gameObject, 48f);
            List<string> names = new List<string>();
            foreach (CreatureInstance pet in pets)
            {
                CreatureData species = game.GetCreature(pet.speciesId);
                names.Add(pet.GetDisplayName(species) + "  |  ур. " + pet.level);
            }

            dropdown.AddOptions(names);
            dropdown.value = selectedIndex;
            dropdown.onValueChanged.AddListener(index =>
            {
                selectedPetId = pets[Mathf.Clamp(index, 0, pets.Count - 1)].uniqueId;
                Refresh();
            });

            details = UIFactory.Label(content, string.Empty, 20, 132f);
            details.alignment = TextAnchor.MiddleCenter;
            details.color = new Color(0.86f, 0.91f, 0.98f);

            upgradeButton = UIFactory.Button(
                "Upgrade",
                content,
                "УЛУЧШИТЬ",
                new Color(0.25f, 0.62f, 0.46f));
            UIFactory.SetLayoutHeight(upgradeButton.gameObject, 58f);
            upgradeButton.onClick.AddListener(PerformUpgrade);

            message = UIFactory.Label(content, string.Empty, 18, 58f);
            message.alignment = TextAnchor.MiddleCenter;
            Refresh();
        }

        private void Refresh()
        {
            CreatureInstance pet = collection.GetPetById(selectedPetId);
            if (pet == null || details == null)
            {
                return;
            }

            ItemData resource = upgrades.GetRequiredResource(pet);
            int required = upgrades.GetRequiredResourceCount(pet);
            int owned = resource != null ? game.GetItemCount(resource.id) : 0;
            details.text = "Текущий уровень: " + pet.level + " / " + PetUpgradeSystem.MaxLevel +
                           "\nНужный ресурс: " + (resource != null ? resource.itemName : "не назначен") +
                           "\nВ наличии: " + owned + " / " + required;

            string reason;
            upgradeButton.interactable = upgrades.CanUpgrade(pet, out reason);
            if (!upgradeButton.interactable)
            {
                message.text = reason;
                message.color = new Color(1f, 0.68f, 0.48f);
            }
            else
            {
                message.text = "Ресурсов достаточно для повышения уровня.";
                message.color = new Color(0.48f, 0.95f, 0.62f);
            }
        }

        private void PerformUpgrade()
        {
            CreatureInstance pet = collection.GetPetById(selectedPetId);
            bool upgraded = upgrades.UpgradePet(pet);
            message.text = upgrades.LastMessage;
            message.color = upgraded
                ? new Color(0.48f, 0.95f, 0.62f)
                : new Color(1f, 0.62f, 0.48f);
            Refresh();
        }
    }
}
