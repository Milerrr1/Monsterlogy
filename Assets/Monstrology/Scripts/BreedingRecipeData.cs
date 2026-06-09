using UnityEngine;

namespace Monstrology
{
    [CreateAssetMenu(fileName = "BreedingRecipe", menuName = "Monstrology/Breeding Recipe")]
    public class BreedingRecipeData : ScriptableObject
    {
        public string parentSpeciesA;
        public string parentSpeciesB;
        public string resultSpecies;
        [Range(0f, 1f)] public float baseSuccessChance = 1f;
        [Range(0f, 1f)] public float rareVariantChance = 0.05f;
        public string requiredItemId;
        public string requiredBiomeId;

        public bool Matches(string speciesA, string speciesB)
        {
            return (parentSpeciesA == speciesA && parentSpeciesB == speciesB) ||
                   (parentSpeciesA == speciesB && parentSpeciesB == speciesA);
        }
    }
}
