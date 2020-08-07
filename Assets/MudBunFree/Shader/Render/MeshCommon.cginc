/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_SPLAT_COMMON
#define MUDBUN_SPLAT_COMMON

#include "../Math/Codec.cginc"
#include "../Math/Vector.cginc"

void mudbun_mesh_vert
(
  uint id, 
  out float4 vertexWs, 
  out float3 vertexLs, 
  out float3 normalWs, 
  out float3 normalLs, 
  out float4 color
)
{
  vertexLs = aGenPoint[id].posNorm.xyz;
  vertexWs = mul(localToWorld, float4(vertexLs, 1.0f));
  normalLs = unpack_normal(aGenPoint[id].posNorm.w);
  normalWs = mul(localToWorld, float4(normalLs, 0.0f)).xyz;

  color = unpack_rgba(aGenPoint[id].material.color);
}

void mudbun_mesh_vert
(
  uint id, 
  out float4 vertexWs, 
  out float3 vertexLs, 
  out float3 normalWs, 
  out float3 normalLs, 
  out float4 color, 
  out float4 emission, 
  out float2 metallicSmoothness, 
  out float4 intWeight
)
{
  mudbun_mesh_vert(id, vertexWs, vertexLs, normalWs, normalLs, color);

  emission = float4(unpack_rgba(aGenPoint[id].material.emissionTightness).rgb, 1.0f);
  metallicSmoothness = unpack_saturated(aGenPoint[id].material.metallicSmoothness);
  intWeight = unpack_rgba(aGenPoint[id].material.intWeight);
}

#endif
