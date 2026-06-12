using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class StarterBoostSystem : MonoBehaviour
    {
        public const float DurationSeconds = 15f * 60f;
        private const float RewardIntervalSeconds = 60f;

        private GameManager game;
        private float playedSeconds;
        private int rewardIndex;
        private float saveTimer;

        public bool IsActive { get { return playedSeconds < DurationSeconds; } }
        public float RemainingSeconds { get { return Mathf.Max(0f, DurationSeconds - playedSeconds); } }

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
            playedSeconds = game != null ? game.StarterBoostPlaySeconds : DurationSeconds;
            rewardIndex = game != null ? game.GetStarterBoostRewardIndex() : 0;
            saveTimer = 10f;
        }

        private void Update()
        {
            if (game == null || !IsActive)
            {
                return;
            }

            playedSeconds = Mathf.Min(DurationSeconds, playedSeconds + Time.unscaledDeltaTime);
            int expectedRewardIndex = Mathf.FloorToInt(playedSeconds / RewardIntervalSeconds);
            while (rewardIndex < expectedRewardIndex)
            {
                rewardIndex++;
                GrantCarePackage(rewardIndex);
            }

            saveTimer -= Time.unscaledDeltaTime;
            if (saveTimer <= 0f || !IsActive)
            {
                game.SaveStarterBoost(playedSeconds, rewardIndex);
                saveTimer = 10f;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && game != null)
            {
                game.SaveStarterBoost(playedSeconds, rewardIndex);
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.SaveStarterBoost(playedSeconds, rewardIndex);
            }
        }

        private void GrantCarePackage(int index)
        {
            game.AddEnergy(2);
            ItemData resource = SelectBiomeResource();
            if (resource != null)
            {
                game.AddItem(resource.id, 1);
            }

            if (index % 3 == 0)
            {
                CreatureData creature = SelectStarterCreature();
                if (creature != null)
                {
                    game.AddCreature(creature);
                }
            }

            if (index % 5 == 0)
            {
                game.AddCoins(60);
            }

            game.RaiseNotification(
                "Буст новичка: +2 энергии" +
                (resource != null ? ", +1 " + resource.itemName : "") +
                (index % 3 == 0 ? ", бонусная встреча" : "") + ".");
        }

        private ItemData SelectBiomeResource()
        {
            if (game.CurrentBiome == null)
            {
                return null;
            }

            List<ItemData> resources = game.Content.items.FindAll(item =>
                item != null && item.preferredBiome == game.CurrentBiome.type &&
                item.kind != ItemKind.Egg);
            return resources.Count > 0 ? resources[Random.Range(0, resources.Count)] : null;
        }

        private CreatureData SelectStarterCreature()
        {
            if (game.CurrentBiome == null)
            {
                return null;
            }

            List<CreatureData> creatures = game.CurrentBiome.availableCreatures.FindAll(creature =>
                creature != null &&
                creature.appearanceChance > 0.001f &&
                creature.rarity != CreatureRarity.Legendary &&
                creature.rarity != CreatureRarity.Secret &&
                game.AreAppearanceConditionsMet(creature));
            return creatures.Count > 0 ? creatures[Random.Range(0, creatures.Count)] : null;
        }
    }
}
