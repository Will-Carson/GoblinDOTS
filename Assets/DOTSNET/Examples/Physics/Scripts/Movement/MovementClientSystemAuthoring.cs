using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace DOTSNET.Examples.Physics
{
    public class MovementClientSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        public Type GetSystemType() => typeof(MovementClientSystem);
    }

    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    [DisableAutoCreation]
    public class MovementClientSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float3 direction = math.normalizesafe(new float3(h, 0, v));

            Entities.ForEach((NetworkEntity networkEntity,
                              MovementComponent movement,
                              PhysicsMass mass,
                              ref Translation translation,
                              ref PhysicsVelocity velocity) =>
            {
                // only for our own player
                if (!networkEntity.owned)
                    return;

                // dynamic body + impulse works
                velocity.ApplyLinearImpulse(mass, direction * movement.force);

                // force y=0 even when players collider or spawn inside each
                // other.
                translation.Value.y = 0;
            })
            .Run();
        }
    }
}
