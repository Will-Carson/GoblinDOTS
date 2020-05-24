
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class TestComponent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        double impact = .5f;
        double targetAffinity = .5f;
        double deedAggession = .5f;
        double affinityDelta = .5f;

        double dominanceDelta;

        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                for (int z = 0; z < 20; z++)
                {
                    //dominanceDelta = sign(-impact) * sign(targetAffinity) * abs(deedAggession) * abs(affinityDelta);
                    ////dominanceDelta += abs(dominanceDelta) * GetPowerCurve(x * -.1f);
                    //dominanceDelta += abs(dominanceDelta) * SolveQuadratic(x, y, z)[0];
                    //if (double.IsNaN(dominanceDelta))
                    //{
                    //    Debug.Log(x + " " + y + " " + z);
                    //}
                }
            }

            dominanceDelta = sign(-impact) * sign(targetAffinity) * abs(deedAggession) * abs(-affinityDelta);
            Debug.Log(dominanceDelta);
            dominanceDelta += abs(dominanceDelta) * NewGetPowerCurve(.8f);
            Debug.Log(dominanceDelta);
            Debug.Log(NewGetPowerCurve(-1));
        }

        // sign(memory.impact) * sign(GetAffinity(witness, memory.deedTarget)) * abs(GetDeed(memory.type).Aggression) * abs(affinityDelta);



        //dominanceDelta = sign(impact) * sign(targetAffinity) * abs(deedAggession) * abs(affinityDelta);
        ////dominanceDelta += abs(dominanceDelta) * GetPowerCurve(fm.power - factionMembers[memory.deedDoer].power);
        //Debug.Log(dominanceDelta);
        //dominanceDelta = sign(-impact) * sign(targetAffinity) * abs(deedAggession) * abs(affinityDelta);
        //Debug.Log(dominanceDelta);
        //dominanceDelta = sign(impact) * sign(-targetAffinity) * abs(deedAggession) * abs(affinityDelta);
        //Debug.Log(dominanceDelta);
        //dominanceDelta = sign(-impact) * sign(-targetAffinity) * abs(deedAggession) * abs(affinityDelta);
        //Debug.Log(dominanceDelta);
    }

    private float GetPowerCurve(float diff)
    {
        var result = diff; // * .1;
        result = result * 2.5f;
        result = result;
        result = pow(2, result);
        result = result / .5f;
        return (float)result;
    }

    private float NewGetPowerCurve(float diff)
    {
        var a = 10;
        var result = pow(a, diff) - 1;
        result = result / (a - 1);
        return result;
    }

    public float[] SolveQuadratic(double a, double b, double c)
    {
        double sqrtpart = (b * b) - (4 * a * c);
        double answer1 = ((-1) * b + sqrt(sqrtpart)) / (2 * a);
        double answer2 = ((-1) * b - sqrt(sqrtpart)) / (2 * a);
        return new float[] { (float)answer1, (float)answer2 };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
