// move player locally.
// NetworkTransform component's syncDirection needs to be set CLIENT_TO_SERVER!
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Example10k
{
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class PlayerMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // get delta time
            float deltaTime = Time.DeltaTime;

            // for the local player
            Entities.ForEach((NetworkEntity networkEntity,
                              PlayerMovementData movement,
                              ref Translation translation) =>
            {
                // is this our player?
                if (!networkEntity.owned)
                    return;

                // move
                float3 direction = new float3(horizontal, 0, vertical);
                translation.Value += math.normalizesafe(direction) * (deltaTime * movement.speed);
            })
            .Run();
        }
    }
}
