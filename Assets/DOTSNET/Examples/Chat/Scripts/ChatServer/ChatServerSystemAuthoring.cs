using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DOTSNET.Examples.Chat
{
    public class ChatServerSystemAuthoring : NetworkServerAuthoring
    {
        // add system if Authoring is used
        public override Type GetSystemType() => typeof(ChatServerSystem);
    }

    // use SelectiveSystemAuthoring to create it selectively
    [DisableAutoCreation]
    public class ChatServerSystem : NetworkServerSystem
    {
        // dependencies
        [AutoAssign] protected NetworkServerSystem server;

        // nicknames per connection
        public Dictionary<int, FixedString32> names = new Dictionary<int, FixedString32>();
    }
}