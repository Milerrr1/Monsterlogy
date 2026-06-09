using System;
using UnityEngine;

namespace Monstrology
{
    [CreateAssetMenu(fileName = "BiomeEvent", menuName = "Monstrology/Biome Event")]
    public class BiomeEventData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public BiomeType biome;
        [Min(10f)] public float durationSeconds = 60f;
        [Range(1f, 4f)] public float rareCreatureMultiplier = 1.5f;
        [Range(1f, 4f)] public float biomeResourceMultiplier = 1.5f;
        public string specialObjectName;
    }

    public class BiomeEventSystem : MonoBehaviour
    {
        [SerializeField, Min(10f)] private float eventCheckInterval = 45f;
        [SerializeField, Range(0f, 1f)] private float eventChance = 0.18f;

        private GameManager game;
        private BiomeEventData activeEvent;
        private float activeTimer;
        private float checkTimer;

        public event Action<BiomeEventData> EventStarted;
        public event Action EventEnded;

        public BiomeEventData ActiveEvent { get { return activeEvent; } }
        public float RemainingSeconds { get { return Mathf.Max(0f, activeTimer); } }
        public float RareCreatureMultiplier
        {
            get { return activeEvent != null ? activeEvent.rareCreatureMultiplier : 1f; }
        }
        public float ResourceMultiplier
        {
            get { return activeEvent != null ? activeEvent.biomeResourceMultiplier : 1f; }
        }

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
            activeEvent = null;
            activeTimer = 0f;
            checkTimer = eventCheckInterval;
        }

        private void Update()
        {
            if (game == null || game.CurrentBiome == null)
            {
                return;
            }

            if (activeEvent != null)
            {
                if (activeEvent.biome != game.CurrentBiome.type)
                {
                    EndEvent();
                    return;
                }

                activeTimer -= Time.deltaTime;
                if (activeTimer <= 0f)
                {
                    EndEvent();
                }

                return;
            }

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
            {
                return;
            }

            checkTimer = eventCheckInterval;
            if (UnityEngine.Random.value <= eventChance)
            {
                TryStartEvent(game.CurrentBiome.type);
            }
        }

        public bool TryStartEvent(BiomeType biome)
        {
            if (game == null || activeEvent != null)
            {
                return false;
            }

            BiomeEventData selected = game.Content.biomeEvents.Find(data =>
                data != null && data.biome == biome);
            if (selected == null)
            {
                return false;
            }

            activeEvent = selected;
            activeTimer = Mathf.Max(10f, selected.durationSeconds);
            game.RegisterBiomeEvent(selected.id);
            game.RaiseNotification("Событие: " + selected.displayName + ". " + selected.description);
            if (EventStarted != null)
            {
                EventStarted(selected);
            }

            return true;
        }

        public bool IsActiveFor(BiomeType biome)
        {
            return activeEvent != null && activeEvent.biome == biome;
        }

        private void EndEvent()
        {
            activeEvent = null;
            activeTimer = 0f;
            checkTimer = eventCheckInterval;
            if (EventEnded != null)
            {
                EventEnded();
            }
        }
    }
}
