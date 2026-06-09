using System;
using System.Globalization;
using UnityEngine;

namespace Monstrology
{
    public class EnergyRegenerationSystem : MonoBehaviour
    {
        public const int MaxEnergy = 100;
        public const int RegenerationSeconds = 45;

        private GameManager game;
        private DateTime energyClockUtc;
        private float refreshTimer;
        private int lastObservedEnergy;

        public int SecondsUntilNextEnergy
        {
            get
            {
                if (game == null || game.Energy >= MaxEnergy)
                {
                    return 0;
                }

                double elapsed = Math.Max(0d, (DateTime.UtcNow - energyClockUtc).TotalSeconds);
                return Mathf.Clamp(
                    RegenerationSeconds - (int)(elapsed % RegenerationSeconds),
                    1,
                    RegenerationSeconds);
            }
        }

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
            DateTime parsed;
            energyClockUtc = DateTime.TryParse(
                game != null ? game.GetEnergyClockUtc() : string.Empty,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed)
                ? parsed.ToUniversalTime()
                : DateTime.UtcNow;
            lastObservedEnergy = game != null ? game.Energy : 0;
            ApplyElapsedEnergy(true);
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.25f;
            if (lastObservedEnergy >= MaxEnergy && game.Energy < MaxEnergy)
            {
                energyClockUtc = DateTime.UtcNow;
                game.ApplyEnergyRegeneration(0, energyClockUtc);
            }

            ApplyElapsedEnergy(false);
            lastObservedEnergy = game.Energy;
        }

        private void ApplyElapsedEnergy(bool offline)
        {
            if (game == null)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (game.Energy >= MaxEnergy)
            {
                energyClockUtc = now;
                return;
            }

            double elapsedSeconds = Math.Max(0d, (now - energyClockUtc).TotalSeconds);
            int restored = (int)(elapsedSeconds / RegenerationSeconds);
            if (restored <= 0)
            {
                return;
            }

            int before = game.Energy;
            int actual = Mathf.Min(restored, MaxEnergy - before);
            energyClockUtc = before + actual >= MaxEnergy
                ? now
                : energyClockUtc.AddSeconds(actual * RegenerationSeconds);
            game.ApplyEnergyRegeneration(actual, energyClockUtc);
            if (offline && actual > 0)
            {
                game.RaiseNotification("Энергия восстановлена: +" + actual + ".");
            }
        }

        public static string FormatTimer(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
    }
}
