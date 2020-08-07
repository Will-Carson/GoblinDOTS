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
#include "../Math/Quaternion.cginc"
#include "../Noise/ClassicNoise3D.cginc"

static const float2 splatVertOffsetLsTable[3] = 
{
  float2(-0.866f, -0.5f), 
  float2( 0.0f  ,  1.0f), 
  float2( 0.866f, -0.5f), 
};

void mudbun_splat_vert
(
  uint id, 
  out float4 vertexWs, 
  out float3 vertexLs, 
  out float3 normalWs, 
  out float3 normalLs, 
  out float2 tex
)
{
  uint iGenPoint = id / 3;

  normalLs = unpack_normal(aGenPoint[iGenPoint].posNorm.w);
  float3 camDir = normalize(UNITY_MATRIX_V._m20_m21_m22);

  #ifdef SHADERPASS_SHADOWCASTER
    //normal = sign(dot(normal, camDir)) * camDir;
  #endif

  float3 tangentLs = normalize(find_ortho(normalLs));

  normalWs = mul(localToWorldIt, float4(normalLs, 0.0f)).xyz;
  float3 tangentWs = mul(localToWorld, float4(tangentLs, 0.0f)).xyz;

  float3 geomNormal = normalize(lerp(normalWs, sign(dot(normalWs, camDir)) * camDir, splatCameraFacing));
  float3 geomTangent = normalize(find_ortho(geomNormal));
  float3 geomTangent2 = normalize(cross(geomTangent, geomNormal));

  float3 centerLs = aGenPoint[iGenPoint].posNorm.xyz;
  float3 centerWs = mul(localToWorld, float4(centerLs, 1.0f)).xyz;

  if (splatRotation > kEpsilon)
  {
    float4 q = quat_axis_angle(geomNormal, cnoise(centerWs * splatRotationNoisiness / voxelSize) * splatRotation);
    geomTangent = quat_rot(q, geomTangent);
    geomTangent2 = quat_rot(q, geomTangent2);
  }

  tex = splatVertOffsetLsTable[id % 3];
  float sizeMult = aGenPoint[iGenPoint].material.size;
  float2 splatVertOffsetLs = splatSize * sizeMult * tex; 
  float3 splatVertOffsetWs = splatVertOffsetLs.x * localToWorldScale.xyz * geomTangent + splatVertOffsetLs.y * localToWorldScale.xyz * geomTangent2;
  vertexWs = float4(centerWs + splatVertOffsetWs, 1.0f);
  vertexLs = mul(worldToLocal, vertexWs).xyz;

  #ifndef SHADERPASS_SHADOWCASTER
    vertexWs.xyz -= project_vec(splatVertOffsetWs, _WorldSpaceCameraPos - centerWs);
  #endif
}

void mudbun_splat_vert
(
  uint id, 
  out float4 vertexWs, 
  out float3 vertexLs, 
  out float3 normalWs, 
  out float3 normalLs, 
  out float4 color, 
  out float4 emission, 
  out float2 metallicSmoothness, 
  out float2 tex, 
  out float4 intWeight
)
{
  mudbun_splat_vert(id, vertexWs, vertexLs, normalWs, normalLs, tex);

  uint iGenPoint = id / 3;
  color = unpack_rgba(aGenPoint[iGenPoint].material.color);
  emission = float4(unpack_rgba(aGenPoint[iGenPoint].material.emissionTightness).rgb, 1.0f);
  metallicSmoothness = unpack_saturated(aGenPoint[iGenPoint].material.metallicSmoothness);
  intWeight = unpack_rgba(aGenPoint[iGenPoint].material.intWeight);
}

#endif
