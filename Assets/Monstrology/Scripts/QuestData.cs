using UnityEngine;

namespace Monstrology
{
    public enum QuestGoalType
    {
        CompleteExplorations,
        FindAnyCreature,
        UnlockBiome,
        FindCreatureCopies,
        CompleteMutations,
        FindSecretCreature
    }

    [CreateAssetMenu(fileName = "Quest", menuName = "Monstrology/Quest")]
    public class QuestData : ScriptableObject
    {
        public string id;
        public string questName;
        [TextArea(2, 4)] public string description;
        public QuestGoalType goalType;
        [Min(1)] public int requiredAmount = 1;
        public BiomeType requiredBiome;
        [Min(0)] public int rewardCoins = 25;
        [Min(0)] public int rewardEnergy = 2;
    }
}
