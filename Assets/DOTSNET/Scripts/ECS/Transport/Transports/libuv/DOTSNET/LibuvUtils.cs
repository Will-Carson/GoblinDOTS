using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace DOTSNET.Libuv
{
    public static class LibuvUtils
    {
        // fast int to byte[] conversion and vice versa
        // -> test with 100k conversions:
        //    BitConverter.GetBytes(ushort): 144ms
        //    bit shifting: 11ms
        // -> 10x speed improvement makes this optimization actually worth it
        // -> this way we don't need to allocate BinaryWriter/Reader either
        // -> 4 bytes because some people may want to send messages larger than
        //    64K bytes
        // -> big endian is standard for network transmissions, and necessary
        //    for compatibility with Erlang
        // -> non-alloc is important for MMO scale networking performance.
        public static void IntToBytesBigEndianNonAlloc(int value, byte[] bytes)
        {
            bytes[0] = (byte)(value >> 24);
            bytes[1] = (byte)(value >> 16);
            bytes[2] = (byte)(value >> 8);
            bytes[3] = (byte)value;
        }

        public static int BytesToIntBigEndian(byte[] bytes, int offset)
        {
            return
                (bytes[offset + 0] << 24) |
                (bytes[offset + 1] << 16) |
                (bytes[offset + 2] << 8) |
                 bytes[offset + 3];
        }

        // copy size header, data into payload buffer so we only have to do ONE
        // libuv QueueWriteStream call, not two.
        public static void ConstructPayload(byte[] payload, ArraySegment<byte> message)
        {
            // construct header (size) without allocations
            IntToBytesBigEndianNonAlloc(message.Count, payload);

            // copy data into it, starting at '4' after header
            Buffer.BlockCopy(message.Array, message.Offset, payload, 4, message.Count);
        }

        // libuv doesn't resolve host names.
        // and it only works with IPv4 with our configuration.
        public static bool ResolveToIPV4(string hostname, out IPAddress address)
        {
            // resolve host name (if hostname. otherwise it returns the IP)
            // and connect to the first available address (IPv4 or IPv6)
            // => GetHostAddresses is BLOCKING (for a very short time). we could
            //    move it to the ConnectThread, but it's hardly worth the extra
            //    code since we would have to create the socket in ConnectThread
            //    too, which would require us to use locks around socket every-
            //    where. it's better to live with a <1s block (if any).
            try
            {
                // resolving usually gives an IPv6 and an IPv4 address.
                // find the IPv4 address.
                IPAddress[] addresses = Dns.GetHostAddresses(hostname);
                foreach (IPAddress ip in addresses)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = ip;
                        return true;
                    }
                }
            }
            catch (SocketException exception)
            {
                // it's not an error. just an invalid host so log a warning.
                Debug.LogWarning("Libuv Connect: failed to resolve host: " + hostname + " reason: " + exception);
            }
            address = null;
            return false;
        }
    }
}