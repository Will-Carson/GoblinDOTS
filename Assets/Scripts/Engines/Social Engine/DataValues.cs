//using System;
//using Unity.Collections;

//public struct DataValues
//{
//    // define personality traits here
//    public float placeholder;

//    public NativeArray<float> GetValues(Allocator a)
//    {
//        var valuesArray = new NativeArray<float>(G.valuesTraits, a);

//        // Add each variable, in order, to the valuesArray
//        valuesArray[0] = placeholder;

//        return valuesArray;
//    }

//    public void SetValues(NativeArray<float> v)
//    {
//        // Set each value here
//        placeholder = v[0];
//    }

//    internal bool InRange(ValueRequirement template)
//    {
//        var test = true;
//        if (placeholder < template.minValues.placeholder || placeholder > template.maxValues.placeholder) test = false;
//        return test;
//    }
//}