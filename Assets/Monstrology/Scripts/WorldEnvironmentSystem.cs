using System;
using UnityEngine;

namespace Monstrology
{
    public class WorldEnvironmentSystem : MonoBehaviour
    {
        [SerializeField, Min(30f)] private float dayLengthSeconds = 240f;
        [SerializeField, Min(15f)] private float weatherDurationSeconds = 90f;
        [SerializeField, Min(2f)] private float saveIntervalSeconds = 10f;

        private GameManager game;
        private float worldTime01;
        private float weatherTimer;
        private float saveTimer;
        private TimeOfDay timeOfDay;
        private WeatherType weather;

        public event Action EnvironmentChanged;

        public TimeOfDay CurrentTimeOfDay { get { return timeOfDay; } }
        public WeatherType CurrentWeather { get { return weather; } }

        public void Initialize(GameManager gameManager)
        {
            if (game != null)
            {
                game.ProgressReset -= HandleProgressReset;
            }

            game = gameManager;
            worldTime01 = game != null ? game.WorldTime01 : 0.3f;
            weatherTimer = game != null ? game.WeatherTimer : 0f;
            timeOfDay = GetTimeOfDay(worldTime01);
            weather = game != null ? game.CurrentWeather : WeatherType.Sunny;
            saveTimer = saveIntervalSeconds;
            if (game != null)
            {
                game.ProgressReset += HandleProgressReset;
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.ProgressReset -= HandleProgressReset;
            }
        }

        private void HandleProgressReset()
        {
            Initialize(game);
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            worldTime01 = Mathf.Repeat(
                worldTime01 + Time.deltaTime / Mathf.Max(30f, dayLengthSeconds),
                1f);
            weatherTimer += Time.deltaTime;
            saveTimer -= Time.deltaTime;

            TimeOfDay nextTime = GetTimeOfDay(worldTime01);
            bool changed = nextTime != timeOfDay;
            timeOfDay = nextTime;

            if (weatherTimer >= weatherDurationSeconds)
            {
                weatherTimer = 0f;
                weather = RollWeather(weather);
                changed = true;
            }

            if (changed || saveTimer <= 0f)
            {
                game.SaveWorldEnvironment(timeOfDay, weather, worldTime01, weatherTimer);
                saveTimer = saveIntervalSeconds;
            }

            if (changed && EnvironmentChanged != null)
            {
                EnvironmentChanged();
            }
        }

        private static TimeOfDay GetTimeOfDay(float value)
        {
            value = Mathf.Repeat(value, 1f);
            if (value < 0.25f) return TimeOfDay.Morning;
            if (value < 0.55f) return TimeOfDay.Day;
            if (value < 0.75f) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }

        private static WeatherType RollWeather(WeatherType current)
        {
            Array values = Enum.GetValues(typeof(WeatherType));
            WeatherType next = current;
            for (int attempt = 0; attempt < 4 && next == current; attempt++)
            {
                next = (WeatherType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            }

            return next;
        }

        public static string Localize(TimeOfDay value)
        {
            switch (value)
            {
                case TimeOfDay.Morning: return "Утро";
                case TimeOfDay.Evening: return "Вечер";
                case TimeOfDay.Night: return "Ночь";
                default: return "День";
            }
        }

        public static string Localize(WeatherType value)
        {
            switch (value)
            {
                case WeatherType.Rain: return "Дождь";
                case WeatherType.Fog: return "Туман";
                case WeatherType.MeteorShower: return "Метеоритный дождь";
                default: return "Солнечно";
            }
        }
    }
}
