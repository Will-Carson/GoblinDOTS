// Modify NetworkTransformSystem settings via Authoring.
using UnityEngine;

namespace DOTSNET
{
    public class NetworkTransformServerSystemAuthoring : MonoBehaviour
    {
        // find NetworkServerSystem in ECS world
        NetworkTransformServerSystem system =>
            Bootstrap.ServerWorld.GetExistingSystem<NetworkTransformServerSystem>();

        // configuration
        public float interval = 0.1f;

        // apply configuration in Awake
        void Awake()
        {
            system.interval = interval;
        }
    }
}