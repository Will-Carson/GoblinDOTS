/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_SHADER_COMMON
#define MUDBUN_SHADER_COMMON

#include "../Math/MathConst.cginc"

#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PS4) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_SWITCH)
  #define MUDBUN_VALID (1)
#endif

#if MUDBUN_VALID
  #include "../BrushDefs.cginc"
  #include "../GenPointDefs.cginc"
  StructuredBuffer<int> indirectDrawArgs;
#endif


#ifdef SHADERPASS // HDRP & URP
  #if SHADERPASS == SHADERPASS_SHADOWS
    #define SHADERPASS_SHADOWCASTER
  #endif
#endif

#if defined(UNITY_PASS_SHADOWCASTER) && !defined(SHADERPASS_SHADOWCASTER) // standard
  #define SHADERPASS_SHADOWCASTER
#endif

float4 _Color;
float4 _Emission;
float _Metallic;
float _Smoothness;

float _AlphaCutoutThreshold;
float _Dithering;

#if MUDBUN_BUILT_IN_RP
int _UseTex0;
sampler2D _MainTex;
float4 _MainTex_ST;
int _MainTexX;
int _MainTexY;
int _MainTexZ;

int _UseTex1;
sampler2D _Tex1;
float4 _Tex1_ST;
int _Tex1X;
int _Tex1Y;
int _Tex1Z;

int _UseTex2;
sampler2D _Tex2;
float4 _Tex2_ST;
int _Tex2X;
int _Tex2Y;
int _Tex2Z;

int _UseTex3;
sampler2D _Tex3;
float4 _Tex3_ST;
int _Tex3X;
int _Tex3Y;
int _Tex3Z;
#endif

float voxelSize;
float splatSize;
float splatRotation;
float splatRotationNoisiness;
float splatCameraFacing;

float4x4 localToWorld;
float4x4 localToWorldIt;
float4 localToWorldScale;
float4x4 worldToLocal;

struct Vertex
{
  float4 vertex    : POSITION;
  float3 normal    : NORMAL;
  float4 color     : COLOR;
  float4 texcoord1 : TEXCOORD1;
  float4 texcoord2 : TEXCOORD2;
  uint id          : SV_VertexID;
};

struct Input
{
  float2 tex                : TEXCOORD0;
  float4 color              : COLOR;
  float4 emission           : TEXCOORD3;
  float2 metallicSmoothness : TEXCOORD4;
  float4 texWeight          : TEXCOORD5;
  float3 localPos           : TEXCOORD6;
  float3 localNorm          : TEXCOORD7;
  float4 screenPos;
};

#define DITHER_SIZE (8)
static const float ditherTable[DITHER_SIZE * DITHER_SIZE] = 
{
  0.00000f, 0.50000f, 0.12500f, 0.62500f, 0.03125f, 0.53125f, 0.15625f, 0.65625f, 
  0.75000f, 0.25000f, 0.87500f, 0.37500f, 0.78125f, 0.28125f, 0.90625f, 0.40625f, 
  0.18750f, 0.68750f, 0.06250f, 0.56250f, 0.21875f, 0.71875f, 0.09375f, 0.59375f, 
  0.93750f, 0.43750f, 0.81250f, 0.31250f, 0.96875f, 0.46875f, 0.84375f, 0.34375f, 
  0.04688f, 0.54688f, 0.17188f, 0.67188f, 0.01562f, 0.51562f, 0.14062f, 0.64062f, 
  0.79688f, 0.29688f, 0.92188f, 0.42188f, 0.76562f, 0.26562f, 0.89062f, 0.39062f, 
  0.23438f, 0.73438f, 0.10938f, 0.60938f, 0.20312f, 0.70312f, 0.07812f, 0.57812f, 
  0.98438f, 0.48438f, 0.85938f, 0.35938f, 0.95312f, 0.45312f, 0.82812f, 0.32812f
};

void applyOpaqueTransparency(Input i, float a)
{
  a = 1.01f * (2.0f * (a - 0.5f)) + 0.5f;
  i.screenPos.xy *= _ScreenParams.xy / (i.screenPos.w + kEpsilon);
  float ditherThreshold = ditherTable[(uint(i.screenPos.y) % DITHER_SIZE) * DITHER_SIZE + uint(i.screenPos.x) % DITHER_SIZE];
  clip(a - lerp(_AlphaCutoutThreshold, max(_AlphaCutoutThreshold, ditherThreshold), _Dithering));
}

float4 tex2D_triplanar
(
  sampler2D tex, 
  float4 texSt, 
  float3 weight, 
  float3 localPos, 
  bool projectX, 
  bool projectY, 
  bool projectZ
)
{
  float4 color = 0.0f;
  float totalWeight = 0.0f;
  if (projectX)
  {
    color += tex2D(tex, localPos.yz * texSt.xy + texSt.zw) * weight.x;
    totalWeight += weight.x;
  }
  if (projectY)
  {
    color += tex2D(tex, localPos.zx * texSt.xy + texSt.zw) * weight.y;
    totalWeight += weight.y;
  }
  if (projectZ)
  {
    color += tex2D(tex, localPos.xy * texSt.xy + texSt.zw) * weight.z;
    totalWeight += weight.z;
  }

  if (totalWeight <= 0.0f)
    return 1.0f;

  return color / totalWeight;
}

#endif
