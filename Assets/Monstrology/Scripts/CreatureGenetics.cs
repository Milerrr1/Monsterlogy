using System;
using UnityEngine;

namespace Monstrology
{
    [Serializable]
    public class CreatureGenetics
    {
        [Range(0f, 1f)] public float sizeGene = 0.5f;
        [Range(0f, 1f)] public float colorGene = 0.5f;
        [Range(0f, 1f)] public float energyGene = 0.5f;
        [Range(0f, 1f)] public float luckGene = 0.5f;
        [Range(0f, 1f)] public float mutationGene = 0.5f;

        public void Normalize()
        {
            sizeGene = Mathf.Clamp01(sizeGene);
            colorGene = Mathf.Clamp01(colorGene);
            energyGene = Mathf.Clamp01(energyGene);
            luckGene = Mathf.Clamp01(luckGene);
            mutationGene = Mathf.Clamp01(mutationGene);
        }

        public static CreatureGenetics CreateWild(PetRarity rarity)
        {
            float rarityBonus = (int)rarity * 0.025f;
            CreatureGenetics result = new CreatureGenetics
            {
                sizeGene = RollWildGene(rarityBonus),
                colorGene = RollWildGene(rarityBonus),
                energyGene = RollWildGene(rarityBonus),
                luckGene = RollWildGene(rarityBonus),
                mutationGene = RollWildGene(rarityBonus)
            };
            result.Normalize();
            return result;
        }

        public static CreatureGenetics Inherit(CreatureGenetics parentA, CreatureGenetics parentB)
        {
            parentA = parentA ?? new CreatureGenetics();
            parentB = parentB ?? new CreatureGenetics();
            CreatureGenetics result = new CreatureGenetics
            {
                sizeGene = InheritGene(parentA.sizeGene, parentB.sizeGene),
                colorGene = InheritGene(parentA.colorGene, parentB.colorGene),
                energyGene = InheritGene(parentA.energyGene, parentB.energyGene),
                luckGene = InheritGene(parentA.luckGene, parentB.luckGene),
                mutationGene = InheritGene(parentA.mutationGene, parentB.mutationGene)
            };
            result.Normalize();
            return result;
        }

        private static float RollWildGene(float rarityBonus)
        {
            return Mathf.Clamp01(UnityEngine.Random.Range(0.2f, 0.8f) + rarityBonus);
        }

        private static float InheritGene(float first, float second)
        {
            return Mathf.Clamp01((first + second) * 0.5f + UnityEngine.Random.Range(-0.08f, 0.08f));
        }
    }
}
