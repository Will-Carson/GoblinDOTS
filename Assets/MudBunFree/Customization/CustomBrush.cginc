/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_CUSTOM_BRUSH
#define MUDBUN_CUSTOM_BRUSH

// make sure these value do not conflict with those in BrushDefs.cginc
#define kCustomSolid           (900)
#define kCustomDistortion      (901)
#define kCustomModifier        (902)


// returns custom SDF value, the signed distance from solid surface
float sdf_custom_brush
(
  float res,      // current SDF result before brush is applied
  inout float3 p, // sample position in world space (distortion brushes modify this)
  float3 pRel,    // sample position in brush space (relative to brush transform)
  SdfBrush brush  // brush data (see BrushDefs.cginc for data layout)
)
{
  float3 h = 0.5f * brush.size;

  // add/modify custom brushes in this switch statement
  switch (brush.type)
  {
    case kCustomSolid:
    {
      // box
      res = sdf_box(pRel, h, brush.radius);
      break;
    }

    case kCustomDistortion:
    {
      // quantize
      float fade = brush.data0.z;
      float d = sdf_box(pRel, h, fade);
      if (d < 0.0f)
      {
        float cellSize = brush.data0.x;
        float strength = brush.data0.y;
        float3 r = p / cellSize;
        float3 f = floor(r);
        float3 t = r - f;
        float3 q = (f + smoothstep(0.0f, 1.0f, strength * (t - 0.5f) + 0.5f)) * cellSize;
        p = lerp(p, q, saturate(-d / max(kEpsilon, fade)));
      }
      break;
    }

    case kCustomModifier:
    {
      // onion
      float d = sdf_box(pRel, h, brush.blend);
      if (d < 0.0f)
      {
        float thickness = brush.data0.x;
        res = abs(res) - thickness;
      }
      break;
    }
  }

  return res;
}


// returns SDF value of border of distortion/modifier brush, the signed distance from border
float sdf_custom_distortion_modifier_bounds_query
(
  float3 p,       // sample position in world space
  float3 pRel,    // sample position in brush space (relative to brush transform)
  SdfBrush brush  // brush data (see BrushDefs.cginc for data layout)
)
{
  float res = kInfinity;
  float3 h = 0.5f * brush.size;

  switch (brush.type)
  {
    case kCustomDistortion:
    {
      // quantize
      float cellSize = brush.data0.w;
      float fade = brush.data0.z;
      res = sdf_box(pRel, h, fade * cellSize);
      break;
    }

    case kCustomModifier:
    {
      // onion
      float thickness = brush.data0.x;
      res = sdf_box(pRel, h + thickness, brush.blend);
      break;
    }
  }

  return res;
}

#endif

