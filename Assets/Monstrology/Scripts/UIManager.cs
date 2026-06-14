using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Monstrology
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int rewardedEnergyAmount = 3;

        private GameManager game;
        private ExplorationSystem exploration;
        private QuestSystem quests;
        private PlayerController2D player;
        private InteractionSystem interaction;
        private CreatureCollectionManager petCollection;
        private AccessoryInventoryManager accessoryInventory;
        private BreedingSystem breeding;
        private PetUpgradeSystem upgrades;
        private WorldExplorationManager world;
        private AchievementSystem achievements;
        private EnergyRegenerationSystem energyRegeneration;
        private StarterBoostSystem starterBoost;
        private BiomeEventSystem biomeEvents;
        private AudioManager audioManager;
        private DailyRewardSystem dailyRewards;
        private CreatureNestSystem creatureNests;

        private Image background;
        private Text biomeText;
        private Text coinsText;
        private Text energyText;
        private Text resultTitle;
        private Text resultDescription;
        private Image resultIcon;
        private GameObject resultCard;
        private CanvasGroup resultCanvasGroup;
        private Coroutine resultHideRoutine;
        private Text tracksText;
        private Text notificationText;
        private Button rewardEnergyButton;
        private Text titleText;
        private RectTransform safeAreaRoot;
        private RectTransform topBarRoot;
        private RectTransform bottomBarRoot;
        private RectTransform mobileControlsRoot;
        private GameObject mobileControlsObject;
        private GameObject fullscreenButton;
        private AdaptiveUIController adaptiveUI;

        private GameObject encyclopediaPanel;
        private GameObject biomePanel;
        private GameObject questPanel;
        private GameObject petsPanel;
        private GameObject accessoryPanel;
        private GameObject breedingPanel;
        private GameObject achievementPanel;
        private GameObject pausePanel;
        private GameObject settingsPanel;
        private GameObject introPanel;
        private GameObject dailyRewardPanel;
        private GameObject nestPanel;

        private EncyclopediaUI encyclopediaUI;
        private BiomeUI biomeUI;
        private PetsPanel petsUI;
        private WardrobePanel wardrobeUI;
        private BreedingPanel breedingUI;
        private AchievementPanel achievementUI;
        private NestPanel nestUI;
        private PetAdoptionDialog adoptionDialog;

        private Transform questContent;
        private float headerTimer;
        private Text pauseStatsText;
        private Text dailyRewardText;
        private bool isPaused;
        private bool rewardedEnergyRequestPending;
        private float timeScaleBeforePause = 1f;

        public bool ResultCardVisible
        {
            get { return resultCard != null && resultCard.activeSelf; }
        }
        public bool HasPauseMenu { get { return pausePanel != null && settingsPanel != null; } }
        public bool IsPaused { get { return isPaused; } }
        public int RewardedEnergyAmount { get { return rewardedEnergyAmount; } }
        public bool NestPanelVisible
        {
            get { return nestPanel != null && nestPanel.activeSelf; }
        }
        public bool MobileControlsVisible
        {
            get
            {
                return mobileControlsObject != null &&
                       mobileControlsObject.activeSelf;
            }
        }
        public ControlMode ControlMode
        {
            get
            {
                return adaptiveUI != null
                    ? adaptiveUI.Mode
                    : AdaptiveUIController.CurrentMode;
            }
        }
        public int ActiveMainPanelCount
        {
            get
            {
                int count = 0;
                GameObject[] panels =
                {
                    encyclopediaPanel,
                    biomePanel,
                    questPanel,
                    petsPanel,
                    accessoryPanel,
                    breedingPanel
                };
                foreach (GameObject panel in panels)
                {
                    if (panel != null && panel.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Initialize(
            GameManager gameManager,
            ExplorationSystem explorationSystem,
            QuestSystem questSystem,
            PlayerController2D playerController,
            InteractionSystem interactionSystem,
            CreatureCollectionManager collectionManager)
        {
            AccessoryInventoryManager inventoryManager = FindObjectOfType<AccessoryInventoryManager>();
            if (inventoryManager == null)
            {
                inventoryManager = gameObject.AddComponent<AccessoryInventoryManager>();
                inventoryManager.Initialize(gameManager, collectionManager);
            }

            BreedingSystem breedingSystem = FindObjectOfType<BreedingSystem>();
            if (breedingSystem == null)
            {
                breedingSystem = gameObject.AddComponent<BreedingSystem>();
                breedingSystem.Initialize(gameManager, collectionManager);
            }

            PetUpgradeSystem upgradeSystem = FindObjectOfType<PetUpgradeSystem>();
            if (upgradeSystem == null)
            {
                upgradeSystem = gameObject.AddComponent<PetUpgradeSystem>();
                upgradeSystem.Initialize(gameManager, collectionManager);
            }

            Initialize(
                gameManager,
                explorationSystem,
                questSystem,
                playerController,
                interactionSystem,
                collectionManager,
                inventoryManager,
                breedingSystem,
                upgradeSystem);
        }

        public void Initialize(
            GameManager gameManager,
            ExplorationSystem explorationSystem,
            QuestSystem questSystem,
            PlayerController2D playerController,
            InteractionSystem interactionSystem,
            CreatureCollectionManager collectionManager,
            AccessoryInventoryManager inventoryManager,
            BreedingSystem breedingSystem,
            PetUpgradeSystem upgradeSystem)
        {
            if (inventoryManager == null)
            {
                inventoryManager = gameObject.AddComponent<AccessoryInventoryManager>();
                inventoryManager.Initialize(gameManager, collectionManager);
            }

            if (breedingSystem == null)
            {
                breedingSystem = gameObject.AddComponent<BreedingSystem>();
                breedingSystem.Initialize(gameManager, collectionManager);
            }

            if (upgradeSystem == null)
            {
                upgradeSystem = gameObject.AddComponent<PetUpgradeSystem>();
                upgradeSystem.Initialize(gameManager, collectionManager);
            }

            game = gameManager;
            exploration = explorationSystem;
            quests = questSystem;
            player = playerController;
            interaction = interactionSystem;
            petCollection = collectionManager;
            accessoryInventory = inventoryManager;
            breeding = breedingSystem;
            upgrades = upgradeSystem;
            world = FindObjectOfType<WorldExplorationManager>();
            achievements = FindObjectOfType<AchievementSystem>();
            energyRegeneration = FindObjectOfType<EnergyRegenerationSystem>();
            starterBoost = FindObjectOfType<StarterBoostSystem>();
            biomeEvents = FindObjectOfType<BiomeEventSystem>();
            audioManager = FindObjectOfType<AudioManager>();
            dailyRewards = FindObjectOfType<DailyRewardSystem>();
            creatureNests = FindObjectOfType<CreatureNestSystem>();
            if (achievements == null)
            {
                achievements = gameObject.AddComponent<AchievementSystem>();
                achievements.Initialize(gameManager, collectionManager, breedingSystem);
            }

            EnsureEventSystem();
            BuildCanvas();
            if (YandexGamesBridge.Instance != null)
            {
                YandexGamesBridge.Instance.RewardedRequestFinished -=
                    HandleRewardedRequestFinished;
                YandexGamesBridge.Instance.RewardedRequestFinished +=
                    HandleRewardedRequestFinished;
            }

            exploration.ExplorationCompleted += ShowExplorationResult;
            game.StateChanged += RefreshHeader;
            game.NotificationRaised += ShowNotification;
            RefreshHeader();
            ShowWelcome();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
            }

            headerTimer -= Time.unscaledDeltaTime;
            if (headerTimer <= 0f)
            {
                RefreshEnergyText();
                headerTimer = 0.25f;
            }
        }

        private void OnDestroy()
        {
            if (isPaused)
            {
                Time.timeScale = timeScaleBeforePause;
            }

            if (exploration != null)
            {
                exploration.ExplorationCompleted -= ShowExplorationResult;
            }

            if (game != null)
            {
                game.StateChanged -= RefreshHeader;
                game.NotificationRaised -= ShowNotification;
            }

            if (YandexGamesBridge.Instance != null)
            {
                YandexGamesBridge.Instance.RewardedRequestFinished -=
                    HandleRewardedRequestFinished;
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

            GameObject safeRootObject = UIFactory.Object(
                "SafeAreaRoot",
                canvasObject.transform);
            safeAreaRoot = safeRootObject.GetComponent<RectTransform>();
            UIFactory.Stretch(safeAreaRoot);

            BuildTopBar(safeRootObject.transform);
            BuildMainContent(safeRootObject.transform);
            BuildBottomBar(safeRootObject.transform);
            BuildWorldControls(safeRootObject.transform);
            BuildModals(safeRootObject.transform);

            adaptiveUI = gameObject.GetComponent<AdaptiveUIController>();
            if (adaptiveUI == null)
            {
                adaptiveUI = gameObject.AddComponent<AdaptiveUIController>();
            }

            adaptiveUI.Initialize(
                safeAreaRoot,
                topBarRoot,
                bottomBarRoot,
                mobileControlsRoot,
                mobileControlsObject,
                fullscreenButton,
                titleText,
                interaction);
        }

        private void BuildTopBar(Transform parent)
        {
            Image bar = UIFactory.Image("TopBar", parent, new Color(0.055f, 0.075f, 0.11f, 0.94f));
            topBarRoot = bar.rectTransform;
            UIFactory.SetRect(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));

            Text title = UIFactory.Text("Title", bar.transform, "МОНСТРОЛОГИЯ", 25, FontStyle.Bold, TextAnchor.MiddleLeft);
            titleText = title;
            UIFactory.SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(240f, 0f));
            title.color = new Color(1f, 0.84f, 0.37f);

            biomeText = UIFactory.Text("Biome", bar.transform, "", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(biomeText.rectTransform, new Vector2(0.35f, 0f), new Vector2(0.65f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            coinsText = CreateResourcePill(bar.transform, "Coins", new Vector2(-242f, 0f), new Color(0.96f, 0.69f, 0.18f));
            energyText = CreateResourcePill(bar.transform, "Energy", new Vector2(-102f, 0f), new Color(0.32f, 0.76f, 0.98f));
            energyText.fontSize = 13;
            energyText.lineSpacing = 0.85f;
            energyText.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(126f, 52f);

            rewardEnergyButton = UIFactory.Button(
                "RewardEnergy",
                bar.transform,
                "+" + rewardedEnergyAmount + " энергии",
                new Color(0.24f, 0.48f, 0.78f));
            UIFactory.SetRect(rewardEnergyButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(86f, 42f));
            rewardEnergyButton.onClick.AddListener(ShowRewardedEnergy);

            Button pauseButton = UIFactory.Button(
                "Pause",
                bar.transform,
                "ПАУЗА",
                new Color(0.22f, 0.28f, 0.38f));
            UIFactory.SetRect(
                pauseButton.GetComponent<RectTransform>(),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(270f, 0f),
                new Vector2(74f, 34f));
            pauseButton.onClick.AddListener(TogglePause);

            Button fullscreen = UIFactory.Button(
                "Fullscreen",
                bar.transform,
                "FULL",
                new Color(0.22f, 0.38f, 0.58f));
            fullscreenButton = fullscreen.gameObject;
            UIFactory.SetRect(
                fullscreen.GetComponent<RectTransform>(),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(352f, 0f),
                new Vector2(62f, 34f));
            fullscreen.onClick.AddListener(RequestFullscreen);
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
            Image resultCardImage = UIFactory.Image("ResultCard", parent, new Color(0.045f, 0.06f, 0.09f, 0.88f));
            resultCard = resultCardImage.gameObject;
            resultCanvasGroup = resultCard.AddComponent<CanvasGroup>();
            UIFactory.ApplyRounded(resultCardImage);
            UIFactory.AddSoftShadow(resultCard);
            UIFactory.SetRect(resultCardImage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
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
            resultCard.SetActive(false);

            Button exploreButton = UIFactory.Button("ExploreFallback", parent, "КОМПАС ИССЛЕДОВАТЕЛЯ",
                new Color(0.93f, 0.43f, 0.23f, 0.94f));
            UIFactory.SetRect(exploreButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -244f), new Vector2(220f, 42f));
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
            bottomBarRoot = bar.rectTransform;
            UIFactory.SetRect(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 58f));

            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 7, 7);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            AddNavigationButton(bar.transform, "БИОМЫ", OpenBiomes);
            AddNavigationButton(bar.transform, "ЭНЦИКЛОПЕДИЯ", OpenEncyclopedia);
            AddNavigationButton(bar.transform, "ПИТОМЦЫ", OpenPets);
            AddNavigationButton(bar.transform, "ГАРДЕРОБ", OpenWardrobe);
            AddNavigationButton(bar.transform, "ЭВОЛЮЦИЯ", OpenBreeding);
            AddNavigationButton(bar.transform, "КВЕСТЫ", OpenQuests);
        }

        private void AddNavigationButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = UIFactory.Button(label, parent, label, new Color(0.16f, 0.22f, 0.31f));
            button.onClick.AddListener(action);
        }

        private void BuildWorldControls(Transform parent)
        {
            mobileControlsObject = UIFactory.Object(
                "MobileControls",
                parent);
            mobileControlsRoot =
                mobileControlsObject.GetComponent<RectTransform>();
            UIFactory.Stretch(mobileControlsRoot);

            Image joystickBase = UIFactory.Image("MovementJoystick", mobileControlsObject.transform, new Color(0.08f, 0.12f, 0.18f, 0.72f));
            joystickBase.sprite = SpriteDatabase.Active.GetJoystickBase();
            UIFactory.SetRect(joystickBase.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(24f, 74f), new Vector2(118f, 118f));

            Image handle = UIFactory.Image("Handle", joystickBase.transform, new Color(0.36f, 0.76f, 0.98f, 0.88f));
            handle.sprite = SpriteDatabase.Active.GetJoystickHandle();
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
                mobileControlsObject.transform,
                "ВЗАИМОДЕЙСТВОВАТЬ",
                new Color(0.28f, 0.68f, 0.52f, 0.94f));
            UIFactory.SetRect(interactButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-22f, 104f), new Vector2(196f, 60f));

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

            questPanel = CreateModal(parent, "КВЕСТЫ И НАГРАДЫ");
            questContent = UIFactory.FindContent(questPanel);

            petsPanel = CreateModal(parent, "МОИ ПИТОМЦЫ");
            petsUI = gameObject.AddComponent<PetsPanel>();
            petsUI.Initialize(
                game,
                petCollection,
                UIFactory.FindContent(petsPanel),
                accessoryInventory,
                upgrades);

            accessoryPanel = CreateModal(parent, "ГАРДЕРОБ");
            wardrobeUI = gameObject.AddComponent<WardrobePanel>();
            wardrobeUI.Initialize(
                game,
                petCollection,
                accessoryInventory,
                UIFactory.FindContent(accessoryPanel));

            breedingPanel = CreateModal(parent, "ЭВОЛЮЦИЯ ВИДОВ");
            breedingUI = gameObject.AddComponent<BreedingPanel>();
            breedingUI.Initialize(game, petCollection, breeding, UIFactory.FindContent(breedingPanel));

            achievementPanel = CreateModal(parent, "ДОСТИЖЕНИЯ");
            achievementUI = gameObject.AddComponent<AchievementPanel>();
            achievementUI.Initialize(achievements, UIFactory.FindContent(achievementPanel));

            nestPanel = CreateModal(parent, "ЛОГОВИЩЕ");
            nestUI = gameObject.AddComponent<NestPanel>();
            nestUI.Initialize(
                game,
                creatureNests,
                nestPanel,
                UIFactory.FindContent(nestPanel));

            BuildAdoptionDialog(parent);
            BuildPauseMenu(parent);
            BuildSettingsMenu(parent);
            BuildIntro(parent);
            BuildDailyReward(parent);
        }

        private void BuildPauseMenu(Transform parent)
        {
            pausePanel = UIFactory.Object("PauseMenu", parent);
            UIFactory.Stretch(pausePanel.GetComponent<RectTransform>());
            Image dim = UIFactory.Image("Dim", pausePanel.transform, new Color(0.015f, 0.02f, 0.035f, 0.9f));
            UIFactory.Stretch(dim.rectTransform);

            Image card = UIFactory.Image("Card", pausePanel.transform, new Color(0.075f, 0.095f, 0.14f));
            UIFactory.ApplyRounded(card);
            UIFactory.AddSoftShadow(card.gameObject);
            UIFactory.SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 420f));

            Text heading = UIFactory.Text(
                "Heading", card.transform, "ПАУЗА", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-40f, 48f));
            heading.color = new Color(1f, 0.82f, 0.32f);

            pauseStatsText = UIFactory.Text(
                "Statistics", card.transform, "", 15, FontStyle.Normal, TextAnchor.UpperLeft);
            UIFactory.SetOffsets(pauseStatsText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(34f, 270f), new Vector2(-34f, -74f));
            pauseStatsText.color = new Color(0.82f, 0.87f, 0.94f);

            AddPauseButton(card.transform, "Continue", "ПРОДОЛЖИТЬ", -5f, ResumeGame,
                new Color(0.25f, 0.62f, 0.46f));
            AddPauseButton(card.transform, "Settings", "НАСТРОЙКИ", -61f, OpenSettingsFromPause,
                new Color(0.26f, 0.48f, 0.72f));
            AddPauseButton(card.transform, "Achievements", "ДОСТИЖЕНИЯ", -117f, OpenAchievements,
                new Color(0.52f, 0.38f, 0.7f));
            pausePanel.SetActive(false);
        }

        private void AddPauseButton(
            Transform parent,
            string name,
            string label,
            float y,
            UnityEngine.Events.UnityAction action,
            Color color)
        {
            Button button = UIFactory.Button(name, parent, label, color);
            UIFactory.SetRect(button.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(270f, 46f));
            button.onClick.AddListener(action);
        }

        private void BuildSettingsMenu(Transform parent)
        {
            settingsPanel = UIFactory.Object("SettingsMenu", parent);
            UIFactory.Stretch(settingsPanel.GetComponent<RectTransform>());
            Image dim = UIFactory.Image("Dim", settingsPanel.transform, new Color(0.015f, 0.02f, 0.035f, 0.92f));
            UIFactory.Stretch(dim.rectTransform);

            Image card = UIFactory.Image("Card", settingsPanel.transform, new Color(0.075f, 0.095f, 0.14f));
            UIFactory.ApplyRounded(card);
            UIFactory.SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 360f));

            Text heading = UIFactory.Text(
                "Heading", card.transform, "НАСТРОЙКИ", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-32f, 46f));
            heading.color = new Color(1f, 0.82f, 0.32f);

            BuildVolumeSlider(card.transform, "Музыка", 62f, true);
            BuildVolumeSlider(card.transform, "Звуки", -18f, false);

            Button back = UIFactory.Button(
                "Back", card.transform, "НАЗАД", new Color(0.32f, 0.42f, 0.56f));
            UIFactory.SetRect(back.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(190f, 46f));
            back.onClick.AddListener(ShowPauseMain);
            settingsPanel.SetActive(false);
        }

        private void BuildVolumeSlider(Transform parent, string label, float y, bool music)
        {
            Text caption = UIFactory.Text(
                label, parent, label, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIFactory.SetRect(caption.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-128f, y + 24f), new Vector2(170f, 30f));

            Text value = UIFactory.Text(
                "Value", parent, "", 15, FontStyle.Normal, TextAnchor.MiddleRight);
            UIFactory.SetRect(value.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(166f, y + 24f), new Vector2(70f, 30f));

            Slider slider = UIFactory.Slider(label + "Slider", parent);
            UIFactory.SetRect(slider.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(360f, 26f));
            slider.value = audioManager != null
                ? music ? audioManager.MusicVolume : audioManager.SfxVolume
                : 0.8f;
            value.text = Mathf.RoundToInt(slider.value * 100f) + "%";
            slider.onValueChanged.AddListener(newValue =>
            {
                value.text = Mathf.RoundToInt(newValue * 100f) + "%";
                if (audioManager != null)
                {
                    if (music)
                    {
                        audioManager.SetMusicVolume(newValue);
                    }
                    else
                    {
                        audioManager.SetSfxVolume(newValue);
                    }
                }
            });
        }

        private void BuildIntro(Transform parent)
        {
            introPanel = UIFactory.Object("IntroPanel", parent);
            UIFactory.Stretch(introPanel.GetComponent<RectTransform>());
            Image dim = UIFactory.Image("Dim", introPanel.transform, new Color(0.015f, 0.02f, 0.035f, 0.94f));
            UIFactory.Stretch(dim.rectTransform);
            Image card = UIFactory.Image("Card", introPanel.transform, new Color(0.085f, 0.11f, 0.17f));
            UIFactory.ApplyRounded(card);
            UIFactory.SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 370f));

            Text heading = UIFactory.Text(
                "Professor", card.transform, "ПРОФЕССОР МОНСТРОЛОГ", 27,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-36f, 48f));
            heading.color = new Color(1f, 0.82f, 0.32f);

            Text body = UIFactory.Text(
                "Body",
                card.transform,
                "Добро пожаловать, исследователь!\n" +
                "Этот мир полон странных существ.\n" +
                "Ищи следы, открывай биомы, собирай питомцев и заполняй энциклопедию.\n\n" +
                "Первая цель: исследуй Лес и найди первое существо.",
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            UIFactory.SetOffsets(body.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(40f, 92f), new Vector2(-40f, -82f));
            body.color = new Color(0.88f, 0.91f, 0.96f);

            Button start = UIFactory.Button(
                "Start", card.transform, "НАЧАТЬ ИССЛЕДОВАНИЕ",
                new Color(0.28f, 0.65f, 0.43f));
            UIFactory.SetRect(start.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(270f, 52f));
            start.onClick.AddListener(CompleteIntro);
            introPanel.SetActive(false);
        }

        private void BuildDailyReward(Transform parent)
        {
            dailyRewardPanel = UIFactory.Object("DailyRewardPanel", parent);
            UIFactory.Stretch(dailyRewardPanel.GetComponent<RectTransform>());
            Image dim = UIFactory.Image("Dim", dailyRewardPanel.transform, new Color(0.015f, 0.02f, 0.035f, 0.78f));
            UIFactory.Stretch(dim.rectTransform);
            Image card = UIFactory.Image("Card", dailyRewardPanel.transform, new Color(0.09f, 0.12f, 0.18f));
            UIFactory.ApplyRounded(card);
            UIFactory.SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470f, 280f));

            Text heading = UIFactory.Text(
                "Heading", card.transform, "ЕЖЕДНЕВНАЯ НАГРАДА", 24,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-30f, 42f));
            heading.color = new Color(1f, 0.82f, 0.32f);

            dailyRewardText = UIFactory.Text(
                "Reward", card.transform, "", 18, FontStyle.Normal, TextAnchor.MiddleCenter);
            UIFactory.SetOffsets(dailyRewardText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(28f, 92f), new Vector2(-28f, -72f));

            Button claim = UIFactory.Button(
                "Claim", card.transform, "ПОЛУЧИТЬ", new Color(0.28f, 0.65f, 0.43f));
            UIFactory.SetRect(claim.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                new Vector2(-8f, 22f), new Vector2(170f, 46f));
            claim.onClick.AddListener(ClaimDailyReward);

            Button later = UIFactory.Button(
                "Later", card.transform, "ПОЗЖЕ", new Color(0.34f, 0.38f, 0.46f));
            UIFactory.SetRect(later.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(8f, 22f), new Vector2(150f, 46f));
            later.onClick.AddListener(HideDailyReward);
            dailyRewardPanel.SetActive(false);
        }

        private void BuildAdoptionDialog(Transform parent)
        {
            GameObject root = UIFactory.Object("PetAdoptionDialog", parent);
            UIFactory.Stretch(root.GetComponent<RectTransform>());

            Image dim = UIFactory.Image("Dim", root.transform, new Color(0.015f, 0.02f, 0.035f, 0.9f));
            UIFactory.Stretch(dim.rectTransform);

            Image card = UIFactory.Image("Card", root.transform, new Color(0.09f, 0.12f, 0.19f));
            UIFactory.ApplyRounded(card);
            UIFactory.AddSoftShadow(card.gameObject);
            UIFactory.SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(570f, 350f));

            Text title = UIFactory.Text(
                "Title",
                card.transform,
                "НОВАЯ ВСТРЕЧА",
                26,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            UIFactory.SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-30f, 44f));
            title.color = new Color(1f, 0.82f, 0.34f);

            Image portrait = UIFactory.Image("Portrait", card.transform, Color.white);
            UIFactory.SetRect(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(32f, 22f), new Vector2(150f, 150f));
            portrait.preserveAspect = true;

            Text species = UIFactory.Text(
                "Species",
                card.transform,
                "",
                18,
                FontStyle.Normal,
                TextAnchor.UpperCenter);
            UIFactory.SetOffsets(species.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(210f, 126f), new Vector2(-30f, -76f));
            species.color = new Color(0.88f, 0.91f, 0.96f);

            InputField nameInput = UIFactory.InputField(
                "PetName",
                card.transform,
                "",
                "Имя питомца");
            UIFactory.SetRect(nameInput.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, -16f), new Vector2(320f, 46f));

            Button accept = UIFactory.Button(
                "Accept",
                card.transform,
                "СОХРАНИТЬ ИМЯ",
                new Color(0.28f, 0.65f, 0.43f));
            UIFactory.SetRect(accept.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                new Vector2(-10f, 24f), new Vector2(220f, 52f));

            Button decline = UIFactory.Button(
                "Decline",
                card.transform,
                "ОСТАВИТЬ ИМЯ ВИДА",
                new Color(0.43f, 0.34f, 0.4f));
            UIFactory.SetRect(decline.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(10f, 24f), new Vector2(260f, 52f));

            adoptionDialog = gameObject.AddComponent<PetAdoptionDialog>();
            adoptionDialog.Initialize(petCollection, root, portrait, species, nameInput, accept, decline);
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

        public void OnExploreButton()
        {
            if (world != null)
            {
                world.StartQuickSearch();
            }
        }

        public void RequestFullscreen()
        {
            WebGLPlatformBridge.RequestFullscreen();
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

        public void OpenQuests()
        {
            OpenPanel(questPanel);
            RebuildQuests();
        }

        public void OpenPets()
        {
            OpenPanel(petsPanel);
            petsUI.Rebuild();
        }

        public void OpenWardrobe()
        {
            OpenPanel(accessoryPanel);
            wardrobeUI.Rebuild();
        }

        public void OpenAccessories()
        {
            OpenWardrobe();
        }

        public void OpenBreeding()
        {
            OpenPanel(breedingPanel);
            breedingUI.Rebuild();
        }

        public void OpenAchievements()
        {
            if (isPaused)
            {
                pausePanel.SetActive(false);
                settingsPanel.SetActive(false);
            }

            OpenPanel(achievementPanel);
            achievementUI.Rebuild();
        }

        public void OpenNest(CreatureNestProgress progress)
        {
            if (progress == null || nestUI == null)
            {
                return;
            }

            OpenPanel(nestPanel);
            nestUI.Show(progress);
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

            biomeText.text = game.CurrentBiome.biomeName + "  |  " +
                             WorldEnvironmentSystem.Localize(game.CurrentTimeOfDay) + "  |  " +
                             WorldEnvironmentSystem.Localize(game.CurrentWeather) +
                             (biomeEvents != null && biomeEvents.ActiveEvent != null
                                 ? "  |  " + biomeEvents.ActiveEvent.displayName
                                 : "");
            coinsText.text = "МОНЕТЫ  " + game.Coins;
            RefreshEnergyText();
            tracksText.text = game.GetTrackSummary();
            Color tint = game.CurrentBiome.fallbackColor;
            tint.a = 0.06f;
            background.color = tint;
            background.sprite = SpriteDatabase.Active.GetBiomeBackground(
                game.CurrentBiome,
                game.CurrentBiome.mapData);
        }

        private void RefreshEnergyText()
        {
            if (energyText == null || game == null)
            {
                return;
            }

            string timer = game.Energy >= EnergyRegenerationSystem.MaxEnergy
                ? "МАКСИМУМ"
                : "следующая через " + EnergyRegenerationSystem.FormatTimer(
                    energyRegeneration != null
                        ? energyRegeneration.SecondsUntilNextEnergy
                        : EnergyRegenerationSystem.RegenerationSeconds);
            energyText.text = "ЭНЕРГИЯ " + game.Energy + "/" +
                              EnergyRegenerationSystem.MaxEnergy + "\n" + timer +
                              (starterBoost != null && starterBoost.IsActive ? " • БУСТ" : "");
        }

        private void ShowExplorationResult(ExplorationResult result)
        {
            if (result == null || resultCard == null)
            {
                return;
            }

            if (resultHideRoutine != null)
            {
                StopCoroutine(resultHideRoutine);
            }

            resultCard.SetActive(true);
            resultCanvasGroup.alpha = 1f;
            resultTitle.text = result.title;
            resultTitle.color = result.accentColor;
            resultDescription.text = result.description;
            resultIcon.sprite = result.icon;
            resultIcon.color = result.icon == null ? result.accentColor : Color.white;
            notificationText.text = "";
            resultHideRoutine = StartCoroutine(HideResultCardAfterDelay());

            if (result.firstSpeciesDiscovery && result.creature != null && adoptionDialog != null)
            {
                SetWorldInputEnabled(false);
                adoptionDialog.Show(result.creature, accepted =>
                {
                    ShowNotification(
                        result.creature.creatureName +
                        " теперь в коллекции питомцев.");
                    SetWorldInputEnabled(true);
                });
            }
        }

        private void ShowWelcome()
        {
            if (resultCard != null)
            {
                resultCard.SetActive(false);
            }

            if (game != null && !game.IntroCompleted)
            {
                introPanel.SetActive(true);
                SetWorldInputEnabled(false);
            }
            else
            {
                ShowDailyRewardIfAvailable();
                SetWorldInputEnabled(
                    dailyRewardPanel == null || !dailyRewardPanel.activeSelf);
            }
        }

        private IEnumerator HideResultCardAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3f);

            const float fadeDuration = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                resultCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            resultCard.SetActive(false);
            resultCanvasGroup.alpha = 1f;
            resultHideRoutine = null;
        }

        private void ShowNotification(string message)
        {
            notificationText.text = message;
        }

        public void ShowRewardedEnergy()
        {
            YandexGamesBridge bridge = YandexGamesBridge.Instance;
            if (bridge == null ||
                rewardedEnergyRequestPending ||
                bridge.IsRewardedRequestPending ||
                bridge.IsAdvertisementOpen)
            {
                return;
            }

            rewardedEnergyRequestPending = true;
            if (rewardEnergyButton != null)
            {
                rewardEnergyButton.interactable = false;
            }

            bridge.ShowRewardedAd(YandexGamesBridge.EnergyRewardId, () =>
            {
                game.AddEnergy(rewardedEnergyAmount);
                bridge.SaveProgress();
                RefreshHeader();
                ShowNotification(
                    "Награда за рекламу: +" +
                    rewardedEnergyAmount +
                    " энергии");
            });
        }

        private void HandleRewardedRequestFinished(bool rewarded)
        {
            rewardedEnergyRequestPending = false;
            if (rewardEnergyButton != null)
            {
                rewardEnergyButton.interactable = true;
            }

            if (!rewarded)
            {
                ShowNotification("Награда за рекламу не получена.");
            }
        }

        private void OpenPanel(GameObject panel)
        {
            HideStandardPanels();
            panel.SetActive(true);
            if (bottomBarRoot != null)
            {
                bottomBarRoot.SetAsLastSibling();
            }
            SetWorldInputEnabled(false);
        }

        public void CloseAllPanels()
        {
            HideStandardPanels();
            if (isPaused)
            {
                settingsPanel.SetActive(false);
                pausePanel.SetActive(true);
                RefreshPauseStatistics();
                SetWorldInputEnabled(false);
                return;
            }

            SetWorldInputEnabled(true);
        }

        private void HideStandardPanels()
        {
            encyclopediaPanel.SetActive(false);
            biomePanel.SetActive(false);
            questPanel.SetActive(false);
            petsPanel.SetActive(false);
            accessoryPanel.SetActive(false);
            breedingPanel.SetActive(false);
            achievementPanel.SetActive(false);
            nestPanel.SetActive(false);
        }

        private void SetWorldInputEnabled(bool value)
        {
            bool overlayBlocksInput = isPaused ||
                                      (introPanel != null && introPanel.activeSelf) ||
                                      (dailyRewardPanel != null && dailyRewardPanel.activeSelf);
            value = value && !overlayBlocksInput;
            if (player != null)
            {
                player.EnableMovement(value);
            }

            if (interaction != null)
            {
                interaction.EnableInteraction(value);
            }

            if (YandexGamesBridge.Instance != null)
            {
                YandexGamesBridge.Instance.SetGameplayAvailable(value);
            }
        }

        public void TogglePause()
        {
            if ((introPanel != null && introPanel.activeSelf) ||
                (dailyRewardPanel != null && dailyRewardPanel.activeSelf))
            {
                return;
            }

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                OpenPause();
            }
        }

        private void HandleEscape()
        {
            if (introPanel != null && introPanel.activeSelf)
            {
                return;
            }

            if (dailyRewardPanel != null && dailyRewardPanel.activeSelf)
            {
                HideDailyReward();
                return;
            }

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                ShowPauseMain();
                return;
            }

            if (HasOpenStandardPanel())
            {
                CloseAllPanels();
                return;
            }

            TogglePause();
        }

        private bool HasOpenStandardPanel()
        {
            return (encyclopediaPanel != null && encyclopediaPanel.activeSelf) ||
                   (biomePanel != null && biomePanel.activeSelf) ||
                   (questPanel != null && questPanel.activeSelf) ||
                   (petsPanel != null && petsPanel.activeSelf) ||
                   (accessoryPanel != null && accessoryPanel.activeSelf) ||
                   (breedingPanel != null && breedingPanel.activeSelf) ||
                   (achievementPanel != null && achievementPanel.activeSelf) ||
                   (nestPanel != null && nestPanel.activeSelf);
        }

        private void OpenPause()
        {
            HideStandardPanels();
            isPaused = true;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            settingsPanel.SetActive(false);
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
            RefreshPauseStatistics();
            SetWorldInputEnabled(false);
        }

        private void ResumeGame()
        {
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
            HideStandardPanels();
            isPaused = false;
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
            SetWorldInputEnabled(true);
        }

        private void OpenSettingsFromPause()
        {
            pausePanel.SetActive(false);
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
            SetWorldInputEnabled(false);
        }

        private void ShowPauseMain()
        {
            settingsPanel.SetActive(false);
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
            RefreshPauseStatistics();
            SetWorldInputEnabled(false);
        }

        private void RefreshPauseStatistics()
        {
            if (pauseStatsText == null || game == null)
            {
                return;
            }

            CreatureInstance favorite = petCollection != null ? petCollection.GetFavorite() : null;
            CreatureData favoriteSpecies = favorite != null
                ? game.GetCreature(favorite.speciesId)
                : null;
            pauseStatsText.text =
                "СТАТИСТИКА ИССЛЕДОВАТЕЛЯ\n" +
                "Исследований: " + game.ExplorationCount +
                "    Существ найдено: " + game.TotalCreaturesFound + "\n" +
                "Питомцев: " + (petCollection != null ? petCollection.Count : 0) +
                "    Биомов открыто: " + game.GetUnlockedBiomeCount() + "/" +
                game.Content.biomes.Count + "\n" +
                "Достижений: " + game.GetUnlockedAchievements().Count + "/" +
                (achievements != null ? achievements.GetDefinitions().Count : 0) + "\n" +
                "Любимчик: " +
                (favorite != null ? favorite.GetDisplayName(favoriteSpecies) : "не выбран") + "\n" +
                "Энциклопедия: " +
                Mathf.RoundToInt(game.GetEncyclopediaCompletion01() * 100f) + "%";
        }

        private void CompleteIntro()
        {
            if (game != null)
            {
                game.CompleteIntro(25, 25);
            }

            introPanel.SetActive(false);
            ShowNotification(
                "Первая цель: исследуйте Лес и найдите первое существо. Получено +25 энергии и +25 монет.");
            ShowDailyRewardIfAvailable();
            SetWorldInputEnabled(
                dailyRewardPanel == null || !dailyRewardPanel.activeSelf);
        }

        private void ShowDailyRewardIfAvailable()
        {
            if (dailyRewards == null || !dailyRewards.CanClaimToday ||
                dailyRewardPanel == null)
            {
                return;
            }

            int day = dailyRewards.NextRewardDay;
            dailyRewardText.text = "День " + day + " из 7\n" +
                                   dailyRewards.GetRewardDescription(day) +
                                   "\nПосле седьмого дня цикл начнётся снова.";
            dailyRewardPanel.SetActive(true);
            dailyRewardPanel.transform.SetAsLastSibling();
            SetWorldInputEnabled(false);
        }

        private void ClaimDailyReward()
        {
            if (dailyRewards == null)
            {
                return;
            }

            string message;
            dailyRewards.Claim(out message);
            dailyRewardPanel.SetActive(false);
            SetWorldInputEnabled(true);
        }

        private void HideDailyReward()
        {
            if (dailyRewardPanel != null)
            {
                dailyRewardPanel.SetActive(false);
            }

            SetWorldInputEnabled(true);
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

        public static Slider Slider(string name, Transform parent)
        {
            GameObject root = Object(name, parent);
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            Image background = Image("Background", root.transform, new Color(0.18f, 0.22f, 0.3f));
            ApplyRounded(background);
            SetRect(background.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 10f));

            GameObject fillArea = Object("Fill Area", root.transform);
            SetOffsets(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(6f, 6f), new Vector2(-6f, -6f));
            Image fill = Image("Fill", fillArea.transform, new Color(0.32f, 0.7f, 0.94f));
            ApplyRounded(fill);
            Stretch(fill.rectTransform);

            GameObject handleArea = Object("Handle Slide Area", root.transform);
            SetOffsets(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(8f, 0f), new Vector2(-8f, 0f));
            Image handle = Image("Handle", handleArea.transform, new Color(0.95f, 0.97f, 1f));
            handle.sprite = SpriteDatabase.Active.GetJoystickHandle();
            SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        public static InputField InputField(
            string name,
            Transform parent,
            string value,
            string placeholderValue)
        {
            Image root = Image(name, parent, new Color(0.14f, 0.18f, 0.25f));
            ApplyRounded(root);
            InputField input = root.gameObject.AddComponent<InputField>();

            Text text = Text("Text", root.transform, value, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetOffsets(text.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(14f, 6f), new Vector2(-14f, -6f));
            text.supportRichText = false;

            Text placeholder = Text(
                "Placeholder",
                root.transform,
                placeholderValue,
                16,
                FontStyle.Italic,
                TextAnchor.MiddleLeft);
            SetOffsets(placeholder.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(14f, 6f), new Vector2(-14f, -6f));
            placeholder.color = new Color(0.62f, 0.66f, 0.74f, 0.8f);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value ?? string.Empty;
            input.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            input.characterLimit = 24;
            input.targetGraphic = root;
            return input;
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
            get { return MonstrologyFontProvider.LegacyFont; }
        }

        private static Sprite RoundedSprite
        {
            get { return SpriteDatabase.Active.GetRoundedPanel(); }
        }
    }
}
