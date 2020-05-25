using System;
using UnityEngine;

namespace DOTSNET
{
    public class BruteForceInterestManagementSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // find BruteForceInterestManagementSystem in ECS world
        BruteForceInterestManagementSystem bruteForceSystem =>
            Bootstrap.ServerWorld.GetExistingSystem<BruteForceInterestManagementSystem>();

        public float visibilityRadius = 15;
        public float updateInterval = 1;

        // add system if Authoring is used
        public Type GetSystemType() => typeof(BruteForceInterestManagementSystem);

        // apply configuration in Awake once.
        void Awake() { Update(); }

        // apply configuration in Update too. it's not recommended in a real
        // project because it would overwrite settings that other ECS world
        // systems might apply. but for our demo, it's just so satisfying to
        // increase the visibility radius at runtime.
        void Update()
        {
            bruteForceSystem.visibilityRadius = visibilityRadius;
            bruteForceSystem.updateInterval = updateInterval;
        }
    }
}