// Interest Management is needed to broadcast an entity's updates only to the
// surrounding players in order to save bandwidth.
//
// This can be done in a lot of different ways:
// - brute force distance checking everyone to everyone else
// - physics sphere casts to find everyone in a radius
// - spatial hashing aka grid checking
// - etc.
//
// So we need a base class for all of them.
using Unity.Entities;
using Unity.Transforms;

namespace DOTSNET
{
    // ComponentSystem for now. Jobs come later.
    [ServerWorld]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    // IMPORTANT: use [UpdateBefore(typeof(BroadcastSystem))] when inheriting
    // IMPORTANT: use [DisableAutoCreation] + SelectiveSystemAuthoring when
    //            inheriting
    public abstract class InterestManagementSystem : SystemBase
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;

        // rebuild all areas of interest for everyone once
        //
        // note:
        //   we DO NOT do any custom rebuilding after someone joined/spawned or
        //   disconnected/unspawned.
        //   this would require INSANE complexity.
        //   for example, OnTransportDisconnect would have to:
        //     1. remove the connection so the connectionId is invalid
        //     2. then call BroadcastAfterUnspawn(oldEntity) which broadcasts
        //        destroyed messages BEFORE rebuilding so we know the old
        //        observers that need to get the destroyed message
        //     3. RebuildAfterUnspawn to remove it
        //     4. then remove the Entity from connection's owned objects, which
        //        IS NOT POSSIBLE anymore because the connection was already
        //        removed. which means that the next rebuild would still see it
        //        etc.
        //        (it's just insanity)
        //   additionally, we would also need extra flags in Spawn to NOT
        //   rebuild when spawning 10k scene objects in start, etc.s
        //
        //   DOTS is fast, so it makes no sense to have that insane complexity.
        //
        // first principles:
        //   it wouldn't even make sense to have special cases because players
        //   might walk in and out of range from each other all the time anyway.
        //   we already need to handle that case. (dis)connect is no different.
        public abstract void RebuildAll();

        // send spawn message when a new observer was added
        public void SendSpawnMessage(Entity entity,
                                     Translation translation,
                                     Rotation rotation,
                                     NetworkEntity networkEntity,
                                     int observerConnectionId)
        {
            // is the entity owned by the observer connection?
            bool owned = networkEntity.connectionId == observerConnectionId;

            // create the spawn message
            SpawnMessage message = new SpawnMessage(
                networkEntity.prefabId,
                networkEntity.netId,
                owned,
                translation.Value,
                rotation.Value
            );

            // send it
            //Debug.LogWarning("Spawning " + EntityManager.GetName(entity) + " with netId=" + networkEntity.netId + " on client with connectionId=" + observerConnectionId);
            server.Send(message, observerConnectionId);
        }

        // send unspawn message when an observer was removed
        public void SendUnspawnMessage(NetworkEntity networkEntity, int observerConnectionId)
        {
            // only if the connection still exists.
            // we don't need to send an unspawn message to this connection if
            // the observer was removed because the connection disconnected.
            if (server.connections.ContainsKey(observerConnectionId))
            {
                // create the unspawn message
                UnspawnMessage message = new UnspawnMessage(networkEntity.netId);

                // send it
                server.Send(message, observerConnectionId);
            }
        }
    }
}
