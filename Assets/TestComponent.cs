
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class TestComponent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < 11; x++)
        {
            var result = x * .1;
            result = result * 8;
            result = result - 9;
            result = pow(2, result);
            result = result / .5f;
            Debug.Log(result);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
