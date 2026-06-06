using System;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class MutationRecipe
    {
        public string baseCreatureId;
        public string itemId;
        public string resultCreatureId;
    }

    public class MutationSystem : MonoBehaviour
    {
        private GameManager game;

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
        }

        public bool TryMutate(string baseCreatureId, string itemId, out string message)
        {
            if (game == null)
            {
                message = "Лаборатория ещё не готова.";
                return false;
            }

            if (!game.IsCreatureFound(baseCreatureId))
            {
                message = "Сначала найдите базовое существо.";
                return false;
            }

            if (game.GetItemCount(itemId) <= 0)
            {
                message = "Такого предмета нет в инвентаре.";
                return false;
            }

            MutationRecipe recipe = game.Content.mutationRecipes.Find(candidate =>
                candidate.baseCreatureId == baseCreatureId && candidate.itemId == itemId);

            if (recipe == null)
            {
                message = "Комбинация не сработала. Предмет сохранён.";
                return false;
            }

            CreatureData result = game.GetCreature(recipe.resultCreatureId);
            if (result == null)
            {
                message = "Результат мутации не настроен.";
                return false;
            }

            game.ConsumeItem(itemId);
            bool firstDiscovery = game.AddCreature(result);
            game.RegisterMutation();
            message = firstDiscovery
                ? "Успех! Открыта новая форма: " + result.creatureName
                : "Мутация повторена: " + result.creatureName;
            return true;
        }
    }
}
