using System.Linq;
using UnityEngine;

namespace Monstrology
{
    public class ReleaseReadinessCheck : MonoBehaviour
    {
        [ContextMenu("Run Release Readiness Check")]
        public bool RunReleaseReadinessCheck()
        {
            GameManager game = FindObjectOfType<GameManager>();
            UIManager ui = FindObjectOfType<UIManager>();
            bool ready = game != null &&
                         game.Content.creatures.Count >= 5 &&
                         game.Content.biomes.Count >= 3 &&
                         FindObjectOfType<AchievementSystem>() != null &&
                         FindObjectOfType<EnergyRegenerationSystem>() != null &&
                         FindObjectOfType<DailyRewardSystem>() != null &&
                         FindObjectOfType<AudioManager>() != null &&
                         FindObjectOfType<EncyclopediaAvailabilityCheck>() != null &&
                         ui != null && ui.HasPauseMenu &&
                         game.Content.speciesEvolutions.Any(evolution =>
                             evolution != null &&
                             game.GetCreature(evolution.baseSpeciesId) != null &&
                             game.GetCreature(evolution.resultSpeciesId) != null &&
                             evolution.requiredCopies > 0) &&
                         WorldExplorationManager.ReleaseDropChancesAreValid &&
                         ContentValidator.ValidateEncyclopediaAvailability(
                             game.Content) == 0;

            if (game != null)
            {
                ContentValidator.Validate(game.Content);
            }

            if (ready)
            {
                Debug.Log("MONSTROLOGY_RELEASE_READINESS_PASS");
            }
            else
            {
                Debug.LogWarning("MONSTROLOGY_RELEASE_READINESS_FAIL");
            }

            return ready;
        }
    }
}
