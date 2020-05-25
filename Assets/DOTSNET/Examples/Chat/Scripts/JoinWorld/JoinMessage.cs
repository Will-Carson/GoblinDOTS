using Unity.Collections;

namespace DOTSNET.Examples.Chat
{
    public struct JoinMessage : NetworkMessage
    {
        public NativeString32 name;

        public ushort GetID() { return 0x1001; }

        public JoinMessage(NativeString32 name)
        {
            this.name = name;
        }

        public bool Serialize(ref SegmentWriter writer)
        {
            return writer.WriteNativeString32(name);
        }

        public bool Deserialize(ref SegmentReader reader)
        {
            return reader.ReadNativeString32(out name);
        }
    }
}