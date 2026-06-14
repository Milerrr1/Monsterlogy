using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public static class MonstrologyFontProvider
    {
        private const string SettingsResourcePath = "MonstrologyFontSettings";

        private static MonstrologyFontSettings settings;
        private static bool loadAttempted;

        public static Font LegacyFont
        {
            get
            {
                EnsureLoaded();
                if (settings != null && settings.LegacyFont != null)
                {
                    return settings.LegacyFont;
                }

                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        public static TMP_FontAsset TmpFont
        {
            get
            {
                EnsureLoaded();
                return settings != null ? settings.TmpFont : null;
            }
        }

        public static void Apply(Text text)
        {
            if (text != null)
            {
                text.font = LegacyFont;
            }
        }

        public static void Apply(TextMesh textMesh)
        {
            if (textMesh == null)
            {
                return;
            }

            Font font = LegacyFont;
            textMesh.font = font;

            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
            {
                renderer.sharedMaterial = font.material;
            }
        }

        public static void Apply(TMP_Text text)
        {
            TMP_FontAsset font = TmpFont;
            if (text != null && font != null)
            {
                text.font = font;
            }
        }

        private static void EnsureLoaded()
        {
            if (loadAttempted)
            {
                return;
            }

            loadAttempted = true;
            settings = Resources.Load<MonstrologyFontSettings>(
                SettingsResourcePath);

            if (settings == null)
            {
                Debug.LogWarning(
                    "Monstrology font settings were not found. " +
                    "Run Tools/Monstrology/UI/Fix Russian Fonts.");
            }
        }
    }
}
