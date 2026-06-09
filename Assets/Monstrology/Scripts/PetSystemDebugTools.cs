using UnityEngine;

namespace Monstrology
{
    public class PetSystemDebugTools : MonoBehaviour
    {
        private GameManager game;
        private CreatureCollectionManager collection;
        private AccessoryInventoryManager accessories;
        private BreedingSystem breeding;

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            AccessoryInventoryManager accessoryManager,
            BreedingSystem breedingSystem)
        {
            game = gameManager;
            collection = collectionManager;
            accessories = accessoryManager;
            breeding = breedingSystem;
        }

        [ContextMenu("Add Test Pet")]
        public void AddTestPet()
        {
            if (game == null || collection == null || game.Content.creatures.Count == 0)
            {
                return;
            }

            CreatureInstance pet = collection.AddPet(game.Content.creatures[0].id, "Test Pet");
            if (pet != null)
            {
                pet.level = 3;
                collection.SaveNow();
            }
        }

        [ContextMenu("Add Test Accessory")]
        public void AddTestAccessory()
        {
            if (game != null && accessories != null && game.Content.accessories.Count > 0)
            {
                accessories.AddAccessory(game.Content.accessories[0]);
            }
        }

        [ContextMenu("Clear Pets")]
        public void ClearPets()
        {
            if (collection != null)
            {
                collection.ClearPets();
            }
        }

        [ContextMenu("Force Evolution Test")]
        public void ForceEvolutionTest()
        {
            if (collection == null || breeding == null || game == null ||
                game.Content.speciesEvolutions.Count == 0)
            {
                return;
            }

            SpeciesEvolutionData evolution = game.Content.speciesEvolutions[0];
            if (collection.GetAllPets().Count == 0)
            {
                collection.AddPet(evolution.baseSpeciesId, "Evolution Test");
            }

            int missing = Mathf.Max(0,
                evolution.requiredCopies - game.GetCreatureCount(evolution.baseSpeciesId));
            game.AddCreatureCopies(evolution.baseSpeciesId, missing);
            breeding.EvolveSpecies(evolution.baseSpeciesId);
        }

        [ContextMenu("Set First Pet As Favorite")]
        public void SetFirstPetAsFavorite()
        {
            if (collection != null && collection.Count > 0)
            {
                collection.SetFavorite(collection.GetAllPets()[0].uniqueId);
            }
        }
    }
}
