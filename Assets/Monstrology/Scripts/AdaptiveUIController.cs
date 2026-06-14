using System;
using UnityEngine;
using UnityEngine.UI;

namespace Monstrology
{
    public enum ControlMode
    {
        Desktop,
        Mobile
    }

    public class AdaptiveUIController : MonoBehaviour
    {
        private static ControlMode? forcedMode;

        private RectTransform safeAreaRoot;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private RectTransform mobileControls;
        private GameObject mobileControlsRoot;
        private GameObject fullscreenButton;
        private Text title;
        private InteractionSystem interaction;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastUnitySafeArea = new Rect(-1f, -1f, -1f, -1f);
        private ControlMode currentMode;
        private bool initialized;
        private bool invalidSafeAreaWarningLogged;

        public static ControlMode CurrentMode
        {
            get
            {
                if (forcedMode.HasValue)
                {
                    return forcedMode.Value;
                }

                return DetectMode();
            }
        }

        public ControlMode Mode { get { return currentMode; } }
        public RectTransform SafeAreaRoot { get { return safeAreaRoot; } }

        public void Initialize(
            RectTransform safeRoot,
            RectTransform topBarRect,
            RectTransform bottomBarRect,
            RectTransform mobileControlsRect,
            GameObject mobileRoot,
            GameObject fullscreenControl,
            Text gameTitle,
            InteractionSystem interactionSystem)
        {
            safeAreaRoot = safeRoot;
            topBar = topBarRect;
            bottomBar = bottomBarRect;
            mobileControls = mobileControlsRect;
            mobileControlsRoot = mobileRoot;
            fullscreenButton = fullscreenControl;
            title = gameTitle;
            interaction = interactionSystem;
            initialized = true;
            Refresh(true);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (Screen.width != lastScreenWidth ||
                Screen.height != lastScreenHeight ||
                safeArea != lastUnitySafeArea ||
                currentMode != CurrentMode)
            {
                Refresh(false);
            }
        }

        public void Refresh(bool force)
        {
            if (!initialized)
            {
                return;
            }

            ControlMode detected = CurrentMode;
            bool modeChanged = force || detected != currentMode;
            currentMode = detected;
            if (modeChanged)
            {
                ApplyControlMode();
            }

            ApplySafeArea();
            ApplyLayout();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastUnitySafeArea = Screen.safeArea;
        }

        public static void SetForcedModeForTests(ControlMode? mode)
        {
            forcedMode = mode;
        }

        public static ControlMode DetectMode()
        {
#if UNITY_EDITOR
            return ControlMode.Desktop;
#else
            if (Application.isMobilePlatform ||
                WebGLPlatformBridge.IsMobileUserAgent())
            {
                return ControlMode.Mobile;
            }

            bool handheldTouch =
                Input.touchSupported &&
                SystemInfo.deviceType == DeviceType.Handheld;
            return handheldTouch ? ControlMode.Mobile : ControlMode.Desktop;
#endif
        }

        private void ApplyControlMode()
        {
            bool mobile = currentMode == ControlMode.Mobile;
            if (mobileControlsRoot != null)
            {
                mobileControlsRoot.SetActive(mobile);
            }

            if (fullscreenButton != null)
            {
                fullscreenButton.SetActive(mobile);
            }

            if (title != null)
            {
                title.gameObject.SetActive(!mobile);
            }

            if (interaction != null)
            {
                interaction.SetMobileMode(mobile);
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect normalizedArea = new Rect(0f, 0f, 1f, 1f);
            try
            {
                Rect unityArea = Screen.safeArea;
                Rect normalizedUnityArea = Rect.MinMaxRect(
                    unityArea.xMin / Screen.width,
                    unityArea.yMin / Screen.height,
                    unityArea.xMax / Screen.width,
                    unityArea.yMax / Screen.height);
                Rect webArea =
                    WebGLPlatformBridge.GetNormalizedSafeArea();
                Rect combinedArea = Rect.MinMaxRect(
                    Mathf.Max(
                        normalizedUnityArea.xMin,
                        webArea.xMin),
                    Mathf.Max(
                        normalizedUnityArea.yMin,
                        webArea.yMin),
                    Mathf.Min(
                        normalizedUnityArea.xMax,
                        webArea.xMax),
                    Mathf.Min(
                        normalizedUnityArea.yMax,
                        webArea.yMax));

                if (!TryValidateNormalizedSafeArea(
                        combinedArea,
                        out normalizedArea))
                {
                    WarnInvalidSafeAreaOnce();
                }
            }
            catch (Exception)
            {
                WarnInvalidSafeAreaOnce();
            }

            safeAreaRoot.anchorMin =
                new Vector2(normalizedArea.xMin, normalizedArea.yMin);
            safeAreaRoot.anchorMax =
                new Vector2(normalizedArea.xMax, normalizedArea.yMax);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        public static bool TryValidateNormalizedSafeArea(
            Rect candidate,
            out Rect safeArea)
        {
            safeArea = new Rect(0f, 0f, 1f, 1f);
            if (!IsFinite(candidate.xMin) ||
                !IsFinite(candidate.yMin) ||
                !IsFinite(candidate.xMax) ||
                !IsFinite(candidate.yMax) ||
                candidate.xMin < 0f ||
                candidate.yMin < 0f ||
                candidate.xMax > 1f ||
                candidate.yMax > 1f ||
                candidate.xMax <= candidate.xMin ||
                candidate.yMax <= candidate.yMin)
            {
                return false;
            }

            safeArea = candidate;
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void WarnInvalidSafeAreaOnce()
        {
            if (invalidSafeAreaWarningLogged)
            {
                return;
            }

            invalidSafeAreaWarningLogged = true;
            Debug.LogWarning(
                "Invalid safe-area data. Full-screen UI fallback is active.");
        }

        private void ApplyLayout()
        {
            bool mobile = currentMode == ControlMode.Mobile;
            if (topBar != null)
            {
                topBar.sizeDelta = new Vector2(0f, mobile ? 80f : 72f);
            }

            if (bottomBar != null)
            {
                bottomBar.sizeDelta = new Vector2(0f, mobile ? 70f : 58f);
                foreach (Text label in bottomBar.GetComponentsInChildren<Text>(true))
                {
                    label.fontSize = mobile ? 11 : 14;
                }
            }

            if (mobileControls != null)
            {
                mobileControls.anchorMin = Vector2.zero;
                mobileControls.anchorMax = Vector2.one;
                mobileControls.offsetMin = Vector2.zero;
                mobileControls.offsetMax = Vector2.zero;
            }
        }
    }
}
