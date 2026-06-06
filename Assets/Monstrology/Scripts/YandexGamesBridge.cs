using System;
using UnityEngine;

namespace Monstrology
{
    public class YandexGamesBridge : MonoBehaviour
    {
        public static YandexGamesBridge Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void ShowRewardedAd(Action<bool> callback)
        {
            Debug.Log("YandexGamesBridge stub: rewarded ad completed.");
            if (callback != null)
            {
                callback(true);
            }
        }

        public void SaveProgress()
        {
            // Replace with Yandex Games cloud save SDK call.
            Debug.Log("YandexGamesBridge stub: local progress is ready for cloud save.");
        }

        public void LoadProgress()
        {
            // Replace with Yandex Games cloud load SDK call.
            Debug.Log("YandexGamesBridge stub: using local PlayerPrefs progress.");
        }

        public void ShowLeaderboard()
        {
            Debug.Log("YandexGamesBridge stub: leaderboard is not connected yet.");
        }
    }
}
