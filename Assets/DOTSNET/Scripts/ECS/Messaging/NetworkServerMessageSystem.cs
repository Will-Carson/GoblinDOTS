// helper class to inherit from for message processing.
// * .server access for ease of use
// * [ServerWorld] tag already specified
// * RegisterMessage + Handler already set up
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    [ServerWorld]
    [UpdateInGroup(typeof(ServerActiveSimulationSystemGroup))]
    public abstract class NetworkServerMessageSystem<T> : SystemBase
        where T : NetworkMessage, new()
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;

        // overwrite to indicate if the message should require authentication
        protected abstract bool RequiresAuthentication();

        // wrapper function to convert NetworkMessage back to type T
        void OnMessageInternal(int connectionId, NetworkMessage message)
        {
            OnMessage(connectionId, (T)message);
        }

        // the handler function
        protected abstract void OnMessage(int connectionId, T message);

        // OnStartRunning registers the message type in the server.
        // -> need to use OnStartRunning, because OnCreate doesn't necessarily
        //    find the server yet.
        protected override void OnStartRunning()
        {
            // register handler
            if (server.RegisterHandler<T>(OnMessageInternal, RequiresAuthentication()))
            {
                Debug.Log("NetworkServerMessage/System Registered for: " + typeof(T));
            }
            else Debug.LogError("NetworkServerMessageSystem: failed to register handler for: " + typeof(T) + ". Was a handler for that message type already registered?");
        }

        // OnStopRunning unregisters the message
        // Otherwise OnStartRunning can't register it again without an error,
        // and we really do want to have a legitimate error there in case
        // someone accidentally registers two handlers for one message.
        protected override void OnStopRunning()
        {
            server.UnregisterHandler<T>();
        }
    }
}