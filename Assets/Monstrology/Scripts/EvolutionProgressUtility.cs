using System.Collections.Generic;

namespace Monstrology
{
    public static class EvolutionProgressUtility
    {
        private static readonly int[] CumulativeThresholds =
        {
            10,
            25,
            60,
            120
        };

        public static int GetThresholdForTransition(int transitionIndex)
        {
            int index = UnityEngine.Mathf.Clamp(
                transitionIndex,
                0,
                CumulativeThresholds.Length - 1);
            return CumulativeThresholds[index];
        }

        public static string GetRootSpeciesId(
            GameContent content,
            string speciesId)
        {
            if (content == null ||
                content.speciesEvolutions == null ||
                string.IsNullOrEmpty(speciesId))
            {
                return speciesId;
            }

            string current = speciesId;
            HashSet<string> visited = new HashSet<string>();
            while (visited.Add(current))
            {
                SpeciesEvolutionData previous =
                    content.speciesEvolutions.Find(evolution =>
                        evolution != null &&
                        evolution.resultSpeciesId == current);
                if (previous == null ||
                    string.IsNullOrEmpty(previous.baseSpeciesId))
                {
                    break;
                }

                current = previous.baseSpeciesId;
            }

            return current;
        }

        public static int GetSpeciesStage(
            GameContent content,
            string speciesId)
        {
            if (content == null || string.IsNullOrEmpty(speciesId))
            {
                return 0;
            }

            int stage = 0;
            string current = speciesId;
            HashSet<string> visited = new HashSet<string>();
            while (visited.Add(current))
            {
                SpeciesEvolutionData previous =
                    content.speciesEvolutions.Find(evolution =>
                        evolution != null &&
                        evolution.resultSpeciesId == current);
                if (previous == null)
                {
                    break;
                }

                stage++;
                current = previous.baseSpeciesId;
            }

            return stage;
        }

        public static void ApplyCumulativeThresholds(GameContent content)
        {
            if (content == null || content.speciesEvolutions == null)
            {
                return;
            }

            foreach (SpeciesEvolutionData evolution in content.speciesEvolutions)
            {
                if (evolution == null)
                {
                    continue;
                }

                int transitionIndex = GetSpeciesStage(
                    content,
                    evolution.baseSpeciesId);
                evolution.requiredCopies =
                    GetThresholdForTransition(transitionIndex);
            }
        }
    }
}
