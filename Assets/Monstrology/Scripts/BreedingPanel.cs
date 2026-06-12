using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class BreedingPanel : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private BreedingSystem breeding;
        private Transform content;
        private readonly List<string> speciesIds = new List<string>();
        private string selectedSpeciesId;
        private Text details;
        private Text result;
        private Button evolveButton;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            BreedingSystem breedingSystem,
            Transform contentRoot)
        {
            game = gameManager;
            collection = collectionManager;
            breeding = breedingSystem;
            content = contentRoot;
            if (game != null)
            {
                game.CreatureRegistered -= HandleCreatureRegistered;
                game.CreatureRegistered += HandleCreatureRegistered;
            }

            if (collection != null)
            {
                collection.CollectionChanged -= HandleCollectionChanged;
                collection.CollectionChanged += HandleCollectionChanged;
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.CreatureRegistered -= HandleCreatureRegistered;
            }

            if (collection != null)
            {
                collection.CollectionChanged -= HandleCollectionChanged;
            }
        }

        public void Rebuild()
        {
            UIFactory.ClearChildren(content);
            speciesIds.Clear();
            foreach (CreatureData creature in game.Content.creatures
                         .Where(creature => creature != null &&
                                            !string.IsNullOrEmpty(creature.id) &&
                                            (game.IsCreatureFound(creature.id) ||
                                             collection.GetPetBySpecies(creature.id) != null))
                         .OrderBy(creature => creature.creatureName))
            {
                if (!speciesIds.Contains(creature.id))
                {
                    speciesIds.Add(creature.id);
                }
            }

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

            UIFactory.Label(
                content,
                "Собирайте копии одного вида, чтобы открыть его следующую форму.",
                18,
                54f);

            if (speciesIds.Count == 0)
            {
                UIFactory.Label(
                    content,
                    "Пока нет открытых видов.\nСледы часто ведут к редким существам.",
                    20,
                    110f);
                return;
            }

            int selectedIndex = Mathf.Max(0, speciesIds.IndexOf(selectedSpeciesId));
            selectedSpeciesId = speciesIds[selectedIndex];

            Dropdown dropdown = UIFactory.Dropdown("Species", content);
            UIFactory.SetLayoutHeight(dropdown.gameObject, 48f);
            List<string> names = new List<string>();
            foreach (string speciesId in speciesIds)
            {
                CreatureData species = game.GetCreature(speciesId);
                names.Add(species != null ? species.creatureName : speciesId);
            }

            dropdown.AddOptions(names);
            dropdown.value = selectedIndex;
            dropdown.onValueChanged.AddListener(index =>
            {
                selectedSpeciesId = speciesIds[Mathf.Clamp(index, 0, speciesIds.Count - 1)];
                Refresh();
            });

            details = UIFactory.Label(content, string.Empty, 20, 150f);
            details.alignment = TextAnchor.MiddleCenter;
            details.color = new Color(0.86f, 0.91f, 0.98f);

            evolveButton = UIFactory.Button(
                "Evolve",
                content,
                "ЭВОЛЮЦИОНИРОВАТЬ",
                new Color(0.62f, 0.34f, 0.72f));
            UIFactory.SetLayoutHeight(evolveButton.gameObject, 58f);
            evolveButton.onClick.AddListener(PerformEvolution);

            result = UIFactory.Label(content, string.Empty, 18, 58f);
            result.alignment = TextAnchor.MiddleCenter;
            Refresh();
        }

        private void Refresh()
        {
            SpeciesEvolutionData evolution = game.GetEvolution(selectedSpeciesId);
            if (details == null)
            {
                return;
            }

            CreatureData current = game.GetCreature(selectedSpeciesId);
            int copies = game.GetCreatureCount(selectedSpeciesId);
            if (evolution == null || string.IsNullOrEmpty(evolution.resultSpeciesId))
            {
                details.text = "Текущий вид: " +
                               (current != null ? current.creatureName : selectedSpeciesId) +
                               "\nКопий собрано: " + copies +
                               "\nМаксимальная форма достигнута.";
                evolveButton.interactable = false;
                result.text = "Для этого вида следующая форма не настроена.";
                result.color = new Color(0.72f, 0.78f, 0.86f);
                return;
            }

            CreatureData next = game.GetCreature(evolution.resultSpeciesId);
            float progress = evolution.requiredCopies > 0
                ? Mathf.Clamp01((float)copies / evolution.requiredCopies)
                : 1f;
            details.text = "Текущий вид: " + (current != null ? current.creatureName : selectedSpeciesId) +
                           "\nКоличество экземпляров: " + copies + " / " + evolution.requiredCopies +
                           "\nСледующая форма: " +
                           (next != null ? next.creatureName : "данные формы отсутствуют") +
                           "\nПрогресс: " + Mathf.RoundToInt(progress * 100f) + "%";

            string reason;
            evolveButton.interactable = breeding.CanEvolve(selectedSpeciesId, out reason);
            result.text = evolveButton.interactable ? "Эволюция доступна." : reason;
            result.color = evolveButton.interactable
                ? new Color(0.48f, 0.95f, 0.62f)
                : new Color(1f, 0.68f, 0.48f);
        }

        private void PerformEvolution()
        {
            CreatureInstance evolved = breeding.EvolveSpecies(selectedSpeciesId);
            result.text = breeding.LastMessage;
            result.color = evolved != null
                ? new Color(0.48f, 0.95f, 0.62f)
                : new Color(1f, 0.62f, 0.48f);
            Rebuild();
        }

        private void HandleCreatureRegistered(CreatureData creature, bool firstDiscovery)
        {
            RebuildIfVisible();
        }

        private void HandleCollectionChanged()
        {
            RebuildIfVisible();
        }

        private void RebuildIfVisible()
        {
            if (content != null && content.gameObject.activeInHierarchy)
            {
                Rebuild();
            }
        }
    }
}
