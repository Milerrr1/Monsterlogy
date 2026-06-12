using System;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class EncyclopediaUI : MonoBehaviour
    {
        private GameManager game;
        private Transform content;
        private CreatureNestSystem nests;

        public void Initialize(GameManager gameManager, Transform contentRoot)
        {
            game = gameManager;
            content = contentRoot;
            nests = FindObjectOfType<CreatureNestSystem>();
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
                grid.cellSize = new Vector2(210f, 320f);
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
                ? SpriteDatabase.Active.GetCreaturePortrait(creature)
                : SpriteDatabase.Active.GetUnknownCreature();
            portrait.color = found && !SpriteDatabase.Active.HasCreatureArtwork(creature)
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
                          "\n\n" + game.GetPseudoOnlineText(creature) +
                          BuildNestStatus(creature);
            }
            else
            {
                int hintLevel = game.GetHintLevel(creature.id);
                if (hintLevel > 0)
                {
                    details = "Силуэт не опознан.\n\n" +
                              BuildProgressiveHint(creature, hintLevel);
                }
                else
                {
                    details =
                        "Силуэт не опознан.\n\nПервая подсказка назовёт только биом.";
                }
            }

            Text info = UIFactory.Text("Info", card.transform, details, 11, FontStyle.Normal, TextAnchor.UpperCenter);
            UIFactory.SetOffsets(info.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, found || game.GetHintLevel(creature.id) >= 4 ? 10f : 48f),
                new Vector2(-10f, -164f));
            info.color = found ? new Color(0.82f, 0.85f, 0.91f) : new Color(0.54f, 0.57f, 0.63f);

            int currentHintLevel = game.GetHintLevel(creature.id);
            if (!found && currentHintLevel < 4)
            {
                int cost = game.GetNextHintCost(creature);
                Button hintButton = UIFactory.Button(
                    "BuyHint",
                    card.transform,
                    "ПОДСКАЗКА " + (currentHintLevel + 1) + "/4  •  " + cost,
                    new Color(0.48f, 0.34f, 0.17f));
                UIFactory.SetRect(hintButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 8f), new Vector2(192f, 34f));
                hintButton.onClick.AddListener(delegate
                {
                    if (game.TryBuyNextHint(creature.id))
                    {
                        Rebuild();
                    }
                });
            }
        }

        private string BuildNestStatus(CreatureData creature)
        {
            CreatureNestProgress nest = nests != null
                ? nests.GetNestForSpecies(creature.id)
                : null;
            if (nest == null)
            {
                return string.Empty;
            }

            TimeSpan remaining = nests.GetTimeUntilReady(nest);
            return "\nЛоговище: уровень " + nest.level +
                   (remaining <= TimeSpan.Zero
                       ? " • награда готова"
                       : " • " + Mathf.CeilToInt((float)remaining.TotalMinutes) + " мин.");
        }

        private string BuildProgressiveHint(CreatureData creature, int level)
        {
            string text = "Уровень подсказки " + Mathf.Clamp(level, 1, 4) + "/4";
            if (level >= 1)
            {
                text += "\nБиом: " + Localization.Biome(creature.biome) + ".";
            }

            if (level >= 2)
            {
                text += "\nВремя: " +
                        (creature.allowedTimes != null &&
                         creature.allowedTimes.Count > 0
                            ? string.Join(", ", creature.allowedTimes.ConvertAll(
                                WorldEnvironmentSystem.Localize))
                            : "может появиться в разное время") + ".";
            }

            if (level >= 3)
            {
                text += "\nПогода: " +
                        (creature.allowedWeather != null &&
                         creature.allowedWeather.Count > 0
                            ? string.Join(", ", creature.allowedWeather.ConvertAll(
                                WorldEnvironmentSystem.Localize))
                            : "особых ограничений нет") + ".";
            }

            if (level >= 4)
            {
                text += "\nСпособ: " + BuildSearchMethod(creature) + ".";
            }

            return text;
        }

        private string BuildSearchMethod(CreatureData creature)
        {
            SpeciesEvolutionData evolution = game.Content.speciesEvolutions.Find(entry =>
                entry != null && entry.resultSpeciesId == creature.id);
            if (evolution != null)
            {
                return "открывается через эволюцию предыдущей формы";
            }

            bool hasNest = game.Content.creatureNests.Exists(nest =>
                nest != null && nest.speciesId == creature.id);
            if (hasNest)
            {
                return "цепочка следов может привести к его логову";
            }

            bool hasEvent = game.Content.biomeEvents.Exists(eventData =>
                eventData != null && eventData.biome == creature.biome);
            if (hasEvent && creature.rarity != CreatureRarity.Common)
            {
                return "исследуйте биом во время редкого события";
            }

            return creature.appearanceConditions != null &&
                   creature.appearanceConditions.Count > 0
                ? creature.appearanceConditions[0].GetHint()
                : "исследуйте биом и проверяйте цепочки следов";
        }
    }
}
