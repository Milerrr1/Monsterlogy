using System;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class BiomeUI : MonoBehaviour
    {
        private GameManager game;
        private Transform content;
        private Action closeAction;

        public void Initialize(GameManager gameManager, Transform contentRoot, Action onBiomeSelected)
        {
            game = gameManager;
            content = contentRoot;
            closeAction = onBiomeSelected;
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
            UIFactory.SetLayoutHeight(row.gameObject, 96f);

            Image colorStrip = UIFactory.Image("Color", row.transform, biome.fallbackColor);
            UIFactory.SetRect(colorStrip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(12f, 0f));

            Text title = UIFactory.Text("Name", row.transform,
                biome.biomeName + (current ? "  • ТЕКУЩИЙ" : ""), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            UIFactory.SetOffsets(title.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(26f, 46f), new Vector2(-220f, -10f));
            title.color = current ? new Color(1f, 0.82f, 0.3f) : Color.white;

            Text description = UIFactory.Text("Description", row.transform, biome.description,
                14, FontStyle.Normal, TextAnchor.LowerLeft);
            UIFactory.SetOffsets(description.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(26f, 10f), new Vector2(-220f, -42f));
            description.color = new Color(0.75f, 0.8f, 0.88f);

            string buttonText = current ? "ВЫБРАНО" : unlocked ? "ПЕРЕЙТИ" : "ОТКРЫТЬ  " + biome.unlockPrice;
            Button button = UIFactory.Button("Select", row.transform, buttonText,
                current ? new Color(0.3f, 0.38f, 0.32f) :
                unlocked ? new Color(0.24f, 0.52f, 0.4f) : new Color(0.64f, 0.42f, 0.18f));
            UIFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(182f, 54f));
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
        }
    }
}
