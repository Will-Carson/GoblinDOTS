// SegmentWriter writes blittable types into a byte array and returns the
// written segment that can be sent to the Transport.
//
// This way it's allocation free, doesn't need pooling, and doesn't need one
// extra abstraction.
//
// Start with an empty segment and allow for it to grow until Array.Length!
//
// note: we don't use a NetworkWriter style class because Burst/jobs need simple
//       types. and using a NetworkWriter struct would be odd when creating
//       value copies all the time.
//
// => it's stateless. easy to test, no dependencies
// => should be compatible with DOTS Jobs/Burst because we use simple types!!!
// => doesn't care where the segment's byte[] comes from. caching it is someone
//    else's job
// => doesn't care about transport either. it just passes a segment.
// => all the functions return a bool to indicate if writing succeeded or not.
//    (it will fail if the .Array is too small)
// => 100% allocation free for MMO scale networking.
// => only DOTS supported blittable types like float3, NativeString, etc.
//
// Endianness:
//   DOTSNET automatically serializes full structs.
//   this only works as long as all the platforms have the same endianness,
//   which is Little Endian on Mac/Windows/Linux.
//   (see https://www.coder.work/article/247983 in case we need both endians)
//
// Use C# extensions to add your own writer functions, for example:
//
//   public static bool WriteItem(this SegmentWriter writer, Item item)
//   {
//       writer.WriteInt(item.Id);
//       return true;
//   }
//
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DOTSNET
{
    public struct SegmentWriter
    {
        // the buffer to write into
        // note: we could also write into an ArraySegment, but so far this was
        //       never necessary.
        byte[] buffer;

        // the position in the buffer
        public int Position;

        // helper field to calculate space in bytes remaining to write
        public int Space => buffer != null ? buffer.Length - Position : 0;

        // generate a segment of written data
        public ArraySegment<byte> segment
        {
            get { return new ArraySegment<byte>(buffer, 0, Position); }
        }

        // byte[] constructor.
        // SegmentWriter will assume that the whole byte[] can be written into,
        // from start to end.
        public SegmentWriter(byte[] buffer)
        {
            this.buffer = buffer;
            Position = 0;
        }

        ////////////////////////////////////////////////////////////////////////
        // writes bytes for blittable(!) type T via fixed memory copying
        //
        // this works for all blittable structs, and the value order is always
        // the same on all platforms because:
        // "C#, Visual Basic, and C++ compilers apply the Sequential layout
        //  value to structures by default."
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute?view=netcore-3.1
        public unsafe bool WriteBlittable<T>(T value)
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
            {
                Debug.LogError(typeof(T) + " is not blittable!");
                return false;
            }
#endif

            // calculate size
            //   sizeof(T) gets the managed size at compile time.
            //   Marshal.SizeOf<T> gets the unmanaged size at runtime (slow).
            // => our 1mio writes benchmark is 6x slower with Marshal.SizeOf<T>
            // => for blittable types, sizeof(T) is even recommended:
            // https://docs.microsoft.com/en-us/dotnet/standard/native-interop/best-practices
            int size = sizeof(T);

            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= size)
            {
                fixed (byte* ptr = &buffer[Position])
                {
                    // Marshal class is 6x slower in our 10mio writes benchmark
                    //Marshal.StructureToPtr(value, (IntPtr)ptr, false);

                    // cast buffer to T* pointer, then assign value to the area
                    *(T*)ptr = value;
                }
                Position += size;
                return true;
            }

            // not enough space to write
            return false;
        }

        ////////////////////////////////////////////////////////////////////////

        // write 1 byte, grow segment
        public bool WriteByte(byte value) => WriteBlittable(value);

        // write 1 byte bool, grow segment
        // -> bool is not blittable, so cast it to a byte first.
        public bool WriteBool(bool value) => WriteBlittable((byte)(value ? 1 : 0));

        // write 2 bytes ushort, grow segment
        public bool WriteUShort(ushort value) => WriteBlittable(value);

        // write 2 bytes short, grow segment
        public bool WriteShort(short value) => WriteBlittable(value);

        // write 4 bytes uint, grow segment
        public bool WriteUInt(uint value) => WriteBlittable(value);

        // write 4 bytes int, grow segment
        public bool WriteInt(int value) => WriteBlittable(value);

        // write 8 bytes int2, grow segment
        public bool WriteInt2(int2 value) => WriteBlittable(value);

        // write 12 bytes int3, grow segment
        public bool WriteInt3(int3 value) => WriteBlittable(value);

        // write 16 bytes int4, grow segment
        public bool WriteInt4(int4 value) => WriteBlittable(value);

        // write 8 bytes ulong, grow segment
        public bool WriteULong(ulong value) => WriteBlittable(value);

        // write 8 bytes long, grow segment
        public bool WriteLong(long value) => WriteBlittable(value);

        // write byte array as ArraySegment, grow segment
        public unsafe bool WriteBytes(ArraySegment<byte> value)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= value.Count)
            {
                // write 'count' bytes at position

                // 10 mio writes: 868ms
                //Array.Copy(value.Array, value.Offset, buffer, Position, value.Count);

                // 10 mio writes: 775ms
                //Buffer.BlockCopy(value.Array, value.Offset, buffer, Position, value.Count);

                fixed (byte* dst = &buffer[Position],
                             src = &value.Array[value.Offset])
                {
                    // 10 mio writes: 637ms
                    UnsafeUtility.MemCpy(dst, src, value.Count);
                }

                // update position
                Position += value.Count;
                return true;
            }
            // not enough space to write
            return false;
        }

        // write size, byte array as ArraySegment, grow segment
        public bool WriteBytesAndSize(ArraySegment<byte> value)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= 4 + value.Count)
            {
                // writes size header first
                // -> ReadBytes and ArraySegment constructor both use 'int', so we
                //    use 'int' here too. that's the max we can support. if we would
                //    use 'uint' then we would have to use a 'checked' conversion to
                //    int, which means that an attacker could trigger an Overflow-
                //    Exception. using int is big enough and fail safe.
                // -> ArraySegment.Array can't be null, so we don't have to
                //    handle that case.
                return WriteInt(value.Count) &&
                       WriteBytes(value);
            }
            // not enough space to write
            return false;
        }

        // write 4 bytes float
        // Write"Float" instead of WriteSingle for consistency with WriteFloat2 etc
        public bool WriteFloat(float value) => WriteBlittable(value);

        // write 8 bytes float2
        public bool WriteFloat2(float2 value) => WriteBlittable(value);

        // write 12 bytes float3
        public bool WriteFloat3(float3 value) => WriteBlittable(value);

        // write 16 bytes float4
        public bool WriteFloat4(float4 value) => WriteBlittable(value);

        // write 8 bytes double
        public bool WriteDouble(double value) => WriteBlittable(value);

        // write 16 bytes double2
        public bool WriteDouble2(double2 value) => WriteBlittable(value);

        // write 24 bytes double3
        public bool WriteDouble3(double3 value) => WriteBlittable(value);

        // write 32 bytes double4
        public bool WriteDouble4(double4 value) => WriteBlittable(value);

        // write 16 bytes decimal
        public bool WriteDecimal(decimal value) => WriteBlittable(value);

        // write 16 bytes quaternion
        public bool WriteQuaternion(quaternion value) => WriteBlittable(value);

        // write Bytes16 struct
        public bool WriteBytes16(Bytes16 value) => WriteBlittable(value);

        // write Bytes30 struct
        public bool WriteBytes30(Bytes30 value) => WriteBlittable(value);

        // write Bytes62 struct
        public bool WriteBytes62(Bytes62 value) => WriteBlittable(value);

        // write Bytes126 struct
        public bool WriteBytes126(Bytes126 value) => WriteBlittable(value);

        // write Bytes510 struct
        public bool WriteBytes510(Bytes510 value) => WriteBlittable(value);

        // write Bytes4094 struct
        public bool WriteBytes4094(Bytes4094 value) => WriteBlittable(value);

        // write NativeString32 struct
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool WriteNativeString32(NativeString32 value)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= 32)
            {
                // write LengthInBytes, Bytes
                return WriteUShort(value.LengthInBytes) &&
                       WriteBytes30(value.buffer);
            }
            // not enough space to write
            return false;
        }

        // write NativeString64 struct
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool WriteNativeString64(NativeString64 value)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= 64)
            {
                // write LengthInBytes, Bytes
                return WriteUShort(value.LengthInBytes) &&
                       WriteBytes62(value.buffer);
            }
            // not enough space to write
            return false;
        }

        // write NativeString128 struct
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool WriteNativeString128(NativeString128 value)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= 128)
            {
                // write LengthInBytes, Bytes
                return WriteUShort(value.LengthInBytes) &&
                       WriteBytes126(value.buffer);
            }
            // not enough space to write
            return false;
        }

        // write NativeString512 struct
        // -> fixed size means not worrying about max size / allocation attacks
        // -> fixed size saves size header
        // -> no need to worry about encoding, we can use .Bytes directly!
        public bool WriteNativeString512(NativeString512 value)
        {
            // enough space in array?
            // => check total size before any writes to make it atomic!
            if (buffer != null && Space >= 512)
            {
                // write LengthInBytes, Bytes
                return WriteUShort(value.LengthInBytes) &&
                       WriteBytes510(value.buffer);
            }
            // not enough space to write
            return false;
        }
    }
}
