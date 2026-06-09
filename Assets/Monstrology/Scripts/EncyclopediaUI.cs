using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class EncyclopediaUI : MonoBehaviour
    {
        private GameManager game;
        private Transform content;

        public void Initialize(GameManager gameManager, Transform contentRoot)
        {
            game = gameManager;
            content = contentRoot;
        }

        public void Rebuild()
        {
            UIFactory.ClearChildren(content);
            GridLayoutGroup grid = content.gameObject.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.padding = new RectOffset(16, 16, 16, 24);
                grid.spacing = new Vector2(12f, 12f);
                grid.cellSize = new Vector2(210f, 258f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
                grid.childAlignment = TextAnchor.UpperCenter;
            }

            foreach (CreatureData creature in game.Content.creatures)
            {
                BuildCard(creature);
            }
        }

        private void BuildCard(CreatureData creature)
        {
            bool found = game.IsCreatureFound(creature.id);
            Image card = UIFactory.Image(creature.id, content, new Color(0.09f, 0.12f, 0.18f));
            UIFactory.ApplyRounded(card);
            UIFactory.AddSoftShadow(card.gameObject);
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = found
                ? Localization.RarityColor(creature.rarity)
                : new Color(0.22f, 0.25f, 0.31f);
            outline.effectDistance = new Vector2(2f, -2f);

            Image portrait = UIFactory.Image("Portrait", card.transform,
                found ? Color.white : new Color(0.02f, 0.025f, 0.04f));
            UIFactory.SetRect(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(126f, 108f));
            portrait.sprite = found
                ? creature.icon != null ? creature.icon : WorldPlaceholderSprites.Circle
                : WorldPlaceholderSprites.Ring;
            portrait.color = found && creature.icon == null
                ? Localization.RarityColor(creature.rarity)
                : found ? Color.white : new Color(0.22f, 0.25f, 0.32f);
            portrait.preserveAspect = true;

            Text name = UIFactory.Text("Name", card.transform, found ? creature.creatureName : "???",
                18, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(-16f, 30f));
            name.color = found ? Localization.RarityColor(creature.rarity) : new Color(0.55f, 0.58f, 0.64f);

            string details;
            if (found)
            {
                details = Localization.Rarity(creature.rarity) + " • " + Localization.Biome(creature.biome) +
                          "\n" + creature.description +
                          "\n\n" + game.GetPseudoOnlineText(creature);
            }
            else
            {
                if (game.IsHintPurchased(creature.id))
                {
                    string hint = BuildAppearanceHint(creature);
                    details = "Силуэт не опознан.\n\nПодсказка: " + hint;
                }
                else
                {
                    details = "Силуэт не опознан.\n\nПодсказка пока скрыта.";
                }
            }

            Text info = UIFactory.Text("Info", card.transform, details, 12, FontStyle.Normal, TextAnchor.UpperCenter);
            UIFactory.SetOffsets(info.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, found || game.IsHintPurchased(creature.id) ? 10f : 48f),
                new Vector2(-10f, -164f));
            info.color = found ? new Color(0.82f, 0.85f, 0.91f) : new Color(0.54f, 0.57f, 0.63f);

            if (!found && !game.IsHintPurchased(creature.id))
            {
                Button hintButton = UIFactory.Button("BuyHint", card.transform, "ПОДСКАЗКА  15",
                    new Color(0.48f, 0.34f, 0.17f));
                UIFactory.SetRect(hintButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 8f), new Vector2(174f, 34f));
                hintButton.onClick.AddListener(delegate
                {
                    if (game.TryBuyHint(creature.id, 15))
                    {
                        Rebuild();
                    }
                });
            }
        }

        private static string BuildAppearanceHint(CreatureData creature)
        {
            if (creature.allowedTimes != null && creature.allowedTimes.Count > 0)
            {
                return "Время появления: " +
                       string.Join(", ", creature.allowedTimes.ConvertAll(
                           WorldEnvironmentSystem.Localize));
            }

            if (creature.allowedWeather != null && creature.allowedWeather.Count > 0)
            {
                return "Погода: " +
                       string.Join(", ", creature.allowedWeather.ConvertAll(
                           WorldEnvironmentSystem.Localize));
            }

            return creature.appearanceConditions.Count > 0
                ? creature.appearanceConditions[0].GetHint()
                : "Ищите в биоме: " + Localization.Biome(creature.biome);
        }
    }
}
