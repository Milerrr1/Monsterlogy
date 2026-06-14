using System;
using System.Collections;
using UnityEngine;
using YG;

namespace Monstrology
{
    public class YandexGamesBridge : MonoBehaviour
    {
        public const string EnergyRewardId = "energy_3";

        public static YandexGamesBridge Instance { get; private set; }

        public event Action<bool> RewardedRequestFinished;

        private bool sdkInitializedLogged;
        private bool rewardedRequestPending;
        private bool rewardDelivered;
        private bool advertisementOpen;
        private bool pluginPauseActive;
        private bool browserFocused = true;
        private bool platformStateCaptured;
        private bool gameplayAvailable;
        private bool gameplayIsActive;
        private bool gameReadySent;

        private int rewardRequestToken;
        private float timeScaleBeforePlatformPause = 1f;
        private bool audioPauseBeforePlatformPause;
        private bool movementBeforePlatformPause = true;
        private bool interactionBeforePlatformPause = true;
        private PlayerController2D player;
        private InteractionSystem interaction;
        private Coroutine restoreRoutine;
        private Coroutine editorRewardRoutine;

        public bool IsAdvertisementOpen { get { return advertisementOpen; } }
        public bool IsRewardedRequestPending { get { return rewardedRequestPending; } }
        public bool IsGameplayAvailable { get { return gameplayAvailable; } }
        public bool GameplayIsActive { get { return gameplayIsActive; } }
        public bool IsGameReadySent { get { return gameReadySent; } }
        public string LastRewardId { get; private set; } = "none";
        public string LastSaveStatus { get; private set; } = "not saved";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            YG2.onGetSDKData += HandleSDKInitialized;
            YG2.onOpenAnyAdv += HandleAdvertisementOpened;
            YG2.onCloseAnyAdv += HandleAdvertisementClosed;
            YG2.onPauseGame += HandlePluginPause;
            YG2.onFocusWindowGame += HandleBrowserFocus;
#if RewardedAdv_yg
            YG2.onErrorRewardedAdv += HandleRewardedError;
#endif
#if InterstitialAdv_yg
            YG2.onErrorInterAdv += HandleInterstitialError;
#endif

#if UNITY_EDITOR
            browserFocused = true;
#else
            browserFocused = YG2.isFocusWindowGame;
#endif
            if (IsSDKReady())
            {
                HandleSDKInitialized();
            }
        }

        private void OnDisable()
        {
            YG2.onGetSDKData -= HandleSDKInitialized;
            YG2.onOpenAnyAdv -= HandleAdvertisementOpened;
            YG2.onCloseAnyAdv -= HandleAdvertisementClosed;
            YG2.onPauseGame -= HandlePluginPause;
            YG2.onFocusWindowGame -= HandleBrowserFocus;
#if RewardedAdv_yg
            YG2.onErrorRewardedAdv -= HandleRewardedError;
#endif
#if InterstitialAdv_yg
            YG2.onErrorInterAdv -= HandleInterstitialError;
#endif

            if (editorRewardRoutine != null)
            {
                StopCoroutine(editorRewardRoutine);
                editorRewardRoutine = null;
            }

            if (platformStateCaptured)
            {
                RestorePlatformState();
            }
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool IsSDKReady()
        {
#if UNITY_EDITOR
            return true;
#else
            return YG2.isSDKEnabled;
#endif
        }

        public void ShowRewardedAd(string rewardId, Action onReward)
        {
            if (rewardedRequestPending || advertisementOpen || YG2.nowAdsShow)
            {
                Debug.LogWarning("[YandexBridge] Rewarded request ignored: an ad is active.");
                return;
            }

            if (!IsSDKReady())
            {
                Debug.LogWarning("[YandexBridge] Rewarded request ignored: SDK is not ready.");
                RaiseRewardedFinished(false);
                return;
            }

            rewardRequestToken++;
            int requestToken = rewardRequestToken;
            rewardedRequestPending = true;
            rewardDelivered = false;
            LastRewardId = string.IsNullOrEmpty(rewardId) ? "none" : rewardId;
            Debug.Log("[YandexBridge] Rewarded ad requested: " + LastRewardId);

#if UNITY_EDITOR
            Debug.Log("[YandexBridge] Rewarded ad simulated in Unity Editor.");
            editorRewardRoutine = StartCoroutine(
                SimulateRewardedAd(requestToken, onReward));
#elif RewardedAdv_yg
            try
            {
                YG2.RewardedAdvShow(
                    rewardId,
                    () => DeliverReward(requestToken, onReward));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[YandexBridge] Rewarded ad could not be shown: " +
                    exception.Message);
                FinishRewardedRequest(false);
            }
#else
            Debug.LogError(
                "[YandexBridge] RewardedAdv module is missing. " +
                "Install PluginYG2 module RewardedAdv.");
            FinishRewardedRequest(false);
#endif
        }

