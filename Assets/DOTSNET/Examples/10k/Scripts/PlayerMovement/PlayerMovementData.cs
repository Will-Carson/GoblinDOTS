using Unity.Entities;

namespace DOTSNET.Examples.Example10k
{
    [GenerateAuthoringComponent]
    public struct PlayerMovementData : IComponentData
    {
        // movement speed in m/s
        public float speed;
    }
}
