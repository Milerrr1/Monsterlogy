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
        public void Initialize(GameManager gameManager)
        {
            // Legacy entry point retained for scenes and integrations created before save version 4.
        }

        public bool TryMutate(string baseCreatureId, string itemId, out string message)
        {
            message = "Система мутаций отключена. Новые формы открываются через эволюцию видов.";
            return false;
        }
    }
}
