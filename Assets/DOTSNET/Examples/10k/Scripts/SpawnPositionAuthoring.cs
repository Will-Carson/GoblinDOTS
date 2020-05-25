// Copies own position to JoinWorldMessageSystem.spawnPosition.
// Spawn position is game dependent.
// DOTSNET doesn't know anything about spawn positions.
using UnityEngine;

namespace DOTSNET.Examples.Example10k
{
    public class SpawnPositionAuthoring : MonoBehaviour
    {
        void Awake()
        {
            Bootstrap.ServerWorld.GetExistingSystem<JoinWorldMessageSystem>().spawnPosition = transform.position;
        }
    }
}
