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
    }
}