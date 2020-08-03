// NetUV ThreadLocalPool magic replaced by ConcurrentPool by vis2k
// see also: https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
using System;
using System.Collections.Concurrent;

namespace NetUV.Core.Common
{
    public class ConcurrentPool<T>
    {
        // ConcurrentStack is completely lock-free.
        // Try ConcurrentBag if stack doesn't work out.
        readonly ConcurrentStack<T> objects = new ConcurrentStack<T>();
        readonly Func<T> objectGenerator;

        public ConcurrentPool(Func<T> objectGenerator)
        {
            this.objectGenerator = objectGenerator;
        }

        public T Take() => objects.TryPop(out T item) ? item : objectGenerator();
        public void Return(T item) => objects.Push(item);
    }
}