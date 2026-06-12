using UnityEngine;

namespace Monstrology
{
    public class EncyclopediaAvailabilityCheck : MonoBehaviour
    {
        [ContextMenu("Run Encyclopedia Availability Check")]
        public void RunEncyclopediaAvailabilityCheck()
        {
            GameManager game = FindObjectOfType<GameManager>();
            if (game == null)
            {
                Debug.LogWarning(
                    "Encyclopedia Availability Check requires an initialized GameManager.");
                return;
            }

            int warnings =
                ContentValidator.ValidateEncyclopediaAvailability(game.Content);
            Debug.Log(warnings == 0
                ? "MONSTROLOGY_ENCYCLOPEDIA_AVAILABILITY_PASS"
                : "MONSTROLOGY_ENCYCLOPEDIA_AVAILABILITY_WARNINGS=" + warnings);
        }
    }
}
