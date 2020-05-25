// helper class to inherit from for message processing.
// * .client access for ease of use
// * [ClientWorld] tag already specified
// * RegisterMessage + Handler already set up
using Unity.Entities;
using UnityEngine;

namespace DOTSNET
{
    [ClientWorld]
    [UpdateInGroup(typeof(ClientConnectedSimulationSystemGroup))]
    public abstract class NetworkClientMessageSystem<T> : SystemBase
        where T : NetworkMessage, new()
    {
        // dependencies
        [AutoAssign] protected NetworkClientSystem client;

        // the handler function
        protected abstract void OnMessage(NetworkMessage message);

        // OnStartRunning registers the message type in the client.
        // -> need to use OnStartRunning, because OnCreate doesn't necessarily
        //    find the server yet.
        protected override void OnStartRunning()
        {
            // register handler
            if (client.RegisterHandler<T>(OnMessage))
            {
                Debug.Log("NetworkClientMessage/System Registered for: " + typeof(T));
            }
            else Debug.LogError("NetworkClientMessageSystem: failed to register handler for: " + typeof(T) + ". Was a handler for that message type already registered?");
        }

        // OnStopRunning unregisters the message
        // Otherwise OnStartRunning can't register it again without an error,
        // and we really do want to have a legitimate error there in case
        // someone accidentally registers two handlers for one message.
        protected override void OnStopRunning()
        {
            client.UnregisterHandler<T>();
        }
    }
}