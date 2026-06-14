using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class FavoriteHelperSystem : MonoBehaviour
    {
        [SerializeField, Min(20f)] private float helpIntervalSeconds = 75f;

        private GameManager game;
        private CreatureCollectionManager collection;
        private CreatureNestSystem nests;
        private WorldExplorationManager world;
        private float helpTimer;

        public float RareCreatureChanceBonus
        {
            get
            {
                CreatureInstance favorite = collection != null ? collection.GetFavorite() : null;
                return favorite == null ? 0f : Mathf.Min(0.1f, favorite.level * 0.002f);
            }
        }

        public float ResourceChanceBonus
        {
            get
            {
                CreatureInstance favorite = collection != null ? collection.GetFavorite() : null;
                return favorite == null ? 0f : Mathf.Min(0.12f, favorite.level * 0.0025f);
            }
        }

        public void Initialize(
            GameManager gameManager,
            CreatureCollectionManager collectionManager,
            CreatureNestSystem nestSystem,
            WorldExplorationManager worldManager)
        {
            game = gameManager;
            collection = collectionManager;
            nests = nestSystem;
            world = worldManager;
            helpTimer = helpIntervalSeconds;
        }

        private void Update()
        {
            if (game == null || collection == null || collection.GetFavorite() == null)
            {
                return;
            }

            helpTimer -= Time.deltaTime;
            if (helpTimer > 0f)
            {
                return;
            }

            helpTimer = helpIntervalSeconds;
            CreatureInstance favorite = collection.GetFavorite();
            float chance = Mathf.Clamp(0.15f + favorite.level * 0.01f, 0.15f, 0.6f);
            if (Random.value > chance)
            {
                return;
            }

            int action = Random.Range(0, 4);
            if (action == 0)
            {
                game.AddTrack(TrackType.Paws);
                game.RaiseNotification(favorite.GetDisplayName(game.GetCreature(favorite.speciesId)) +
                                       " заметил свежий след.");
            }
            else if (action == 1)
            {
                ItemData resource = SelectBiomeResource();
                if (resource != null)
                {
                    game.AddItem(resource.id);
                    game.RaiseNotification(favorite.GetDisplayName(game.GetCreature(favorite.speciesId)) +
                                           " нашёл: " +
                                           resource.GetVisibleName(game) +
                                           ".");
                }
            }
            else if (action == 2 && world != null)
            {
                world.SpawnFavoriteHint(true);
            }
            else if (nests != null && game.CurrentBiome != null &&
                     nests.GetReadyCount(game.CurrentBiome.type) > 0)
            {
                game.RaiseNotification("Любимчик напоминает: в логовищах готова награда.");
            }
            else if (world != null)
            {
                world.SpawnFavoriteHint(false);
            }
        }

        private ItemData SelectBiomeResource()
        {
            if (game.CurrentBiome == null)
            {
                return null;
            }

            List<ItemData> items = game.Content.items.FindAll(item =>
                item != null && item.preferredBiome == game.CurrentBiome.type &&
                item.kind != ItemKind.Egg);
            return items.Count > 0 ? items[Random.Range(0, items.Count)] : null;
        }
    }
}
