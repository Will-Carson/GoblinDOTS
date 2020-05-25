using System;
using Unity.Entities;
using UnityEngine;

namespace DOTSNET.Examples.Chat
{
    public class ChatMessageClientSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
    {
        // add system if Authoring is used
        public Type GetSystemType() => typeof(ChatMessageClientSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ChatMessageClientSystem : NetworkClientMessageSystem<ChatMessage>
    {
        protected override void OnUpdate() {}
        protected override void OnMessage(NetworkMessage message)
        {
            // convert to the actual message type
            ChatMessage msg = (ChatMessage)message;
            Debug.Log("Client message: " + msg.sender + ": " + msg.text);

            // add message
            ChatClientSystem chatClient = (ChatClientSystem)client;
            chatClient.messages.Enqueue(msg);

            // respect max entries
            if (chatClient.messages.Count > chatClient.keepMessages)
                chatClient.messages.Dequeue();
        }
    }
}