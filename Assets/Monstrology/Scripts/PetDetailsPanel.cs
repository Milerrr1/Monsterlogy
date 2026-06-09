using System.Collections.Generic;

namespace Monstrology
{
    public static class PetDetailsPanel
    {
        public static string BuildGeneticsText(CreatureInstance pet)
        {
            CreatureGenetics genetics = pet != null && pet.genetics != null
                ? pet.genetics
                : new CreatureGenetics();
            return "Гены: Размер " + Percent(genetics.sizeGene) +
                   "  |  Энергия " + Percent(genetics.energyGene) +
                   "  |  Удача " + Percent(genetics.luckGene);
        }

        public static string BuildAccessoryText(
            CreatureInstance pet,
            AccessoryInventoryManager inventory)
        {
            List<AccessoryData> equipped = pet != null && inventory != null
                ? inventory.GetEquippedAccessories(pet.uniqueId)
                : new List<AccessoryData>();
            if (equipped.Count == 0)
            {
                return "Гардероб: одежда не надета";
            }

            List<string> names = new List<string>();
            foreach (AccessoryData accessory in equipped)
            {
                names.Add(accessory.slot + ": " + accessory.displayName);
            }

            return "Гардероб: " + string.Join("  |  ", names);
        }

        private static string Percent(float value)
        {
            return UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(value) * 100f) + "%";
        }
    }
}
