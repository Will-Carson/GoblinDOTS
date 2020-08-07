/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_GEN_POINT_DEFS
#define MUDBUN_GEN_POINT_DEFS

struct GenPoint
{
  float4 posNorm;

  int4 boneIndex;

  uint boneWeight;
  uint padding0;
  uint padding1;
  uint padding2;

  SdfBrushMaterialCompressed material;
};

#if MUDBUN_IS_COMPUTE_SHADER
RWStructuredBuffer<GenPoint> aGenPoint;
#else
StructuredBuffer<GenPoint> aGenPoint;
#endif

#endif

