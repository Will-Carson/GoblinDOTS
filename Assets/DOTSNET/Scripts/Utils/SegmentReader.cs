﻿// SegmentReader reads blittable types from an ArraySegment.
//
// This way it's allocation free, doesn't need pooling, and doesn't need one
// extra abstraction.
//
// => Transport gives an ArraySegment, we use it all the way to the end.
// => should be compatible with DOTS Jobs/Burst because we use simple types!!!
// => this is also easily testable
// => all the functions return a bool to indicate if reading succeeded or not.
//    this way we can detect invalid messages / attacks easily
// => 100% safe from allocation attacks because WE DO NOT ALLOCATE ANYTHING.
//    if WriteBytesAndSize receives a uint.max header, we don't allocate giga-
//    bytes of RAM because we just return a segment of a segment.
// => only DOTS supported blittable types like float3, NativeString, etc.
//
// Use C# extensions to add your own reader functions, for example:
//
//   public static bool ReadItem(this SegmentReader reader, out Item item)
//   {
//       return reader.ReadInt(out item.Id);
//   }
//
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DOTSNET
{
    public struct SegmentReader
    {
        // the segment with Offset and Count
        // -> Offset is position in byte array
        // -> Count is bytes until the end of the segment
        ArraySegment<byte> segment;

        // previously we modified Offset & Count when reading.
        // now we have a separate Position that actually starts at '0', so based
        // on Offset
        // (previously we also recreated 'segment' after every read. now we just
        //  increase Position, which is easier and faster)
        public int Position;

        // helper field to calculate amount of bytes remaining to read
        public int Remaining => segment.Count - (segment.Offset + Position);

        public SegmentReader(ArraySegment<byte> segment)
        {
            this.segment = segment;
            Position = 0;
        }

        // read 'size' bytes for blittable(!) type T via fixed memory copying
        internal unsafe bool ReadBlittable<T>(out T value, int size)
            where T : unmanaged
        {
            // check if blittable for safety.
            // calling this with non-blittable types like bool would otherwise
            // give us strange runtime errors.
            // (for example, 0xFF would be neither true/false in unit tests
            //  with Assert.That(value, Is.Equal(true/false))
            //
            // => it's enough to check in Editor
            // => the check is around 20% slower for 1mio reads
            // => it's definitely worth it to avoid strange non-blittable issues
#if UNITY_EDITOR
            if (!UnsafeUtility.IsBlittable(typeof(T)))
                throw new Exception(typeof(T) + " is not blittable!");
#endif

            // enough data to read?
            if (segment.Array != null && Remaining >= size)
            {
                fixed (byte* ptr = &segment.Array[segment.Offset + Position])
                {
                    // cast buffer to a T* pointer and then read from it.
                    // value is a copy of that memory.
                    // value does not live at 'ptr' position.
                    // we also have a unit test to guarantee that.
                    // (so changing Array does not change value afterwards)
                    // breakpoint here to check manually:
                    //void* valuePtr = UnsafeUtility.AddressOf(ref value);
                    value = *(T*)ptr;
                }
                Position += size;
                return true;
            }
            value = new T();
            return false;
        }

        // read 1 byte
        public bool ReadByte(out byte value) => ReadBlittable(out value, 1);

        // read 1 byte boolean
        // Read"Bool" instead of "ReadBoolean" for consistency with ReadInt etc.
        public bool ReadBool(out bool value)
        {
            // read it as byte (which is blittable),
            // then convert to bool (which is not blittable)
            if (ReadByte(out byte temp))
            {
                value = temp != 0;
                return true;
            }
            value = false;
            return false;
        }

        // read 2 bytes ushort
        // Read"UShort" instead of "ReadUInt16" for consistency with ReadFloat etc.
        public bool ReadUShort(out ushort value) => ReadBlittable(out value, 2);

        // read 2 bytes short
        // Read"Short" instead of "ReadInt16" for consistency with ReadFloat etc.
        public bool ReadShort(out short value) => ReadBlittable(out value, 2);

        // read 4 bytes uint
        // Read"UInt" instead of "ReadUInt32" for consistency with ReadFloat etc.
        public bool ReadUInt(out uint value) => ReadBlittable(out value, 4);

        // read 4 bytes int
        // Read"Int" instead of "ReadInt32" for consistency with ReadInt2 etc.
        public bool ReadInt(out int value) => ReadBlittable(out value, 4);

        // peek 4 bytes int (read them without actually modifying the position)
        // -> this is useful for cases like ReadBytesAndSize where we need to
        //    peek the header first to decide if we do a full read or not
        //    (in other words, to make it atomic)
        // -> we pass segment by value, not by reference. this way we can reuse
        //    the regular ReadInt call without any modifications to segment.
        public bool PeekInt(out int value)
        {
            int previousPosition = Position;
            bool result = ReadInt(out value);
            Position = previousPosition;
            return result;
        }

        // read 8 bytes int2
        public bool ReadInt2(out int2 value) => ReadBlittable(out value, 8);

        // read 12 bytes int3
        public bool ReadInt3(out int3 value) => ReadBlittable(out value, 12);

        // read 16 bytes int4
        public bool ReadInt4(out int4 value) => ReadBlittable(out value, 16);

        // read 8 bytes ulong
        // Read"ULong" instead of "ReadUInt64" for consistency with ReadFloat etc.
        public bool ReadULong(out ulong value) => ReadBlittable(out value, 8);

        // read 8 bytes long
        // Read"Long" instead of "ReadInt64" for consistency with ReadFloat etc.
        public bool ReadLong(out long value) => ReadBlittable(out value, 8);

        // read byte array as ArraySegment to avoid allocations
        public bool ReadBytes(int count, out ArraySegment<byte> value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (segment.Array != null && Remaining >= count)
            {
                // create 'value' segment and point it at the right section
                value = new ArraySegment<byte>(segment.Array, segment.Offset + Position, count);

                // update position
                Position += count;
                return true;
            }
            // not enough data to read
            return false;
        }

        // read size, bytes as ArraySegment to avoid allocations
        public bool ReadBytesAndSize(out ArraySegment<byte> value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            //    => at first it needs at least 4 bytes for the header
            //    => then it needs enough size for header + size bytes
            if (segment.Array != null && Remaining >= 4 &&
                PeekInt(out int size) &&
                0 <= size && 4 + size <= Remaining)
            {
                // we already peeked the size and it's valid. so let's skip it.
                Position += 4;

                // now do the actual bytes read
                // -> ReadBytes and ArraySegment constructor both use 'int', so we
                //    use 'int' here too. that's the max we can support. if we would
                //    use 'uint' then we would have to use a 'checked' conversion to
                //    int, which means that an attacker could trigger an Overflow-
                //    Exception. using int is big enough and fail safe.
                // -> ArraySegment.Array can't be null, so we don't have to
                //    handle that case
                return ReadBytes(size, out value);
            }
            // not enough data to read
            return false;
        }

        // read 4 bytes float
        // Read"Float" instead of ReadSingle for consistency with ReadFloat3 etc
        public bool ReadFloat(out float value) => ReadBlittable(out value, 4);

        // read 8 bytes float2
        public bool ReadFloat2(out float2 value) => ReadBlittable(out value, 8);

        // read 12 bytes float3
        public bool ReadFloat3(out float3 value) => ReadBlittable(out value, 12);

        // read 16 bytes float4
        public bool ReadFloat4(out float4 value) => ReadBlittable(out value, 16);

        // read 8 bytes double
        public bool ReadDouble(out double value) => ReadBlittable(out value, 8);

        // read 16 bytes double2
        public bool ReadDouble2(out double2 value) => ReadBlittable(out value, 16);

        // read 24 bytes double3
        public bool ReadDouble3(out double3 value) => ReadBlittable(out value, 24);

        // read 32 bytes double4
        public bool ReadDouble4(out double4 value) => ReadBlittable(out value, 32);

        // read 16 bytes decimal
        public bool ReadDecimal(out decimal value) => ReadBlittable(out value, 16);

        // read 16 bytes quaternion
        public bool ReadQuaternion(out quaternion value) => ReadBlittable(out value, 16);

        // read Bytes16 struct
        public bool ReadBytes16(out Bytes16 value) => ReadBlittable(out value, 16);

        // read Bytes30 struct
        public bool ReadBytes30(out Bytes30 value) => ReadBlittable(out value, 30);

        // read Bytes62 struct
        public bool ReadBytes62(out Bytes62 value) => ReadBlittable(out value, 62);

        // read Bytes126 struct
        // => check total size before any reads to make it atomic!
        public bool ReadBytes126(out Bytes126 value) => ReadBlittable(out value, 126);

        // read Bytes510 struct
        public bool ReadBytes510(out Bytes510 value) => ReadBlittable(out value, 510);

        // read NativeString32
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool ReadNativeString32(out NativeString32 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (segment.Array != null && Remaining >= 32)
            {
                // read length in bytes, and check max to avoid allocation attacks
                if (ReadUShort(out value.LengthInBytes) &&
                    value.LengthInBytes <= NativeString32.MaxLength)
                {
                    // read the Bytes30 struct
                    return ReadBytes30(out value.buffer);
                }
            }
            value = new NativeString32();
            return false;
        }

        // read NativeString64
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool ReadNativeString64(out NativeString64 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (segment.Array != null && Remaining >= 64)
            {
                // read length in bytes, and check max to avoid allocation attacks
                if (ReadUShort(out value.LengthInBytes) &&
                    value.LengthInBytes <= NativeString64.MaxLength)
                {
                    // read the Bytes62 struct
                    return ReadBytes62(out value.buffer);
                }
            }
            value = new NativeString64();
            return false;
        }

        // read NativeString128
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool ReadNativeString128(out NativeString128 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (segment.Array != null && Remaining >= 128)
            {
                // read length in bytes, and check max to avoid allocation attacks
                if (ReadUShort(out value.LengthInBytes) &&
                    value.LengthInBytes <= NativeString128.MaxLength)
                {
                    // read the Bytes126 struct
                    return ReadBytes126(out value.buffer);
                }
            }
            value = new NativeString128();
            return false;
        }

        // read NativeString512
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool ReadNativeString512(out NativeString512 value)
        {
            // enough data to read?
            // => check total size before any reads to make it atomic!
            if (segment.Array != null && Remaining >= 512)
            {
                // read length in bytes, and check max to avoid allocation attacks
                if (ReadUShort(out value.LengthInBytes) &&
                    value.LengthInBytes <= NativeString512.MaxLength)
                {
                    // read the Bytes510 struct
                    return ReadBytes510(out value.buffer);
                }
            }
            value = new NativeString512();
            return false;
        }
    }
}