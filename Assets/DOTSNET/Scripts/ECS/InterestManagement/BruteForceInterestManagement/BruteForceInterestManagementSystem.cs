// DOTS is fast. this is a simple brute force system for now.
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTSNET
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class BruteForceInterestManagementSystem : InterestManagementSystem
    {
        // visibility radius
        // modified by Authoring component!
        public float visibilityRadius = float.MaxValue;

        // don't update every tick. update every so often.
        // note: can't used FixedRateUtils because they only work for groups
        public float updateInterval = 1;
        double lastUpdateTime;

        // cache rebuild HashSet so we don't have to recreate it for each entity
        // each time
        HashSet<int> rebuildCache = new HashSet<int>();

        // helper function to check if an Entity is seen by ANY of the
        // connection's owned objects
        internal bool IsVisibleToAny(Translation translation, HashSet<Entity> ownedEntities)
        {
            foreach (Entity owned in ownedEntities)
            {
                // get owned translation
                Translation ownedTranslation = GetComponent<Translation>(owned);

                // check the distance between the two
                float distance = math.distance(translation.Value, ownedTranslation.Value);
                if (distance <= visibilityRadius)
                {
                    // return immediately. no need to check all others too.
                    // we only ever add ONE connectionId to observers.
                    // we don't add player's and player pet's connectionIds,
                    // otherwise we would broadcast to the connection twice.
                    return true;
                }
            }
            return false;
        }

        // helper function to rebuild observers
        // -> HashSet is passed so we don't have to reallocate it each time!
        internal void RebuildFor(Translation translation, HashSet<int> result)
        {
            result.Clear();

            // for each connection
            foreach (KeyValuePair<int, ConnectionState> kvp in server.connections)
            {
                // is it visible to ANY of the connection's owned entities?
                if (IsVisibleToAny(translation, kvp.Value.ownedEntities))
                {
                    //UnityEngine.Debug.LogWarning(EntityManager.GetName(entity) + " is visible to connectionId=" + kvp.Key + " owned objects.");
                    result.Add(kvp.Key);
                }
            }
        }

        // helper function to remove old observers that aren't in a new rebuild
        void RemoveOldObservers(Entity entity,
                                DynamicBuffer<NetworkObserver> observers,
                                NetworkEntity networkEntity,
                                HashSet<int> rebuild)
        {
            // check which of the previous observers can be removed now
            // DynamicBuffer foreach allocates. use for.
            for (int i = 0; i < observers.Length; ++i)
            {
                int connectionId = observers[i];
                if (!rebuild.Contains(connectionId))
                {
                    //Debug.LogWarning(EntityManager.GetName(entity) + " old observer found with connectionId=" + connectionId);

                    // remove it from the observers buffer
                    observers.RemoveAt(i);
                    --i;

                    // broadcast system needs to send an unspawn message
                    // see AddNewObservers() comment for the 3 different cases.
                    // it's the same here.
                    SendUnspawnMessage(
                        networkEntity,
                        connectionId
                    );
                }
            }
        }

        // helper function to add new rebuild entries to observers
        void AddNewObservers(Entity entity,
                             DynamicBuffer<NetworkObserver> observers,
                             Translation translation,
                             Rotation rotation,
                             NetworkEntity networkEntity,
                             HashSet<int> rebuild)
        {
            // check which of the rebuild observers are new
            foreach (int connectionId in rebuild)
            {
                if (!observers.Contains(connectionId))
                {
                    //Debug.LogWarning(EntityManager.GetName(entity) + " new observer found with connectionId=" + connectionId);

                    // add it to the observers buffer
                    observers.Add(connectionId);

                    // broadcast system needs to send a spawn message.
                    //
                    // there are three possible cases:
                    //   if we have a monster and a player walks near it:
                    //   -> we add player connectionId to monster observers
                    //   -> we need to send SpawnMessage(Monster) to player
                    //
                    //   if we have playerA and playerB and they walk near:
                    //   -> we add playerB connectionId to playerA observers
                    //      when rebuilding playerA
                    //   -> we need to send SpawnMessage(PlayerB) to playerA
                    //      we do NOT need to send SpawnMessage(PlayerA) to
                    //      playerB because rebuilding playerB will take care of
                    //      it later.
                    //
                    //   if a player first spawns into an empty world:
                    //   -> we will add his own connectionId to his observers
                    //   -> and then send SpawnMessage(Player) to his connection
                    //
                    // => all three cases require the same call:
                    SendSpawnMessage(
                        entity,
                        translation,
                        rotation,
                        networkEntity,
                        connectionId
                    );
                }
            }
        }

        public override void RebuildAll()
        {
            // for each NetworkEntity, we need to check if it's visible from
            // ANY of the player's entities. not just the main player.
            //
            // consider a MOBA game where a player might place a watchtower at
            // the other end of the map:
            // * if we check visibility only to the main player, then the watch-
            //   tower would not see anything
            // * if we check visibility to all player objects, both the watch-
            //   tower and the main player object would see enemies

            // for each NetworkEntity
            Entities.ForEach((Entity entity,
                              DynamicBuffer<NetworkObserver> observers,
                              Translation translation,
                              Rotation rotation,
                              NetworkEntity networkEntity) =>
            {
                // rebuild observers for this entity once
                RebuildFor(translation, rebuildCache);

                // remove old observers, add new observers
                RemoveOldObservers(entity, observers, networkEntity, rebuildCache);
                AddNewObservers(entity, observers, translation, rotation, networkEntity, rebuildCache);
            })
            .WithoutBurst()
            .Run();
        }

        // update rebuilds every couple of seconds
        protected override void OnUpdate()
        {
            if (Time.ElapsedTime >= lastUpdateTime + updateInterval)
            {
                RebuildAll();
                lastUpdateTime = Time.ElapsedTime;
            }
        }
    }
}
