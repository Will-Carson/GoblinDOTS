namespace DOTSNET
{
    public class SpawnMessageSystem : NetworkClientMessageSystem<SpawnMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(NetworkMessage message)
        {
            SpawnMessage spawnMessage = (SpawnMessage)message;
            client.Spawn(spawnMessage.prefabId,
                         spawnMessage.netId,
                         spawnMessage.owned,
                         spawnMessage.position,
                         spawnMessage.rotation);
        }
    }
}
