using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class GameplayHintSystem : MonoBehaviour
    {
        private static readonly List<string> Hints = new List<string>
        {
            "Следы часто ведут к редким существам.",
            "Компас показывает направление только к одной находке.",
            "Любимчик иногда помогает заметить полезные предметы.",
            "Собирай копии одного вида, чтобы открыть следующую форму.",
            "Гардероб меняет внешний вид питомца и может давать бонусы."
        };

        private GameManager game;
        private int actionCount;
        private float startupDelay;
        private int hintIndex;

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
            startupDelay = 8f;
            hintIndex = Mathf.Abs(System.DateTime.UtcNow.DayOfYear) % Hints.Count;
            if (game != null)
            {
                game.ExplorationRegistered += HandleAction;
                game.CreatureRegistered += HandleCreature;
                game.BiomeUnlocked += HandleBiome;
            }
        }

        private void Update()
        {
            if (game == null || startupDelay < 0f)
            {
                return;
            }

            startupDelay -= Time.unscaledDeltaTime;
            if (startupDelay <= 0f)
            {
                startupDelay = -1f;
                ShowNextHint();
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.ExplorationRegistered -= HandleAction;
                game.CreatureRegistered -= HandleCreature;
                game.BiomeUnlocked -= HandleBiome;
            }
        }

        public void ShowNextHint()
        {
            if (game == null || Hints.Count == 0)
            {
                return;
            }

            game.RaiseNotification("Совет: " + Hints[hintIndex % Hints.Count]);
            hintIndex++;
        }

        private void HandleAction()
        {
            actionCount++;
            if (actionCount % 6 == 0)
            {
                ShowNextHint();
            }
        }

        private void HandleCreature(CreatureData creature, bool firstDiscovery)
        {
            if (firstDiscovery)
            {
                actionCount++;
            }
        }

        private void HandleBiome(BiomeData biome)
        {
            ShowNextHint();
        }
    }
}
