using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class PetsPanel : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private Transform content;
        private string selectedPetId;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            Transform contentRoot)
        {
            game = gameManager;
            collection = collectionManager;
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
            UIFactory.SetLayoutHeight(card.gameObject, 190f);

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
            int nextLevel = collection.GetExperienceForNextLevel(pet);
            Text details = UIFactory.Text(
                "Details",
                card.transform,
                "Вид: " + speciesName +
                "\nУровень: " + pet.level + "   Опыт: " + pet.experience + "/" + nextLevel +
                "\nРедкость: " + PetLocalization.Rarity(pet.rarity) +
                "\nХарактер: " + PetLocalization.Personality(pet.personalityType) +
                "\nВозраст: " + FormatAge(pet.ageInDays),
                15,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            UIFactory.SetOffsets(details.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(178f, 20f), new Vector2(-250f, -58f));
            details.color = new Color(0.84f, 0.88f, 0.94f);

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
                new Vector2(1f, 0.5f), new Vector2(-22f, 10f), new Vector2(220f, 42f));
            rename.onClick.AddListener(delegate
            {
                collection.RenamePet(pet.uniqueId, renameInput.text);
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
