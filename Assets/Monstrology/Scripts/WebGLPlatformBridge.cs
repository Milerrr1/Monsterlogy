using System.Runtime.InteropServices;
using UnityEngine;

namespace Monstrology
{
    public static class WebGLPlatformBridge
    {
        private static readonly Rect FullNormalizedSafeArea =
            new Rect(0f, 0f, 1f, 1f);

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int Monstrology_IsMobileUserAgent();

        [DllImport("__Internal")]
        private static extern int Monstrology_RequestFullscreen();

        [DllImport("__Internal")]
        private static extern float Monstrology_GetSafeInsetLeft();

        [DllImport("__Internal")]
        private static extern float Monstrology_GetSafeInsetRight();

        [DllImport("__Internal")]
        private static extern float Monstrology_GetSafeInsetTop();

        [DllImport("__Internal")]
        private static extern float Monstrology_GetSafeInsetBottom();
#endif

        public static bool IsMobileUserAgent()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return Monstrology_IsMobileUserAgent() == 1;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        public static bool RequestFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return Monstrology_RequestFullscreen() == 1;
            }
            catch
            {
                return false;
            }
#else
            Screen.fullScreen = !Screen.fullScreen;
            return true;
#endif
        }

        public static Rect GetNormalizedSafeArea()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return NormalizeSafeAreaInsets(
                    Monstrology_GetSafeInsetLeft(),
                    Monstrology_GetSafeInsetRight(),
                    Monstrology_GetSafeInsetTop(),
                    Monstrology_GetSafeInsetBottom());
            }
            catch
            {
                return FullNormalizedSafeArea;
            }
#else
            return FullNormalizedSafeArea;
#endif
        }

        public static Rect NormalizeSafeAreaInsets(
            float left,
            float right,
            float top,
            float bottom)
        {
            if (!IsNormalizedInset(left) ||
                !IsNormalizedInset(right) ||
                !IsNormalizedInset(top) ||
                !IsNormalizedInset(bottom) ||
                left + right >= 1f ||
                top + bottom >= 1f)
            {
                return FullNormalizedSafeArea;
            }

            Rect safeArea = Rect.MinMaxRect(
                left,
                bottom,
                1f - right,
                1f - top);
            return safeArea.width > 0f && safeArea.height > 0f
                ? safeArea
                : FullNormalizedSafeArea;
        }

        private static bool IsNormalizedInset(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   value >= 0f &&
                   value <= 1f;
        }
    }
}
