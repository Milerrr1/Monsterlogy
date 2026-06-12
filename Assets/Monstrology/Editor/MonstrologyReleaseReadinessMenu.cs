#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Monstrology.Editor
{
    public static class MonstrologyReleaseReadinessMenu
    {
        [MenuItem("Tools/Monstrology/Run Release Readiness Check")]
        public static void Run()
        {
            ReleaseReadinessCheck check = Object.FindObjectOfType<ReleaseReadinessCheck>();
            if (check == null)
            {
                Debug.LogWarning(
                    "Release Readiness Check requires Play Mode so runtime systems can be inspected.");
                return;
            }

            check.RunReleaseReadinessCheck();
        }

        [MenuItem("Tools/Monstrology/Run Encyclopedia Availability Check")]
        public static void RunEncyclopediaAvailabilityCheck()
        {
            EncyclopediaAvailabilityCheck check =
                Object.FindObjectOfType<EncyclopediaAvailabilityCheck>();
            if (check == null)
            {
                Debug.LogWarning(
                    "Encyclopedia Availability Check requires Play Mode.");
                return;
            }

            check.RunEncyclopediaAvailabilityCheck();
        }

        [MenuItem("Tools/Monstrology/Run Beta Readiness Check")]
        public static void RunBetaReadinessCheck()
        {
            BetaReadinessCheck check =
                Object.FindObjectOfType<BetaReadinessCheck>();
            if (check == null)
            {
                Debug.LogWarning("Beta Readiness Check requires Play Mode.");
                return;
            }

            check.RunBetaReadinessCheck();
        }

        [MenuItem("Tools/Monstrology/Validate Creature Template")]
        public static void ValidateCreatureTemplate()
        {
            CreatureVisualValidator validator =
                Object.FindObjectOfType<CreatureVisualValidator>();
            if (validator == null)
            {
                Debug.LogWarning(
                    "Creature Template validation requires Play Mode.");
                return;
            }

            validator.ValidateCreatureTemplate();
        }
    }
}
#endif
