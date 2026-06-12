using System;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public class NestPanel : MonoBehaviour
    {
        private GameManager game;
        private CreatureNestSystem nests;
        private GameObject root;
        private Text titleText;
        private Text detailsText;
        private Text timerText;
        private Button inspectButton;
        private CreatureNestProgress current;
        private float refreshTimer;

        public string CurrentNestId
        {
            get { return current != null ? current.nestId : string.Empty; }
        }

        public void Initialize(
            GameManager gameManager,
            CreatureNestSystem nestSystem,
            GameObject panelRoot,
            Transform content)
        {
            game = gameManager;
            nests = nestSystem;
            root = panelRoot;
            Build(content);
        }

        private void Update()
        {
            if (root == null || !root.activeInHierarchy || current == null)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.5f;
                Refresh();
            }
        }

        public void Show(CreatureNestProgress progress)
        {
            current = progress;
            refreshTimer = 0f;
            Refresh();
        }

        private void Build(Transform content)
        {
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(110, 110, 34, 34);
            layout.spacing = 16f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            titleText = UIFactory.Label(content, string.Empty, 27, 58f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.78f, 0.3f);

            detailsText = UIFactory.Label(content, string.Empty, 19, 190f);
            detailsText.alignment = TextAnchor.MiddleCenter;
            detailsText.color = new Color(0.86f, 0.9f, 0.96f);

            timerText = UIFactory.Label(content, string.Empty, 20, 52f);
            timerText.alignment = TextAnchor.MiddleCenter;

            inspectButton = UIFactory.Button(
                "InspectNest",
                content,
                "ОСМОТРЕТЬ ЛОГОВО",
                new Color(0.56f, 0.36f, 0.18f));
            UIFactory.SetLayoutHeight(inspectButton.gameObject, 58f);
            inspectButton.onClick.AddListener(Inspect);
        }

        private void Refresh()
        {
            if (current == null || nests == null)
            {
                return;
            }

            CreatureNestData data = nests.GetNestData(current.nestId);
            CreatureData creature = data != null && game != null
                ? game.GetCreature(data.speciesId)
                : null;
            if (data == null)
            {
                titleText.text = "Логовище";
                detailsText.text = "Данные логовища не найдены.";
                timerText.text = string.Empty;
                inspectButton.interactable = false;
                return;
            }

            titleText.text = data.displayName;
            detailsText.text =
                (string.IsNullOrEmpty(data.description)
                    ? "Здесь часто встречаются " +
                      (creature != null ? creature.creatureName : data.speciesId) + "."
                    : data.description) +
                "\n\nСвязанный вид: " +
                (creature != null ? creature.creatureName : data.speciesId) +
                "\nУровень логова: " + current.level +
                "    Осмотров: " + current.claims +
                "\nВозможные находки: копии вида, ресурс прокачки, " +
                "редкая одежда биома и особая встреча.";

            bool ready = nests.IsRewardReady(current);
            TimeSpan remaining = nests.GetTimeUntilReady(current);
            timerText.text = ready
                ? "До следующей награды: ГОТОВО"
                : "До следующей награды: " + FormatTime(remaining);
            timerText.color = ready
                ? new Color(0.48f, 0.95f, 0.62f)
                : new Color(0.78f, 0.82f, 0.9f);
            inspectButton.interactable = ready;
        }

        private void Inspect()
        {
            if (current == null || nests == null)
            {
                return;
            }

            string message;
            bool claimed = nests.Claim(current.nestId, out message);
            if (game != null)
            {
                game.RaiseNotification(message);
            }

            if (claimed)
            {
                current = nests.GetNest(current.nestId);
            }

            Refresh();
        }

        private static string FormatTime(TimeSpan time)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)time.TotalSeconds));
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds % 3600 / 60;
            int seconds = totalSeconds % 60;
            return hours.ToString("00") + ":" +
                   minutes.ToString("00") + ":" +
                   seconds.ToString("00");
        }
    }
}
