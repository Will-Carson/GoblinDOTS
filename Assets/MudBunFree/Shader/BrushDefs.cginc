/*****************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_BRUSH_DEFS
#define MUDBUN_BRUSH_DEFS

#include "Math/Codec.cginc"

#define kSdfNoOp                (-1)

#define kSdfGroupStart          (-2)
#define kSdfGroupEnd            (-3)

// primitives
#define kSdfBox                  (0)
#define kSdfSphere               (1)
#define kSdfCylinder             (2)
#define kSdfTorus                (3)
#define kSdfSolidAngle           (4)

// effects
#define kSdfParticle           (100)
#define kSdfParticleSystem     (101)
#define kSdfNoiseVolume        (102)
#define kSdfNoiseCurveSimple   (103)
#define kSdfNoiseCurveFull     (104)

// distortion
#define kSdfFishEye            (200)
#define kSdfPinch              (201)
#define kSdfTwist              (202)
#define kSdfQuantize           (203)

// modifiers
#define kSdfOnion              (300)

// operators
#define kSdfUnion                (0)
#define kSdfSubtract             (1)
#define kSdfIntersect            (2)
#define kSdfDye                  (3)
#define kSdfDistort           (-100)
#define kSdfModify             (100)

// flags
#define kSdfBrushFlagsContributeMaterial        (1 << 0)
#define kSdfBrushFlagsCountAsBone               (1 << 1)
#define kSdfBrushFlagsLockNoisePosition         (1 << 2)
#define kSdfBrushFlagsSphericalNoiseCoordinates (1 << 3)

// boundaries
#define kSdfNoiseBoundaryBox        (0)
#define kSdfNoiseBoundarySphere     (1)
#define kSdfNoiseBoundaryCylinder   (2)
#define kSdfNoiseBoundaryTorus      (3)
#define kSdfNoiseBoundarySolidAngle (4)

struct SdfBrushMaterial
{
  float4 color;
  float4 emission;
  float4 metallicSmoothnessSizeTightness;
  float4 intWeight;
};

struct SdfBrushMaterialCompressed
{
  uint color;
  uint emissionTightness;
  uint intWeight;
  uint padding0;

  float metallicSmoothness;
  float size;
  float padding1;
  float padding2;
};

SdfBrushMaterialCompressed pack_material(SdfBrushMaterial mat)
{
  SdfBrushMaterialCompressed m;

  m.color = pack_rgba(mat.color);
  m.emissionTightness = pack_rgba(float4(mat.emission.rgb, mat.metallicSmoothnessSizeTightness.w));
  m.intWeight = pack_rgba(mat.intWeight);
  m.padding0 = 0.0f;

  m.metallicSmoothness = pack_saturated(mat.metallicSmoothnessSizeTightness.xy);
  m.size = mat.metallicSmoothnessSizeTightness.z;
  m.padding1 = 0.0f;
  m.padding2 = 0.0f;

  return m;
}

struct SdfBrush
{
  int type;
  int op;
  int iProxy;
  int index;

  float3 position;
  float blend;

  float4 rotation;

  float3 size;
  float radius;

  float4 data0;
  float4 data1;
  float4 data2;
  float4 data3;

  int flags;
  int materialIndex;
  int boneIndex;
  int padding;
};

StructuredBuffer<SdfBrush> aBrush;
StructuredBuffer<SdfBrushMaterial> aBrushMaterial;
int numBrushes;

#endif

