using Unity.Entities;

namespace DOTSNET
{
    public static class Extensions
    {
        // helper function to get a unique id for Entities.
        // it combines 4 bytes Index + 4 bytes Version into 8 bytes unique Id
        // note: in theory the Index should be enough because it is only reused
        //       after the Entity was destroyed. but let's be 100% safe and use
        //       Index + Version as recommended in the Entity documentation.
        public static ulong UniqueId(this Entity entity)
        {
            // convert to uint
            uint index = (uint)entity.Index;
            uint version = (uint)entity.Version;

            // shift version from 0x000000FFFFFFFF to 0xFFFFFFFF00000000
            ulong shiftedVersion = (ulong)version << 32;

            // OR into result
            return (index & 0xFFFFFFFF) | shiftedVersion;
        }

        // DynamicBuffer helper function to check if it contains an element
        public static bool Contains<T>(this DynamicBuffer<T> buffer, T value)
            where T : struct
        {
            // DynamicBuffer foreach allocates. use for.
            for (int i = 0; i < buffer.Length; ++i)
                // .Equals can't be called from a Job.
                // GetHashCode() works as long as <T> implements it manually!
                // (which is faster too!)
                if (buffer[i].GetHashCode() == value.GetHashCode())
                    return true;
            return false;
        }
    }
}