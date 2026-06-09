using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class PetsPanel : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private AccessoryInventoryManager accessoryInventory;
        private PetUpgradeSystem upgrades;
        private Transform content;
        private string selectedPetId;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            Transform contentRoot,
            AccessoryInventoryManager inventoryManager = null,
            PetUpgradeSystem upgradeSystem = null)
        {
            game = gameManager;
            collection = collectionManager;
            accessoryInventory = inventoryManager;
            upgrades = upgradeSystem;
            content = contentRoot;

            if (collection != null)
            {
                collection.CollectionChanged -= HandleCollectionChanged;
                collection.CollectionChanged += HandleCollectionChanged;
            }
        }

        private void OnDestroy()
        {
            if (collection != null)
            {
                collection.CollectionChanged -= HandleCollectionChanged;
            }
        }

        public void Rebuild()
        {
            if (content == null || collection == null)
            {
                return;
            }

            UIFactory.ClearChildren(content);
            EnsureLayout();

            if (collection.Count == 0)
            {
                Text empty = UIFactory.Label(
                    content,
                    "У вас пока нет питомцев.\nПри первом открытии нового вида можно предложить ему стать питомцем.",
                    20,
                    140f);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = new Color(0.76f, 0.82f, 0.9f);
                return;
            }

            CreatureInstance selected = collection.GetPetById(selectedPetId);
            if (selected == null)
            {
                selected = collection.GetAllPets()[0];
                selectedPetId = selected.uniqueId;
            }

            BuildSelectedCard(selected);
            Text heading = UIFactory.Label(content, "ВСЕ ПИТОМЦЫ", 18, 38f);
            heading.fontStyle = FontStyle.Bold;
            heading.color = new Color(1f, 0.82f, 0.34f);

            foreach (CreatureInstance pet in collection.GetAllPets())
            {
                BuildPetRow(pet);
            }
        }

        private void EnsureLayout()
        {
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(26, 26, 18, 28);
            layout.spacing = 12f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
        }

        private void BuildSelectedCard(CreatureInstance pet)
        {
            CreatureData species = game.GetCreature(pet.speciesId);
            Image card = UIFactory.Image("SelectedPet", content, new Color(0.1f, 0.14f, 0.21f));
            UIFactory.ApplyRounded(card);
            UIFactory.AddSoftShadow(card.gameObject);
            UIFactory.SetLayoutHeight(card.gameObject, 360f);

            Image portrait = UIFactory.Image("Portrait", card.transform, Color.white);
            UIFactory.SetRect(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(132f, 132f));
            portrait.sprite = species != null && species.icon != null
                ? species.icon
                : WorldPlaceholderSprites.Circle;
            portrait.color = species != null && species.icon != null
                ? Color.white
                : PetLocalization.RarityColor(pet.rarity);
            portrait.preserveAspect = true;
            AccessoryVisualUtility.BuildUiVisuals(portrait.transform, pet, accessoryInventory);

            string favorite = pet.isFavorite ? "ЛЮБИМЧИК  |  " : "";
            Text name = UIFactory.Text(
                "PetName",
                card.transform,
                favorite + pet.GetDisplayName(species),
                24,
                FontStyle.Bold,
                TextAnchor.UpperLeft);
            UIFactory.SetRect(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(88f, -18f), new Vector2(-210f, 36f));
            name.color = PetLocalization.RarityColor(pet.rarity);

            string speciesName = species != null ? species.creatureName : pet.speciesId;
            Text details = UIFactory.Text(
                "Details",
                card.transform,
                "Вид: " + speciesName +
                "\nУровень: " + pet.level + " / " + PetUpgradeSystem.MaxLevel +
                "\nОпыт: " + pet.experience +
                "\nКопий собрано: " + game.GetCreatureCount(pet.speciesId) +
                "\nРедкость: " + PetLocalization.Rarity(pet.rarity) +
                "\nХарактер: " + PetLocalization.Personality(pet.personalityType) +
                "\nВозраст: " + FormatAge(pet.ageInDays),
                15,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            UIFactory.SetOffsets(details.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(178f, 96f), new Vector2(-250f, -58f));
            details.color = new Color(0.84f, 0.88f, 0.94f);

            Text genes = UIFactory.Text(
                "Genes",
                card.transform,
                PetDetailsPanel.BuildGeneticsText(pet) + "\n" +
                PetDetailsPanel.BuildAccessoryText(pet, accessoryInventory),
                14,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            UIFactory.SetOffsets(genes.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(24f, 18f), new Vector2(-250f, -252f));
            genes.color = new Color(0.72f, 0.88f, 0.92f);

            InputField renameInput = UIFactory.InputField(
                "RenameInput",
                card.transform,
                pet.customName,
                "Имя питомца");
            UIFactory.SetRect(renameInput.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-22f, -26f), new Vector2(220f, 42f));

            Button rename = UIFactory.Button("Rename", card.transform, "СОХРАНИТЬ ИМЯ",
                new Color(0.26f, 0.52f, 0.72f));
            UIFactory.SetRect(rename.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-22f, 86f), new Vector2(220f, 42f));
            rename.onClick.AddListener(delegate
            {
                collection.RenamePet(pet.uniqueId, renameInput.text);
            });

            ItemData resource = upgrades != null ? upgrades.GetRequiredResource(pet) : null;
            int required = upgrades != null ? upgrades.GetRequiredResourceCount(pet) : 0;
            int owned = resource != null ? game.GetItemCount(resource.id) : 0;
            Text upgradeInfo = UIFactory.Text(
                "UpgradeInfo",
                card.transform,
                "Ресурс: " + (resource != null ? resource.itemName : "не назначен") +
                "\nВ наличии: " + owned + " / " + required,
                14,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);
            UIFactory.SetRect(
                upgradeInfo.rectTransform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-22f, 20f),
                new Vector2(220f, 62f));
            upgradeInfo.color = new Color(0.86f, 0.91f, 0.98f);

            string upgradeReason;
            bool canUpgrade = upgrades != null && upgrades.CanUpgrade(pet, out upgradeReason);
            Button upgradeButton = UIFactory.Button(
                "Upgrade",
                card.transform,
                pet.level >= PetUpgradeSystem.MaxLevel ? "МАКС. УРОВЕНЬ" : "ПОВЫСИТЬ УРОВЕНЬ",
                new Color(0.25f, 0.62f, 0.46f));
            UIFactory.SetRect(
                upgradeButton.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-22f, -42f),
                new Vector2(220f, 44f));
            upgradeButton.interactable = canUpgrade;
            upgradeButton.onClick.AddListener(() =>
            {
                bool upgraded = upgrades.UpgradePet(pet);
                game.RaiseNotification(upgrades.LastMessage);
                if (upgraded)
                {
                    Rebuild();
                }
            });

            Button favoriteButton = UIFactory.Button(
                "Favorite",
                card.transform,
                pet.isFavorite ? "УЖЕ ЛЮБИМЧИК" : "СДЕЛАТЬ ЛЮБИМЧИКОМ",
                pet.isFavorite
                    ? new Color(0.42f, 0.5f, 0.3f)
                    : new Color(0.7f, 0.46f, 0.2f));
            UIFactory.SetRect(favoriteButton.GetComponent<RectTransform>(),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-22f, 18f), new Vector2(220f, 42f));
            favoriteButton.interactable = !pet.isFavorite;
            favoriteButton.onClick.AddListener(delegate
            {
                collection.SetFavorite(pet.uniqueId);
            });
        }

        private void BuildPetRow(CreatureInstance pet)
        {
            CreatureData species = game.GetCreature(pet.speciesId);
            bool selected = pet.uniqueId == selectedPetId;
            Image row = UIFactory.Image(
                pet.uniqueId,
                content,
                selected ? new Color(0.15f, 0.22f, 0.31f) : new Color(0.085f, 0.11f, 0.17f));
            UIFactory.ApplyRounded(row);
            UIFactory.SetLayoutHeight(row.gameObject, 92f);

            Image portrait = UIFactory.Image("Portrait", row.transform, Color.white);
            UIFactory.SetRect(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(66f, 66f));
            portrait.sprite = species != null && species.icon != null
                ? species.icon
                : WorldPlaceholderSprites.Circle;
            portrait.color = species != null && species.icon != null
                ? Color.white
                : PetLocalization.RarityColor(pet.rarity);
            portrait.preserveAspect = true;
            AccessoryVisualUtility.BuildUiVisuals(portrait.transform, pet, accessoryInventory);

            Text title = UIFactory.Text(
                "Name",
                row.transform,
                (pet.isFavorite ? "ЛЮБИМЧИК  |  " : "") + pet.GetDisplayName(species),
                18,
                FontStyle.Bold,
                TextAnchor.UpperLeft);
            UIFactory.SetOffsets(title.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(98f, 47f), new Vector2(-170f, -10f));
            title.color = PetLocalization.RarityColor(pet.rarity);

            string speciesName = species != null ? species.creatureName : pet.speciesId;
            Text details = UIFactory.Text(
                "Details",
                row.transform,
                speciesName + "  |  Ур. " + pet.level + "  |  " +
                "Копий: " + game.GetCreatureCount(pet.speciesId) + "  |  " +
                PetLocalization.Rarity(pet.rarity) + "  |  " +
                PetLocalization.Personality(pet.personalityType) + "  |  " +
                FormatAge(pet.ageInDays),
                14,
                FontStyle.Normal,
                TextAnchor.LowerLeft);
            UIFactory.SetOffsets(details.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(98f, 14f), new Vector2(-170f, -45f));
            details.color = new Color(0.76f, 0.81f, 0.89f);

            Button select = UIFactory.Button(
                "Select",
                row.transform,
                selected ? "ВЫБРАН" : "КАРТОЧКА",
                selected ? new Color(0.3f, 0.4f, 0.5f) : new Color(0.24f, 0.5f, 0.4f));
            UIFactory.SetRect(select.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(138f, 48f));
            select.interactable = !selected;
            select.onClick.AddListener(delegate
            {
                selectedPetId = pet.uniqueId;
                Rebuild();
            });
        }

        private void HandleCollectionChanged()
        {
            if (content != null && content.gameObject.activeInHierarchy)
            {
                Rebuild();
            }
        }

        private static string FormatAge(int days)
        {
            if (days == 1)
            {
                return "1 день";
            }

            if (days >= 2 && days <= 4)
            {
                return days + " дня";
            }

            return days + " дней";
        }
    }
}
