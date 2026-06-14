using TMPro;
using UnityEngine;

namespace Monstrology
{
    public sealed class MonstrologyFontSettings : ScriptableObject
    {
        [SerializeField] private Font legacyFont;
        [SerializeField] private TMP_FontAsset tmpFont;

        public Font LegacyFont
        {
            get { return legacyFont; }
        }

        public TMP_FontAsset TmpFont
        {
            get { return tmpFont; }
        }

#if UNITY_EDITOR
        public void Configure(Font font, TMP_FontAsset fontAsset)
        {
            legacyFont = font;
            tmpFont = fontAsset;
        }
#endif
    }
}
