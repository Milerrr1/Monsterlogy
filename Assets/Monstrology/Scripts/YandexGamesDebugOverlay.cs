using UnityEngine;

namespace Monstrology
{
    public class YandexGamesDebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GUIStyle labelStyle;
        private bool expanded;
        private bool displayStateInitialized;

        private void OnGUI()
        {
            YandexGamesBridge bridge = YandexGamesBridge.Instance;
            if (bridge == null)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 13;
                labelStyle.normal.textColor = Color.white;
            }

            if (!displayStateInitialized)
            {
                expanded =
                    AdaptiveUIController.CurrentMode == ControlMode.Desktop;
                displayStateInitialized = true;
            }

            float buttonX = Mathf.Max(12f, Screen.width - 58f);
            if (!expanded)
            {
                if (GUI.Button(new Rect(buttonX, 48f, 46f, 30f), "DBG"))
                {
                    expanded = true;
                }

                return;
            }

            if (GUI.Button(new Rect(buttonX, 48f, 46f, 30f), "X"))
            {
                expanded = false;
                return;
            }

            GameManager game = GameManager.Instance;
            string energy = game != null ? game.Energy.ToString() : "n/a";
            float panelX = Mathf.Max(12f, Screen.width - 289f);
            Rect panel = new Rect(panelX, 86f, 277f, 168f);
            GUI.Box(panel, "Yandex Demo Diagnostics");
            GUILayout.BeginArea(new Rect(panelX + 12f, 112f, 252f, 134f));
            GUILayout.Label("SDK Ready: " + bridge.IsSDKReady(), labelStyle);
            GUILayout.Label("Game Ready sent: " + bridge.IsGameReadySent, labelStyle);
            GUILayout.Label("Gameplay Active: " + bridge.GameplayIsActive, labelStyle);
            GUILayout.Label("Ad Open: " + bridge.IsAdvertisementOpen, labelStyle);
            GUILayout.Label("Last Reward ID: " + bridge.LastRewardId, labelStyle);
            GUILayout.Label("Current Energy: " + energy, labelStyle);
            GUILayout.Label("Last Save: " + bridge.LastSaveStatus, labelStyle);
            GUILayout.EndArea();
        }
#endif
    }
}
