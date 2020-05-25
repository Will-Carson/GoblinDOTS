namespace DOTSNET
{
    public class UnspawnMessageSystem : NetworkClientMessageSystem<UnspawnMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(NetworkMessage message)
        {
            UnspawnMessage spawnMessage = (UnspawnMessage)message;
            client.Unspawn(spawnMessage.netId);
        }
    }
}
