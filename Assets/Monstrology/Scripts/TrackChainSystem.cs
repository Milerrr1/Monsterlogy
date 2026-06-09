using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public class TrackChainSystem : MonoBehaviour
    {
        private static readonly TrackType[] Chain =
        {
            TrackType.Paws,
            TrackType.Paws,
            TrackType.Fur,
            TrackType.StrangeSound,
            TrackType.Lair
        };

        private WorldExplorationManager world;

        public void Initialize(WorldExplorationManager worldManager)
        {
            world = worldManager;
        }

        public void Advance(WorldPickup trace)
        {
            if (world == null || trace == null)
            {
                return;
            }

            int nextStage = Mathf.Max(0, trace.TrackChainStage) + 1;
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.1f)
            {
                direction = Vector2.right;
            }

            Vector3 nextPosition = trace.transform.position +
                                   (Vector3)(direction * Random.Range(4.5f, 7.5f));
            if (nextStage < Chain.Length)
            {
                world.SpawnTrackStep(nextPosition, Chain[nextStage], nextStage);
            }
            else
            {
                world.SpawnTrackedCreature(nextPosition);
            }
        }
    }
}
