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

            HorizontalLayoutGroup cardLayout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 18f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = true;
            cardLayout.childForceExpandWidth = false;

            Transform left = CreateColumn(card.transform, "PortraitColumn", 150f, 0f);
            Transform center = CreateColumn(card.transform, "InfoColumn", 280f, 1f);
            Transform right = CreateColumn(card.transform, "ActionsColumn", 220f, 0f);

            Image portrait = UIFactory.Image("Portrait", left, Color.white);
            SetLayoutSize(portrait.gameObject, 140f, 140f, 0f);
            portrait.sprite = SpriteDatabase.Active.GetCreaturePortrait(species);
            portrait.color = SpriteDatabase.Active.HasCreatureArtwork(species)
                ? Color.white
                : PetLocalization.RarityColor(pet.rarity);
            portrait.preserveAspect = true;
            AccessoryVisualUtility.BuildUiVisuals(portrait.transform, pet, accessoryInventory);

            Text name = UIFactory.Text(
                "PetName",
                center,
                pet.GetDisplayName(species),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            UIFactory.SetLayoutHeight(name.gameObject, 38f);
            name.color = PetLocalization.RarityColor(pet.rarity);

            string speciesName = species != null ? species.creatureName : pet.speciesId;
            Text details = UIFactory.Text(
                "Details",
                center,
                "Вид: " + speciesName +
                "\nУровень: " + pet.level + " / " + PetUpgradeSystem.MaxLevel +
                "\nОпыт: " + pet.experience +
                "\nКопий собрано: " + game.GetCreatureCount(pet.speciesId) +
                "\nРедкость: " + PetLocalization.Rarity(pet.rarity) +
                "\nХарактер: " + PetLocalization.Personality(pet.personalityType),
                15,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            UIFactory.SetLayoutHeight(details.gameObject, 126f);
            details.color = new Color(0.84f, 0.88f, 0.94f);

            Text genes = UIFactory.Text(
                "Genes",
                left,
                PetDetailsPanel.BuildGeneticsText(pet) + "\n" +
                PetDetailsPanel.BuildAccessoryText(pet, accessoryInventory),
                12,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            UIFactory.SetLayoutHeight(genes.gameObject, 156f);
            genes.color = new Color(0.72f, 0.88f, 0.92f);

            ItemData resource = upgrades != null ? upgrades.GetRequiredResource(pet) : null;
            int required = upgrades != null ? upgrades.GetRequiredResourceCount(pet) : 0;
            int owned = resource != null ? game.GetItemCount(resource.id) : 0;
            Text upgradeInfo = UIFactory.Text(
                "UpgradeInfo",
                center,
                resource != null
                    ? "Ресурс: " + resource.itemName +
                      "\nВ наличии: " + owned + " / Нужно: " + required
                    : "Прокачка для этого питомца пока недоступна.",
                14,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);
            UIFactory.SetLayoutHeight(upgradeInfo.gameObject, 52f);
            upgradeInfo.color = resource != null
                ? new Color(0.86f, 0.91f, 0.98f)
                : new Color(0.72f, 0.76f, 0.84f);

            GameObject renameRow = UIFactory.Object("RenameRow", center);
            UIFactory.SetLayoutHeight(renameRow, 44f);
            HorizontalLayoutGroup renameLayout = renameRow.AddComponent<HorizontalLayoutGroup>();
            renameLayout.spacing = 8f;
            renameLayout.childControlHeight = true;
            renameLayout.childControlWidth = true;
            renameLayout.childForceExpandHeight = true;
            renameLayout.childForceExpandWidth = false;

            InputField renameInput = UIFactory.InputField(
                "RenameInput",
                renameRow.transform,
                pet.customName,
                "Имя");
            SetLayoutSize(renameInput.gameObject, 130f, 44f, 1f);

            Button rename = UIFactory.Button("Rename", renameRow.transform, "СОХРАНИТЬ",
                new Color(0.26f, 0.52f, 0.72f));
            SetLayoutSize(rename.gameObject, 118f, 44f, 0f);
            rename.onClick.AddListener(delegate
            {
                collection.RenamePet(pet.uniqueId, renameInput.text);
            });

            if (pet.isFavorite)
            {
                Text favoriteStatus = UIFactory.Text(
                    "FavoriteStatus",
                    right,
                    "УЖЕ ЛЮБИМЧИК",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                UIFactory.SetLayoutHeight(favoriteStatus.gameObject, 48f);
                favoriteStatus.color = new Color(0.72f, 0.86f, 0.48f);
            }
            else
            {
                Button favoriteButton = UIFactory.Button(
                    "Favorite",
                    right,
                    "СДЕЛАТЬ ЛЮБИМЧИКОМ",
                    new Color(0.7f, 0.46f, 0.2f));
                UIFactory.SetLayoutHeight(favoriteButton.gameObject, 48f);
                favoriteButton.onClick.AddListener(delegate
                {
                    collection.SetFavorite(pet.uniqueId);
                });
            }

            string upgradeReason = string.Empty;
            bool canUpgrade = upgrades != null && upgrades.CanUpgrade(pet, out upgradeReason);
            Button upgradeButton = UIFactory.Button(
                "Upgrade",
                right,
                pet.level >= PetUpgradeSystem.MaxLevel ? "МАКС. УРОВЕНЬ" : "ПОВЫСИТЬ УРОВЕНЬ",
                new Color(0.25f, 0.62f, 0.46f));
            UIFactory.SetLayoutHeight(upgradeButton.gameObject, 52f);
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

            Text upgradeStatus = UIFactory.Text(
                "UpgradeStatus",
                right,
                canUpgrade
                    ? "Ресурсов достаточно."
                    : resource == null
                        ? "Прокачка пока недоступна."
                        : upgradeReason,
                13,
                FontStyle.Normal,
                TextAnchor.UpperCenter);
            UIFactory.SetLayoutHeight(upgradeStatus.gameObject, 82f);
            upgradeStatus.color = canUpgrade
                ? new Color(0.52f, 0.9f, 0.64f)
                : new Color(0.82f, 0.78f, 0.72f);
        }

        private static Transform CreateColumn(
            Transform parent,
            string name,
            float preferredWidth,
            float flexibleWidth)
        {
            GameObject column = UIFactory.Object(name, parent);
            LayoutElement element = column.AddComponent<LayoutElement>();
            element.minWidth = preferredWidth;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;

            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return column.transform;
        }

        private static void SetLayoutSize(
            GameObject target,
            float preferredWidth,
            float preferredHeight,
            float flexibleWidth)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.minWidth = preferredWidth;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
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
            portrait.sprite = SpriteDatabase.Active.GetCreaturePortrait(species);
            portrait.color = SpriteDatabase.Active.HasCreatureArtwork(species)
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
