using UnityEngine;

namespace Monstrology
{
    public class QuestSystem : MonoBehaviour
    {
        private GameManager game;

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
        }

        public bool IsCompleted(QuestData quest)
        {
            if (game == null || quest == null)
            {
                return false;
            }

            switch (quest.goalType)
            {
                case QuestGoalType.CompleteExplorations:
                    return game.ExplorationCount >= quest.requiredAmount;
                case QuestGoalType.FindAnyCreature:
                    return game.TotalCreaturesFound >= quest.requiredAmount;
                case QuestGoalType.UnlockBiome:
                    return game.IsBiomeUnlocked(quest.requiredBiome);
                case QuestGoalType.FindCreatureCopies:
                    return game.TotalCreaturesFound >= quest.requiredAmount;
                case QuestGoalType.CompleteMutations:
                    return game.MutationCount >= quest.requiredAmount;
                case QuestGoalType.FindSecretCreature:
                    return game.Content.creatures.Exists(creature =>
                        creature.rarity == CreatureRarity.Secret && game.IsCreatureFound(creature.id));
                default:
                    return false;
            }
        }

        public int GetProgress(QuestData quest)
        {
            if (game == null || quest == null)
            {
                return 0;
            }

            switch (quest.goalType)
            {
                case QuestGoalType.CompleteExplorations:
                    return game.ExplorationCount;
                case QuestGoalType.FindAnyCreature:
                case QuestGoalType.FindCreatureCopies:
                    return game.TotalCreaturesFound;
                case QuestGoalType.CompleteMutations:
                    return game.MutationCount;
                case QuestGoalType.UnlockBiome:
                case QuestGoalType.FindSecretCreature:
                    return IsCompleted(quest) ? quest.requiredAmount : 0;
                default:
                    return 0;
            }
        }

        public bool TryClaim(QuestData quest)
        {
            if (!IsCompleted(quest) || game.IsQuestClaimed(quest.id))
            {
                return false;
            }

            game.ClaimQuest(quest);
            return true;
        }
    }
}
