using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Monstrology
{
    public class UIManager : MonoBehaviour
    {
        private GameManager game;
        private ExplorationSystem exploration;
        private MutationSystem mutations;
        private QuestSystem quests;
        private PlayerController2D player;
        private InteractionSystem interaction;

        private Image background;
        private Text biomeText;
        private Text coinsText;
        private Text energyText;
        private Text resultTitle;
        private Text resultDescription;
        private Image resultIcon;
        private Text tracksText;
        private Text notificationText;

        private GameObject encyclopediaPanel;
        private GameObject biomePanel;
        private GameObject mutationPanel;
        private GameObject questPanel;

        private EncyclopediaUI encyclopediaUI;
        private BiomeUI biomeUI;

        private Dropdown creatureDropdown;
        private Dropdown itemDropdown;
        private Text mutationMessage;
        private Transform questContent;

        public void Initialize(
            GameManager gameManager,
            ExplorationSystem explorationSystem,
            MutationSystem mutationSystem,
            QuestSystem questSystem,
            PlayerController2D playerController,
            InteractionSystem interactionSystem)
        {
            game = gameManager;
            exploration = explorationSystem;
            mutations = mutationSystem;
            quests = questSystem;
            player = playerController;
            interaction = interactionSystem;

            EnsureEventSystem();
            BuildCanvas();
            exploration.ExplorationCompleted += ShowExplorationResult;
            game.StateChanged += RefreshHeader;
            game.NotificationRaised += ShowNotification;
            RefreshHeader();
            ShowWelcome();
        }

        private void OnDestroy()
        {
            if (exploration != null)
            {
                exploration.ExplorationCompleted -= ShowExplorationResult;
            }

            if (game != null)
            {
                game.StateChanged -= RefreshHeader;
                game.NotificationRaised -= ShowNotification;
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = UIFactory.Object("MonstrologyCanvas", transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            background = UIFactory.Image("BiomeTint", canvasObject.transform, new Color(0.16f, 0.28f, 0.24f, 0.06f));
            UIFactory.Stretch(background.rectTransform);
            background.raycastTarget = false;

            Image shade = UIFactory.Image("Shade", background.transform, new Color(0.02f, 0.04f, 0.07f, 0.04f));
            UIFactory.Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            BuildTopBar(canvasObject.transform);
            BuildMainContent(canvasObject.transform);
            BuildBottomBar(canvasObject.transform);
            BuildWorldControls(canvasObject.transform);
            BuildModals(canvasObject.transform);
        }

        private void BuildTopBar(Transform parent)
        {
            Image bar = UIFactory.Image("TopBar", parent, new Color(0.055f, 0.075f, 0.11f, 0.94f));
            UIFactory.SetRect(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));

            Text title = UIFactory.Text("Title", bar.transform, "МОНСТРОЛОГИЯ", 25, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIFactory.SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(260f, 0f));
            title.color = new Color(1f, 0.84f, 0.37f);

            biomeText = UIFactory.Text("Biome", bar.transform, "", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(biomeText.rectTransform, new Vector2(0.35f, 0f), new Vector2(0.65f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            coinsText = CreateResourcePill(bar.transform, "Coins", new Vector2(-242f, 0f), new Color(0.96f, 0.69f, 0.18f));
            energyText = CreateResourcePill(bar.transform, "Energy", new Vector2(-102f, 0f), new Color(0.32f, 0.76f, 0.98f));

            Button energyButton = UIFactory.Button("RewardEnergy", bar.transform, "+ Энергия", new Color(0.24f, 0.48f, 0.78f));
            UIFactory.SetRect(energyButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(86f, 42f));
            energyButton.onClick.AddListener(ShowRewardedEnergy);
        }

        private Text CreateResourcePill(Transform parent, string name, Vector2 position, Color accent)
        {
            Image pill = UIFactory.Image(name, parent, new Color(1f, 1f, 1f, 0.1f));
            UIFactory.ApplyRounded(pill);
            UIFactory.SetRect(pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), position, new Vector2(126f, 42f));
            Image marker = UIFactory.Image("Marker", pill.transform, accent);
            UIFactory.SetRect(marker.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
            Text text = UIFactory.Text("Value", pill.transform, "", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.Stretch(text.rectTransform);
            return text;
        }

        private void BuildMainContent(Transform parent)
        {
            Image resultCard = UIFactory.Image("ResultCard", parent, new Color(0.045f, 0.06f, 0.09f, 0.88f));
            UIFactory.ApplyRounded(resultCard);
            UIFactory.AddSoftShadow(resultCard.gameObject);
            UIFactory.SetRect(resultCard.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -82f), new Vector2(370f, 152f));

            resultIcon = UIFactory.Image("ResultIcon", resultCard.transform, new Color(0.3f, 0.7f, 0.5f));
            UIFactory.SetRect(resultIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(92f, 92f));
            resultIcon.preserveAspect = true;

            resultTitle = UIFactory.Text("ResultTitle", resultCard.transform, "", 20, FontStyle.Bold, TextAnchor.UpperLeft);
            UIFactory.SetRect(resultTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(62f, -18f), new Vector2(-144f, 34f));

            resultDescription = UIFactory.Text("ResultDescription", resultCard.transform, "", 13, FontStyle.Normal, TextAnchor.UpperLeft);
            UIFactory.SetOffsets(resultDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(126f, 14f), new Vector2(-16f, -54f));
            resultDescription.color = new Color(0.88f, 0.91f, 0.96f);

            Button exploreButton = UIFactory.Button("ExploreFallback", parent, "Быстрый поиск  -1",
                new Color(0.93f, 0.43f, 0.23f, 0.94f));
            UIFactory.SetRect(exploreButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -244f), new Vector2(180f, 42f));
            exploreButton.onClick.AddListener(OnExploreButton);

            tracksText = UIFactory.Text("Tracks", parent, "", 13, FontStyle.Normal, TextAnchor.MiddleCenter);
            UIFactory.SetRect(tracksText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(520f, 24f));
            tracksText.color = new Color(0.9f, 0.85f, 0.67f);

            notificationText = UIFactory.Text("Notification", parent, "", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(notificationText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(560f, 32f));
            notificationText.color = new Color(1f, 0.88f, 0.44f);
        }

        private void BuildBottomBar(Transform parent)
        {
            Image bar = UIFactory.Image("BottomBar", parent, new Color(0.055f, 0.075f, 0.11f, 0.96f));
            UIFactory.SetRect(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 58f));

            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(160, 160, 7, 7);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            AddNavigationButton(bar.transform, "БИОМЫ", OpenBiomes);
            AddNavigationButton(bar.transform, "ЭНЦИКЛОПЕДИЯ", OpenEncyclopedia);
            AddNavigationButton(bar.transform, "МУТАЦИИ", OpenMutations);
            AddNavigationButton(bar.transform, "КВЕСТЫ", OpenQuests);
        }

        private void AddNavigationButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = UIFactory.Button(label, parent, label, new Color(0.16f, 0.22f, 0.31f));
            button.onClick.AddListener(action);
        }

        private void BuildWorldControls(Transform parent)
        {
            Image joystickBase = UIFactory.Image("MovementJoystick", parent, new Color(0.08f, 0.12f, 0.18f, 0.72f));
            joystickBase.sprite = WorldPlaceholderSprites.Circle;
            UIFactory.SetRect(joystickBase.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(24f, 74f), new Vector2(118f, 118f));

            Image handle = UIFactory.Image("Handle", joystickBase.transform, new Color(0.36f, 0.76f, 0.98f, 0.88f));
            handle.sprite = WorldPlaceholderSprites.Circle;
            UIFactory.SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
            handle.raycastTarget = false;

            VirtualJoystick joystick = joystickBase.gameObject.AddComponent<VirtualJoystick>();
            joystick.Initialize(player, handle.rectTransform, 36f);

            Text prompt = UIFactory.Text("InteractionPrompt", parent, "", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(420f, 34f));
            prompt.color = new Color(1f, 0.91f, 0.56f);

            Button interactButton = UIFactory.Button(
                "Interact",
                parent,
                "ВЗАИМОДЕЙСТВОВАТЬ",
                new Color(0.28f, 0.68f, 0.52f, 0.94f));
            UIFactory.SetRect(interactButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-22f, 82f), new Vector2(178f, 52f));

            if (interaction != null)
            {
                interaction.BindUI(prompt, interactButton);
            }
        }

        private void BuildModals(Transform parent)
        {
            encyclopediaPanel = CreateModal(parent, "ЭНЦИКЛОПЕДИЯ");
            encyclopediaUI = gameObject.AddComponent<EncyclopediaUI>();
            encyclopediaUI.Initialize(game, UIFactory.FindContent(encyclopediaPanel));

            biomePanel = CreateModal(parent, "КАРТА БИОМОВ");
            biomeUI = gameObject.AddComponent<BiomeUI>();
            biomeUI.Initialize(game, UIFactory.FindContent(biomePanel), CloseAllPanels);

            mutationPanel = CreateModal(parent, "ЛАБОРАТОРИЯ МУТАЦИЙ");
            BuildMutationPanel(UIFactory.FindContent(mutationPanel));

            questPanel = CreateModal(parent, "КВЕСТЫ И НАГРАДЫ");
            questContent = UIFactory.FindContent(questPanel);
        }

        private GameObject CreateModal(Transform parent, string title)
        {
            GameObject overlay = UIFactory.Object(title, parent);
            UIFactory.Stretch(overlay.GetComponent<RectTransform>());

            Image dim = UIFactory.Image("Dim", overlay.transform, new Color(0.015f, 0.02f, 0.035f, 0.82f));
            UIFactory.Stretch(dim.rectTransform);

            Image panel = UIFactory.Image("Panel", overlay.transform, new Color(0.075f, 0.095f, 0.14f, 0.98f));
            UIFactory.ApplyRounded(panel);
            UIFactory.AddSoftShadow(panel.gameObject);
            UIFactory.SetOffsets(panel.rectTransform, new Vector2(0.035f, 0.06f), new Vector2(0.965f, 0.94f),
                Vector2.zero, Vector2.zero);

            Text heading = UIFactory.Text("Heading", panel.transform, title, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIFactory.SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-42f, -12f), new Vector2(-174f, 58f));
            heading.color = new Color(1f, 0.82f, 0.32f);

            Button close = UIFactory.Button("Close", panel.transform, "ЗАКРЫТЬ", new Color(0.72f, 0.28f, 0.35f));
            UIFactory.SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(112f, 44f));
            close.onClick.AddListener(CloseAllPanels);

            Transform content = UIFactory.ScrollContent(panel.transform, new Vector2(24f, 24f), new Vector2(-24f, -82f));
            content.name = "Content";
            overlay.SetActive(false);
            return overlay;
        }

        private void BuildMutationPanel(Transform content)
        {
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(120, 120, 30, 30);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            UIFactory.Label(content, "Выберите найденное существо и предмет-катализатор.", 18, 38f);
            creatureDropdown = UIFactory.Dropdown("CreatureDropdown", content);
            UIFactory.SetLayoutHeight(creatureDropdown.gameObject, 48f);
            itemDropdown = UIFactory.Dropdown("ItemDropdown", content);
            UIFactory.SetLayoutHeight(itemDropdown.gameObject, 48f);

            Button mutate = UIFactory.Button("Mutate", content, "МУТИРОВАТЬ", new Color(0.63f, 0.3f, 0.82f));
            UIFactory.SetLayoutHeight(mutate.gameObject, 58f);
            mutate.onClick.AddListener(PerformMutation);

            mutationMessage = UIFactory.Label(content,
                "Подсказка: некоторые формы открываются только в лаборатории.", 18, 74f);
            mutationMessage.alignment = TextAnchor.MiddleCenter;
            mutationMessage.color = new Color(0.9f, 0.86f, 0.98f);
        }

        private void RebuildMutationOptions()
        {
            creatureDropdown.ClearOptions();
            itemDropdown.ClearOptions();

            List<string> creatureNames = new List<string>();
            foreach (CreatureData creature in game.Content.creatures)
            {
                if (game.IsCreatureFound(creature.id))
                {
                    creatureNames.Add(creature.creatureName);
                }
            }

            List<string> itemNames = new List<string>();
            foreach (ItemData item in game.Content.items)
            {
                if (game.GetItemCount(item.id) > 0)
                {
                    itemNames.Add(item.itemName + "  x" + game.GetItemCount(item.id));
                }
            }

            creatureDropdown.AddOptions(creatureNames.Count > 0 ? creatureNames : new List<string> { "Нет найденных существ" });
            itemDropdown.AddOptions(itemNames.Count > 0 ? itemNames : new List<string> { "Нет предметов" });
        }

        public void OnExploreButton()
        {
            exploration.Explore();
        }

        public void OpenBiomes()
        {
            OpenPanel(biomePanel);
            biomeUI.Rebuild();
        }

        public void OpenEncyclopedia()
        {
            OpenPanel(encyclopediaPanel);
            encyclopediaUI.Rebuild();
        }

        public void OpenMutations()
        {
            OpenPanel(mutationPanel);
            RebuildMutationOptions();
        }

        public void OpenQuests()
        {
            OpenPanel(questPanel);
            RebuildQuests();
        }

        public void PerformMutation()
        {
            List<CreatureData> creatures = game.Content.creatures.FindAll(creature => game.IsCreatureFound(creature.id));
            List<ItemData> items = game.Content.items.FindAll(item => game.GetItemCount(item.id) > 0);
            if (creatures.Count == 0 || items.Count == 0)
            {
                mutationMessage.text = "Для опыта нужны найденное существо и предмет.";
                return;
            }

            int creatureIndex = Mathf.Clamp(creatureDropdown.value, 0, creatures.Count - 1);
            int itemIndex = Mathf.Clamp(itemDropdown.value, 0, items.Count - 1);
            string message;
            bool success = mutations.TryMutate(creatures[creatureIndex].id, items[itemIndex].id, out message);
            mutationMessage.text = message;
            mutationMessage.color = success
                ? new Color(0.48f, 0.95f, 0.62f)
                : new Color(1f, 0.67f, 0.48f);
            RebuildMutationOptions();
        }

        private void RebuildQuests()
        {
            UIFactory.ClearChildren(questContent);
            VerticalLayoutGroup layout = questContent.gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = questContent.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(30, 30, 16, 30);
                layout.spacing = 10f;
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }

            foreach (QuestData quest in game.Content.quests)
            {
                BuildQuestRow(quest);
            }
        }

        private void BuildQuestRow(QuestData quest)
        {
            bool completed = quests.IsCompleted(quest);
            bool claimed = game.IsQuestClaimed(quest.id);
            Image row = UIFactory.Image(quest.id, questContent,
                claimed ? new Color(0.11f, 0.18f, 0.15f) : new Color(0.10f, 0.13f, 0.19f));
            UIFactory.ApplyRounded(row);
            UIFactory.AddSoftShadow(row.gameObject);
            UIFactory.SetLayoutHeight(row.gameObject, 92f);

            Text title = UIFactory.Text("Title", row.transform, quest.questName, 19, FontStyle.Bold, TextAnchor.UpperLeft);
            UIFactory.SetOffsets(title.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(18f, 46f), new Vector2(-210f, -10f));
            title.color = completed ? new Color(0.46f, 0.92f, 0.59f) : Color.white;

            int progress = Mathf.Min(quests.GetProgress(quest), quest.requiredAmount);
            Text description = UIFactory.Text("Description", row.transform,
                quest.description + "\nПрогресс: " + progress + "/" + quest.requiredAmount +
                "   Награда: " + quest.rewardCoins + " монет, " + quest.rewardEnergy + " энергии",
                14, FontStyle.Normal, TextAnchor.LowerLeft);
            UIFactory.SetOffsets(description.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(18f, 10f), new Vector2(-210f, -42f));
            description.color = new Color(0.78f, 0.82f, 0.89f);

            string buttonLabel = claimed ? "ПОЛУЧЕНО" : completed ? "ЗАБРАТЬ" : "В ПРОЦЕССЕ";
            Button button = UIFactory.Button("Claim", row.transform, buttonLabel,
                claimed ? new Color(0.22f, 0.38f, 0.29f) :
                completed ? new Color(0.28f, 0.62f, 0.36f) : new Color(0.24f, 0.28f, 0.36f));
            UIFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(172f, 52f));
            button.interactable = completed && !claimed;
            button.onClick.AddListener(delegate
            {
                quests.TryClaim(quest);
                RebuildQuests();
            });
        }

        private void RefreshHeader()
        {
            if (game == null || game.CurrentBiome == null || biomeText == null)
            {
                return;
            }

            biomeText.text = game.CurrentBiome.biomeName;
            coinsText.text = "МОНЕТЫ  " + game.Coins;
            energyText.text = "ЭНЕРГИЯ  " + game.Energy;
            tracksText.text = game.GetTrackSummary();
            Color tint = game.CurrentBiome.fallbackColor;
            tint.a = 0.06f;
            background.color = tint;
            background.sprite = null;
        }

        private void ShowExplorationResult(ExplorationResult result)
        {
            resultTitle.text = result.title;
            resultTitle.color = result.accentColor;
            resultDescription.text = result.description;
            resultIcon.sprite = result.icon;
            resultIcon.color = result.icon == null ? result.accentColor : Color.white;
            notificationText.text = "";
        }

        private void ShowWelcome()
        {
            resultTitle.text = "Добро пожаловать, исследователь!";
            resultTitle.color = new Color(1f, 0.82f, 0.34f);
            resultDescription.text =
                "Ходите по карте с WASD, стрелками или джойстиком. Подойдите к находке и нажмите E.";
            resultIcon.sprite = null;
            resultIcon.color = new Color(0.28f, 0.72f, 0.5f);
        }

        private void ShowNotification(string message)
        {
            notificationText.text = message;
        }

        public void ShowRewardedEnergy()
        {
            if (YandexGamesBridge.Instance == null)
            {
                return;
            }

            YandexGamesBridge.Instance.ShowRewardedAd(success =>
            {
                if (success)
                {
                    game.AddEnergy(5);
                    ShowNotification("Награда за рекламу: +5 энергии");
                }
            });
        }

        private void OpenPanel(GameObject panel)
        {
            CloseAllPanels();
            panel.SetActive(true);
            SetWorldInputEnabled(false);
        }

        public void CloseAllPanels()
        {
            encyclopediaPanel.SetActive(false);
            biomePanel.SetActive(false);
            mutationPanel.SetActive(false);
            questPanel.SetActive(false);
            SetWorldInputEnabled(true);
        }

        private void SetWorldInputEnabled(bool value)
        {
            if (player != null)
            {
                player.EnableMovement(value);
            }

            if (interaction != null)
            {
                interaction.EnableInteraction(value);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }

    internal static class UIFactory
    {
        private static Font cachedFont;
        private static Sprite roundedSprite;

        public static GameObject Object(string name, Transform parent)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            instance.transform.SetParent(parent, false);
            return instance;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            GameObject instance = Object(name, parent);
            Image image = instance.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static void ApplyRounded(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = RoundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        public static void AddSoftShadow(GameObject target)
        {
            if (target == null || target.GetComponent<Shadow>() != null)
            {
                return;
            }

            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }

        public static Text Text(string name, Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            GameObject instance = Object(name, parent);
            Text text = instance.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Text Label(Transform parent, string value, int size, float height)
        {
            Text text = Text("Label", parent, value, size, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetLayoutHeight(text.gameObject, height);
            return text;
        }

        public static Button Button(string name, Transform parent, string label, Color color)
        {
            Image image = Image(name, parent, color);
            ApplyRounded(image);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            button.colors = colors;
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            Text text = Text("Label", image.transform, label, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            return button;
        }

        public static Dropdown Dropdown(string name, Transform parent)
        {
            Image root = Image(name, parent, new Color(0.14f, 0.18f, 0.25f));
            ApplyRounded(root);
            Dropdown dropdown = root.gameObject.AddComponent<Dropdown>();
            Text label = Text("Label", root.transform, "", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(16f, 0f), new Vector2(-52f, 0f));

            Text arrow = Text("Arrow", root.transform, "▼", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), Vector2.zero, new Vector2(46f, 0f));

            Image template = Image("Template", root.transform, new Color(0.08f, 0.11f, 0.16f));
            SetRect(template.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 180f));
            template.gameObject.SetActive(false);
            ScrollRect scroll = template.gameObject.AddComponent<ScrollRect>();

            Image viewport = Image("Viewport", template.transform, new Color(1f, 1f, 1f, 0.02f));
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = Object("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            SetRect(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 32f));

            Toggle item = Image("Item", content.transform, new Color(0.14f, 0.18f, 0.25f)).gameObject.AddComponent<Toggle>();
            SetRect(item.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 32f));
            Text itemLabel = Text("Item Label", item.transform, "Option", 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(12f, 0f), new Vector2(-12f, 0f));
            item.targetGraphic = item.GetComponent<Image>();

            scroll.viewport = viewport.rectTransform;
            scroll.content = contentRect;
            scroll.horizontal = false;
            dropdown.targetGraphic = root;
            dropdown.captionText = label;
            dropdown.template = template.rectTransform;
            dropdown.itemText = itemLabel;
            return dropdown;
        }

        public static Transform ScrollContent(Transform parent, Vector2 minOffset, Vector2 maxOffset)
        {
            GameObject scrollObject = Object("ScrollView", parent);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            SetOffsets(scrollRectTransform, Vector2.zero, Vector2.one, minOffset, maxOffset);
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            Image viewport = Image("Viewport", scrollObject.transform, new Color(1f, 1f, 1f, 0.015f));
            Stretch(viewport.rectTransform);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = Object("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            SetRect(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = contentRect;
            return content.transform;
        }

        public static Transform FindContent(GameObject panel)
        {
            ScrollRect scroll = panel.GetComponentInChildren<ScrollRect>(true);
            return scroll != null ? scroll.content : panel.transform;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        public static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        public static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void SetLayoutHeight(GameObject target, float height)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.preferredHeight = height;
            element.minHeight = height;
        }

        public static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
            }
        }

        private static Font Font
        {
            get
            {
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return cachedFont;
            }
        }

        private static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite != null)
                {
                    return roundedSprite;
                }

                const int size = 32;
                const int radius = 9;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "Runtime Rounded UI";
                texture.hideFlags = HideFlags.DontSave;
                texture.filterMode = FilterMode.Bilinear;
                Color[] pixels = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int cornerX = x < radius ? radius : x >= size - radius ? size - radius - 1 : x;
                        int cornerY = y < radius ? radius : y >= size - radius ? size - radius - 1 : y;
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                        pixels[y * size + x] = distance <= radius
                            ? Color.white
                            : new Color(1f, 1f, 1f, 0f);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                roundedSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                roundedSprite.name = "Runtime Rounded UI";
                roundedSprite.hideFlags = HideFlags.DontSave;
                return roundedSprite;
            }
        }
    }
}
