using Unity.Entities;
using Unity.Collections;

public struct DataValues
{
    // TODO BROKE
    public static NativeArray<float> GetValues(Allocator a, DataValues v)
    {
        return new NativeArray<float>(2, a);
    }
}