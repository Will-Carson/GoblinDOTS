/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using UnityEngine;

namespace MudBun
{
  public class MudNoiseScroller : MonoBehaviour
  {
    public Vector3 Speed = Vector3.zero;

    private void Update()
    {
      var noise = GetComponent<MudNoiseVolume>();
      if (noise != null)
      {
        noise.Offset += Speed * Time.deltaTime;
        switch (noise.CoordinateSystem)
        {
          case MudNoiseVolume.CoordinateSystemEnum.Cartesian:
            noise.Offset = 
              new Vector3
              (
                Mathf.Repeat(noise.Offset.x, MathUtil.CartesianNoisePeriod), 
                Mathf.Repeat(noise.Offset.y, MathUtil.CartesianNoisePeriod), 
                Mathf.Repeat(noise.Offset.z, MathUtil.CartesianNoisePeriod)
              );
            break;

          case MudNoiseVolume.CoordinateSystemEnum.Spherical:
            noise.Offset =
              new Vector3
              (
                Mathf.Repeat(noise.Offset.x, MathUtil.CartesianNoisePeriod), 
                Mathf.Repeat(noise.Offset.y, MathUtil.SphericalNoisePeriod), 
                Mathf.Repeat(noise.Offset.z, MathUtil.CartesianNoisePeriod)
              );
            break;
        }
      }

      var curveSimple = GetComponent<MudCurveSimple>();
      if (curveSimple != null)
      {
        curveSimple.NoiseOffset += Speed.x * Time.deltaTime;
        curveSimple.NoiseOffset = Mathf.Repeat(curveSimple.NoiseOffset, MathUtil.CartesianNoisePeriod);
      }

      var curveFull = GetComponent<MudCurveFull>();
      if (curveFull != null)
      {
        curveFull.NoiseOffset += Speed.x * Time.deltaTime;
        curveFull.NoiseOffset = Mathf.Repeat(curveFull.NoiseOffset, MathUtil.CartesianNoisePeriod);
      }
    }
  }
}

