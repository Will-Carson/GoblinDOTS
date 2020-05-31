using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class TestComponent : MonoBehaviour
{
    private void Awake()
    {
        for (int i = 0; i < World.All.Count; i++)
        {
            World.All[i].QuitUpdate = true;
        }
    }

    private void Start()
    {
        var t = new TestClass<B>();
        t.Func();
    }
}

public class TestClass<T> where T : unmanaged, A
{
    public void Func()
    {
        var b = new B()
        {
            BigMoney = 10
        };

        NativeArray<T> As = new NativeArray<T>(1, Allocator.Temp);
        dynamic i = b;
        As[0] = i;
        Debug.Log(As[0].BigMoney);
        As.Dispose();
    }
}
public interface A
{
    int BigMoney { get; set; }
}
public struct B : A
{
    public int BigMoney { get; set; }
}
