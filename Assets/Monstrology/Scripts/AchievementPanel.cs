using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class AchievementPanel : MonoBehaviour
    {
        private AchievementSystem achievements;
        private Transform content;

        public void Initialize(AchievementSystem achievementSystem, Transform contentRoot)
        {
            achievements = achievementSystem;
            content = contentRoot;
        }

        public void Rebuild()
        {
            UIFactory.ClearChildren(content);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(30, 30, 20, 30);
            layout.spacing = 10f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            foreach (AchievementDefinition achievement in achievements.GetDefinitions())
            {
                bool unlocked = achievements.IsUnlocked(achievement.id);
                Image row = UIFactory.Image(
                    achievement.id,
                    content,
                    unlocked ? new Color(0.12f, 0.24f, 0.17f) : new Color(0.09f, 0.11f, 0.16f));
                UIFactory.ApplyRounded(row);
                UIFactory.SetLayoutHeight(row.gameObject, 78f);

                Text text = UIFactory.Text(
                    "Text",
                    row.transform,
                    (unlocked ? "ОТКРЫТО  " : "ЗАКРЫТО  ") + achievement.title +
                    "\n" + achievement.description,
                    17,
                    unlocked ? FontStyle.Bold : FontStyle.Normal,
                    TextAnchor.MiddleLeft);
                UIFactory.SetOffsets(text.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(18f, 8f), new Vector2(-18f, -8f));
                text.color = unlocked
                    ? new Color(0.48f, 0.95f, 0.62f)
                    : new Color(0.62f, 0.66f, 0.74f);
            }
        }
    }
}
