// Modify NetworkTransformSystem settings via Authoring.
using UnityEngine;

namespace DOTSNET
{
    public class NetworkTransformClientSystemAuthoring : MonoBehaviour
    {
        // find NetworkServerSystem in ECS world
        NetworkTransformClientSystem system =>
            Bootstrap.ClientWorld.GetExistingSystem<NetworkTransformClientSystem>();

        // configuration
        public float interval = 0.1f;

        // apply configuration in Awake
        void Awake()
        {
            system.interval = interval;
        }
    }
}