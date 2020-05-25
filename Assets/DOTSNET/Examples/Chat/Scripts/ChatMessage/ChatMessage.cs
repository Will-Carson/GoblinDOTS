﻿using Unity.Collections;

namespace DOTSNET.Examples.Chat
{
    public struct ChatMessage : NetworkMessage
    {
        public NativeString32 sender;
        public NativeString128 text;

        public ushort GetID() { return 0x1003; }

        public ChatMessage(NativeString32 sender, NativeString128 text)
        {
            this.sender = sender;
            this.text = text;
        }

        public bool Serialize(ref SegmentWriter writer)
        {
            return writer.WriteNativeString32(sender) &&
                   writer.WriteNativeString128(text);
        }

        public bool Deserialize(ref SegmentReader reader)
        {
            return reader.ReadNativeString32(out sender) &&
                   reader.ReadNativeString128(out text);
        }
    }
}