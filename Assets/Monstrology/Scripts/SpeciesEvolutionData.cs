using UnityEngine;

namespace Monstrology
{
    [CreateAssetMenu(fileName = "SpeciesEvolution", menuName = "Monstrology/Species Evolution")]
    public class SpeciesEvolutionData : ScriptableObject
    {
        public string baseSpeciesId;
        [Min(1)] public int requiredCopies = 100;
        public string resultSpeciesId;
    }
}
