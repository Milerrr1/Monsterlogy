using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class BiomeUI : MonoBehaviour
    {
        private GameManager game;
        private Transform content;
        private Action closeAction;
        private CreatureNestSystem nests;

        public void Initialize(GameManager gameManager, Transform contentRoot, Action onBiomeSelected)
        {
            game = gameManager;
            content = contentRoot;
            closeAction = onBiomeSelected;
            nests = FindObjectOfType<CreatureNestSystem>();
        }

        public void Rebuild()
        {
            UIFactory.ClearChildren(content);
            VerticalLayoutGroup layout = content.gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(28, 28, 16, 28);
                layout.spacing = 10f;
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }

            foreach (BiomeData biome in game.Content.biomes)
            {
                BuildBiomeRow(biome);
            }
        }

        private void BuildBiomeRow(BiomeData biome)
        {
            bool unlocked = game.IsBiomeUnlocked(biome.type);
            bool current = game.CurrentBiome == biome;
            Image row = UIFactory.Image(biome.type.ToString(), content, new Color(0.09f, 0.12f, 0.18f));
            UIFactory.ApplyRounded(row);
            UIFactory.AddSoftShadow(row.gameObject);
            UIFactory.SetLayoutHeight(row.gameObject, 154f);

            Image colorStrip = UIFactory.Image("Color", row.transform, biome.fallbackColor);
            UIFactory.SetRect(colorStrip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(12f, 0f));

            Text title = UIFactory.Text("Name", row.transform,
                biome.biomeName + (current ? "  • ТЕКУЩИЙ" : ""), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            UIFactory.SetOffsets(title.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(26f, 106f), new Vector2(-220f, -10f));
            title.color = current ? new Color(1f, 0.82f, 0.3f) : Color.white;

            Text description = UIFactory.Text("Description", row.transform, biome.description,
                14, FontStyle.Normal, TextAnchor.LowerLeft);
            UIFactory.SetOffsets(description.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(26f, 72f), new Vector2(-220f, -42f));
            description.color = new Color(0.75f, 0.8f, 0.88f);

            int found = game.GetBiomeCreatureFound(biome.type);
            int total = game.GetBiomeCreatureTotal(biome.type);
            int discoveredNests = nests != null ? nests.GetDiscoveredCount(biome.type) : 0;
            int totalNests = nests != null ? nests.GetTotalNestCount(biome.type) : 0;
            int totalEvents = game.Content.biomeEvents.Count(data =>
                data != null && data.biome == biome.type);
            int foundEvents = game.Content.biomeEvents.Count(data =>
                data != null && data.biome == biome.type &&
                game.GetFoundBiomeEvents().Contains(data.id));
            Text progress = UIFactory.Text(
                "Progress",
                row.transform,
                "Найдено: " + found + "/" + total +
                "  |  Логовища: " + discoveredNests + "/" + totalNests +
                "  |  События: " + foundEvents + "/" + totalEvents +
                "\n" + BuildUnknownPreview(biome),
                13,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            UIFactory.SetOffsets(progress.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(26f, 10f), new Vector2(-220f, -86f));
            progress.color = new Color(0.86f, 0.79f, 0.55f);

            string buttonText = current ? "ВЫБРАНО" : unlocked ? "ПЕРЕЙТИ" : "ОТКРЫТЬ  " + biome.unlockPrice;
            Button button = UIFactory.Button("Select", row.transform, buttonText,
                current ? new Color(0.3f, 0.38f, 0.32f) :
                unlocked ? new Color(0.24f, 0.52f, 0.4f) : new Color(0.64f, 0.42f, 0.18f));
            UIFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-16f, 31f), new Vector2(182f, 46f));
            button.interactable = !current;
            button.onClick.AddListener(delegate
            {
                if (game.TryUnlockBiome(biome))
                {
                    Rebuild();
                    if (closeAction != null)
                    {
                        closeAction();
                    }
                }
            });

            Button nestButton = UIFactory.Button(
                "ClaimNests",
                row.transform,
                nests != null && nests.GetReadyCount(biome.type) > 0
                    ? "СОБРАТЬ ЛОГОВА"
                    : "ЛОГОВА НЕ ГОТОВЫ",
                new Color(0.35f, 0.3f, 0.55f));
            UIFactory.SetRect(
                nestButton.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-16f, -31f),
                new Vector2(182f, 42f));
            nestButton.interactable = unlocked && nests != null &&
                                      nests.GetReadyCount(biome.type) > 0;
            nestButton.onClick.AddListener(() =>
            {
                int claimed = nests.ClaimReadyInBiome(biome.type);
                if (claimed == 0)
                {
                    game.RaiseNotification("В логовищах пока нет готовых наград.");
                }

                Rebuild();
            });
        }

        private string BuildUnknownPreview(BiomeData biome)
        {
            List<CreatureData> creatures = biome.availableCreatures
                .Where(creature => creature != null && creature.appearanceChance > 0.001f)
                .ToList();
            List<string> parts = new List<string>();
            AddRarityPreview(parts, creatures, CreatureRarity.Legendary, "Легендарное");
            AddRarityPreview(parts, creatures, CreatureRarity.Rare, "Редкое");
            AddRarityPreview(parts, creatures, CreatureRarity.Epic, "Эпическое");
            return parts.Count > 0 ? string.Join("  |  ", parts) : "Все тайны биома впереди.";
        }

        private void AddRarityPreview(
            ICollection<string> parts,
            IEnumerable<CreatureData> creatures,
            CreatureRarity rarity,
            string label)
        {
            List<CreatureData> matching = creatures.Where(creature => creature.rarity == rarity).ToList();
            if (matching.Count == 0)
            {
                return;
            }

            List<string> names = matching
                .Select(creature => game.IsCreatureFound(creature.id) ? creature.creatureName : "???")
                .ToList();
            parts.Add(label + ": " + string.Join(", ", names));
        }
    }
}
