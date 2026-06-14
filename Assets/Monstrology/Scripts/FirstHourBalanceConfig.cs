using System;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class FirstHourPhaseWeights
    {
        [Min(0f)] public float creature = 1f;
        [Min(0f)] public float trace = 1f;
        [Min(0f)] public float resource = 1f;
        [Min(0f)] public float egg = 1f;
        [Min(0f)] public float accessory = 1f;
        [Min(0f)] public float empty = 1f;
        [Min(0f)] public float rareCreature = 1f;
        [Min(0f)] public float newContent = 1f;
    }

    [CreateAssetMenu(
        fileName = "FirstHourBalanceConfig",
        menuName = "Monstrology/First Hour Balance")]
    public class FirstHourBalanceConfig : ScriptableObject
    {
        [Header("Base finding weights")]
        [Min(0f)] public float creatureWeight = 0.18f;
        [Min(0f)] public float traceWeight = 0.16f;
        [Min(0f)] public float resourceWeight = 0.31f;
        [Min(0f)] public float eggWeight = 0.05f;
        [Min(0f)] public float accessoryWeight = 0.10f;
        [Min(0f)] public float emptyWeight = 0.06f;
        [Min(0f)] public float nestAndEventWeight = 0.04f;

        [Header("Phase boundaries, minutes")]
        [Min(1f)] public float phaseOneEndMinutes = 10f;
        [Min(1f)] public float phaseTwoEndMinutes = 25f;
        [Min(1f)] public float phaseThreeEndMinutes = 40f;
        [Min(1f)] public float firstHourEndMinutes = 60f;

        [Header("Phase multipliers")]
        public FirstHourPhaseWeights phaseZeroToTen =
            new FirstHourPhaseWeights
            {
                creature = 1.55f,
                trace = 1.35f,
                resource = 1.15f,
                egg = 0.6f,
                accessory = 0.35f,
                empty = 0f,
                rareCreature = 0f,
                newContent = 1.8f
            };
        public FirstHourPhaseWeights phaseTenToTwentyFive =
            new FirstHourPhaseWeights
            {
                creature = 1.2f,
                trace = 1f,
                resource = 1.2f,
                egg = 0.8f,
                accessory = 2f,
                empty = 0.7f,
                rareCreature = 0.55f,
                newContent = 1.5f
            };
        public FirstHourPhaseWeights phaseTwentyFiveToForty =
            new FirstHourPhaseWeights
            {
                creature = 1.35f,
                trace = 1.25f,
                resource = 1.25f,
                egg = 1f,
                accessory = 1.35f,
                empty = 0.9f,
                rareCreature = 0.85f,
                newContent = 1.25f
            };
        public FirstHourPhaseWeights phaseFortyToSixty =
            new FirstHourPhaseWeights
            {
                creature = 1.1f,
                trace = 1.15f,
                resource = 1.15f,
                egg = 1f,
                accessory = 1.1f,
                empty = 1f,
                rareCreature = 1f,
                newContent = 1.1f
            };
        public FirstHourPhaseWeights longTerm =
            new FirstHourPhaseWeights();

        [Header("Pity and repetition")]
        [Min(0)] public int protectedOpeningFindings = 5;
        [Min(0)] public int maxEmptyStreak = 2;
        [Min(1)] public int maxSameResourceStreak = 3;
        [Min(0f)] public float newContentPityMinutes = 6f;
        [Min(0f)] public float wardrobePityMinutes = 10f;
        [Min(1)] public int firstHourWardrobeSoftCap = 2;
        [Range(0f, 1f)]
        public float wardrobeAfterSoftCapMultiplier = 0.03f;
        [Min(0f)] public float rareLockMinutes = 8f;
        [Min(0f)] public float rareHintPityMinutes = 18f;
        [Min(1f)] public float pityMultiplier = 2.25f;
        [Min(1f)] public float nearEvolutionCreatureMultiplier = 2.1f;
        [Range(1, 9)] public int nearEvolutionMinimumCopies = 4;
        [Min(0f)] public float pickupCooldownSeconds = 0.25f;

        public FirstHourPhaseWeights GetPhase(float activeSeconds)
        {
            float minutes = Mathf.Max(0f, activeSeconds) / 60f;
            if (minutes < phaseOneEndMinutes)
            {
                return phaseZeroToTen;
            }

            if (minutes < phaseTwoEndMinutes)
            {
                return phaseTenToTwentyFive;
            }

            if (minutes < phaseThreeEndMinutes)
            {
                return phaseTwentyFiveToForty;
            }

            if (minutes < firstHourEndMinutes)
            {
                return phaseFortyToSixty;
            }

            return longTerm;
        }

        public static FirstHourBalanceConfig LoadOrCreate()
        {
            FirstHourBalanceConfig config =
                Resources.Load<FirstHourBalanceConfig>(
                    "FirstHourBalanceConfig");
            if (config != null)
            {
                return config;
            }

            config = CreateInstance<FirstHourBalanceConfig>();
            config.name = "FirstHourBalanceConfig_Runtime";
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