        public void ShowRewardedAd(Action<bool> callback)
        {
            ShowRewardedAd(
                "legacy_reward",
                () =>
                {
                    if (callback != null)
                    {
                        callback(true);
                    }
                });
        }

        public void ShowInterstitialAd()
        {
            if (advertisementOpen || YG2.nowAdsShow)
            {
                Debug.LogWarning(
                    "[YandexBridge] Interstitial request ignored: an ad is active.");
                return;
            }

            if (!IsSDKReady())
            {
                Debug.LogWarning(
                    "[YandexBridge] Interstitial request ignored: SDK is not ready.");
                return;
            }

#if InterstitialAdv_yg
            Debug.Log("[YandexBridge] Interstitial requested.");
            try
            {
                YG2.InterstitialAdvShow();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[YandexBridge] Interstitial ad could not be shown: " +
                    exception.Message);
                HandleAdvertisementError();
            }
#else
            Debug.LogError(
                "[YandexBridge] InterstitialAdv module is missing. " +
                "Install PluginYG2 module InterstitialAdv.");
#endif
        }

        public void NotifyGameReady()
        {
            if (gameReadySent || !gameplayAvailable || !IsSDKReady())
            {
                return;
            }

            if (YG2.infoYG.Basic.autoGRA)
            {
                gameReadySent = true;
                return;
            }

            gameReadySent = true;
            YG2.GameReadyAPI();
            Debug.Log("[YandexBridge] Game Ready sent.");
        }

        public void NotifyGameplayStarted()
        {
            gameplayAvailable = true;
            NotifyGameReady();
            RefreshGameplayState();
        }

        public void NotifyGameplayStopped()
        {
            gameplayAvailable = false;
            RefreshGameplayState();
        }

        public void SavePlatformProgress()
        {
            try
            {
                SaveSystem.Flush();
                PlayerPrefs.Save();
                LastSaveStatus =
                    "saved " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception exception)
            {
                LastSaveStatus = "failed: " + exception.Message;
                Debug.LogWarning(
                    "[YandexBridge] Progress flush failed: " +
                    exception.Message);
            }
        }

        public void SaveProgress()
        {
            SavePlatformProgress();
        }

        public void LoadProgress()
        {
            // GameManager loads the existing PlayerPrefs save through SaveSystem.
        }

        public void ShowLeaderboard()
        {
            Debug.Log("[YandexBridge] Leaderboard is not connected yet.");
        }

        public void SetGameplayAvailable(bool value)
        {
            if (value)
            {
                NotifyGameplayStarted();
            }
            else
            {
                NotifyGameplayStopped();
            }
        }

        private IEnumerator SimulateRewardedAd(
            int requestToken,
            Action onReward)
        {
            advertisementOpen = true;
            BeginPlatformPause();
            yield return new WaitForSecondsRealtime(0.25f);

            DeliverReward(requestToken, onReward);
            yield return null;

            advertisementOpen = false;
            editorRewardRoutine = null;
            FinishRewardedRequest(rewardDelivered);
            TryEndPlatformPause();
        }

        private void DeliverReward(int requestToken, Action onReward)
        {
            if (!rewardedRequestPending ||
                rewardDelivered ||
                requestToken != rewardRequestToken)
            {
                return;
            }

            rewardDelivered = true;
            Debug.Log("[YandexBridge] Reward granted: " + LastRewardId);
            if (onReward != null)
            {
                try
                {
                    onReward();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void FinishRewardedRequest(bool success)
        {
            if (!rewardedRequestPending)
            {
                return;
            }

            rewardedRequestPending = false;
            bool completedWithReward = success && rewardDelivered;
            RaiseRewardedFinished(completedWithReward);
            rewardDelivered = false;
        }

        private void RaiseRewardedFinished(bool success)
        {
            if (RewardedRequestFinished != null)
            {
                RewardedRequestFinished(success);
            }
        }

        private void HandleSDKInitialized()
        {
            if (!sdkInitializedLogged)
            {
                sdkInitializedLogged = true;
                Debug.Log("[YandexBridge] SDK initialized.");
            }

            if (YG2.infoYG.Basic.autoGRA)
            {
                gameReadySent = true;
            }

            if (gameplayAvailable)
            {
                NotifyGameReady();
            }

            RefreshGameplayState();
        }

        private void RefreshGameplayState()
        {
            bool shouldStart = gameplayAvailable &&
                               browserFocused &&
                               !pluginPauseActive &&
                               !advertisementOpen &&
                               IsSDKReady();
            if (shouldStart == gameplayIsActive)
            {
                return;
            }

            gameplayIsActive = shouldStart;
            if (gameplayIsActive)
            {
                NotifyGameReady();
                YG2.GameplayStart();
                Debug.Log("[YandexBridge] Gameplay started.");
            }
            else
            {
                YG2.GameplayStop();
                Debug.Log("[YandexBridge] Gameplay stopped.");
            }
        }

        private void HandleAdvertisementOpened()
        {
            advertisementOpen = true;
            BeginPlatformPause();
        }

        private void HandleAdvertisementClosed()
        {
            advertisementOpen = false;
            FinishRewardedRequest(rewardDelivered);
            TryEndPlatformPause();
        }

        private void HandlePluginPause(bool pause)
        {
            pluginPauseActive = pause;
            if (pause)
            {
                BeginPlatformPause();
            }
            else
            {
                TryEndPlatformPause();
            }
        }

        private void HandleBrowserFocus(bool focused)
        {
#if UNITY_EDITOR
            if (Application.isBatchMode)
            {
                return;
            }
#endif
            browserFocused = focused;
            if (!focused)
            {
                SaveProgress();
                BeginPlatformPause();
            }
            else
            {
                TryEndPlatformPause();
            }
        }

        private void BeginPlatformPause()
        {
            if (restoreRoutine != null)
            {
                StopCoroutine(restoreRoutine);
                restoreRoutine = null;
            }

            if (!platformStateCaptured)
            {
                ResolveGameplayObjects();
                timeScaleBeforePlatformPause = Time.timeScale;
                audioPauseBeforePlatformPause = AudioListener.pause;
                movementBeforePlatformPause =
                    player == null || player.MovementEnabled;
                interactionBeforePlatformPause =
                    interaction == null || interaction.InteractionEnabled;
                platformStateCaptured = true;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
            SetGameplayInput(false);
            RefreshGameplayState();
        }

        private void TryEndPlatformPause()
        {
            if (advertisementOpen || pluginPauseActive || !browserFocused)
            {
                RefreshGameplayState();
                return;
            }

            if (restoreRoutine != null)
            {
                StopCoroutine(restoreRoutine);
            }

            restoreRoutine = StartCoroutine(RestoreAfterPlatformPause());
        }

        private IEnumerator RestoreAfterPlatformPause()
        {
            yield return null;
            restoreRoutine = null;
            if (advertisementOpen || pluginPauseActive || !browserFocused)
            {
                yield break;
            }

            RestorePlatformState();
            RefreshGameplayState();
        }

        private void RestorePlatformState()
        {
            if (!platformStateCaptured)
            {
                return;
            }

            Time.timeScale = timeScaleBeforePlatformPause;
            AudioListener.pause = audioPauseBeforePlatformPause;
            ResolveGameplayObjects();
            if (player != null)
            {
                player.EnableMovement(
                    movementBeforePlatformPause && gameplayAvailable);
            }

            if (interaction != null)
            {
                interaction.EnableInteraction(
                    interactionBeforePlatformPause && gameplayAvailable);
            }

            platformStateCaptured = false;
        }

#if RewardedAdv_yg
        private void HandleRewardedError()
        {
            YG2.nowRewardAdv = false;
            FinishRewardedRequest(false);
            HandleAdvertisementError();
        }
#endif

#if InterstitialAdv_yg
        private void HandleInterstitialError()
        {
            YG2.nowInterAdv = false;
            HandleAdvertisementError();
        }
#endif

        private void HandleAdvertisementError()
        {
            advertisementOpen = YG2.nowAdsShow;
            if (advertisementOpen)
            {
                return;
            }

            if (YG2.isPauseGame)
            {
                YG2.PauseGame(false);
            }
            else
            {
                pluginPauseActive = false;
                TryEndPlatformPause();
            }
        }

        private void ResolveGameplayObjects()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController2D>();
            }

            if (interaction == null)
            {
                interaction = FindObjectOfType<InteractionSystem>();
            }
        }

        private void SetGameplayInput(bool value)
        {
            ResolveGameplayObjects();
            if (player != null)
            {
                player.EnableMovement(value);
            }

            if (interaction != null)
            {
                interaction.EnableInteraction(value);
            }
        }
    }
}
